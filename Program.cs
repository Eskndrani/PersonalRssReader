using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalRssReader.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=articles.db"));

builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddAuthorization();

builder.Services.AddHttpClient("FeedReader", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddScoped<FeedArticleService>();
builder.Services.AddScoped<IAiService, AiService>();
builder.Services.AddHostedService<FeedRefreshWorker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseDeveloperExceptionPage();

app.MapIdentityApi<IdentityUser>();

app.MapGet("/api/auth/me", async (HttpContext http) =>
{
    var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null) return Results.Unauthorized();
    return Results.Ok(new { userId, email = http.User.Identity?.Name });
}).RequireAuthorization();

app.MapPost("/api/auth/logout", async (SignInManager<IdentityUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok();
}).RequireAuthorization();

app.MapGet("/api/feeds", async (AppDbContext db, HttpContext http) =>
{
    var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var feeds = await db.Feeds.Where(f => f.UserId == userId).OrderBy(f => f.Title).ToListAsync();
    return Results.Ok(feeds);
}).RequireAuthorization();

app.MapPost("/api/feeds", async (
    FeedDto dto, AppDbContext db, FeedArticleService articleService,
    HttpContext http, CancellationToken ct) =>
{
    var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    if (await db.Feeds.AnyAsync(f => f.Url == dto.Url && f.UserId == userId))
        return Results.BadRequest("A feed with this URL already exists.");

    string title;
    try
    {
        title = await articleService.FetchFeedTitleAsync(dto.Url, dto.Username, dto.Password, ct);
    }
    catch (FeedFetchException ex)
    {
        return Results.BadRequest(ex.Message);
    }

    var feed = new FeedSubscription
    {
        Id = Guid.NewGuid().ToString(),
        Url = dto.Url,
        Title = title,
        Username = dto.Username,
        Password = dto.Password,
        UserId = userId
    };
    db.Feeds.Add(feed);
    await db.SaveChangesAsync();

    return Results.Created($"/api/feeds/{feed.Id}", feed);
}).RequireAuthorization();

app.MapPost("/api/feeds/batch", async (
    BatchFeedDto dto, AppDbContext db, FeedArticleService articleService,
    HttpContext http, CancellationToken ct) =>
{
    var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var existingUrls = await db.Feeds.Where(f => f.UserId == userId).Select(f => f.Url).ToListAsync();
    var existingSet = new HashSet<string>(existingUrls);

    var newUrls = dto.Urls
        .Select(u => u.Trim())
        .Where(u => !string.IsNullOrEmpty(u) && !existingSet.Contains(u))
        .Distinct()
        .ToArray();

    if (newUrls.Length == 0)
        return Results.Ok(new { Added = 0, Failed = 0 });

    var semaphore = new SemaphoreSlim(10);
    var added = 0;
    var failed = 0;

    var tasks = newUrls.Select(async url =>
    {
        await semaphore.WaitAsync();
        try
        {
            var title = await articleService.FetchFeedTitleAsync(url, dto.Username, dto.Password, ct);
            db.Feeds.Add(new FeedSubscription
            {
                Id = Guid.NewGuid().ToString(),
                Url = url,
                Title = title,
                Username = dto.Username,
                Password = dto.Password,
                UserId = userId
            });
            Interlocked.Increment(ref added);
        }
        catch (Exception)
        {
            Interlocked.Increment(ref failed);
        }
        finally
        {
            semaphore.Release();
        }
    });

    await Task.WhenAll(tasks);

    if (added > 0)
        await db.SaveChangesAsync();

    return Results.Ok(new { Added = added, Failed = failed });
}).RequireAuthorization();

app.MapPatch("/api/feeds/{id}/favorite", async (string id, AppDbContext db, HttpContext http) =>
{
    var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var feed = await db.Feeds.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
    if (feed is null) return Results.NotFound();

    feed.IsFavorite = !feed.IsFavorite;
    await db.SaveChangesAsync();

    return Results.Ok(feed);
}).RequireAuthorization();

app.MapDelete("/api/feeds/{id}", async (string id, AppDbContext db, HttpContext http) =>
{
    var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var feed = await db.Feeds.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);
    if (feed is null) return Results.NotFound();

    db.Feeds.Remove(feed);

    var articlesToDelete = await db.Articles
        .Where(a => a.UserId == userId && a.FeedTitle == feed.Title)
        .ToListAsync();
    if (articlesToDelete.Count > 0)
    {
        db.Articles.RemoveRange(articlesToDelete);
        await db.SaveChangesAsync();
    }

    return Results.NoContent();
}).RequireAuthorization();

app.MapGet("/api/news", async (
    AppDbContext db, FeedArticleService articleService,
    HttpContext http, [FromQuery] int? retentionDays, CancellationToken ct) =>
{
    try
    {
        var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var feeds = await db.Feeds.Where(f => f.UserId == userId).ToListAsync();

        var tasks = feeds.Select(async feed =>
        {
            try
            {
                return await articleService.FetchArticlesAsync(
                    feed.Url, feed.Title, feed.Username, feed.Password, ct);
            }
            catch (Exception)
            {
                return new List<ArticleEntity>();
            }
        });

        var results = await Task.WhenAll(tasks);
        var parsedArticles = results.SelectMany(x => x)
            .GroupBy(a => a.Link)
            .Select(g => g.First())
            .ToList();

        foreach (var a in parsedArticles)
            a.UserId = userId;

        if (parsedArticles.Count > 0)
        {
            var existing = (await db.Articles
                .Where(a => a.UserId == userId)
                .Select(a => a.Link).ToListAsync()).ToHashSet();

            var newArticles = parsedArticles.Where(a => !existing.Contains(a.Link)).ToList();
            if (newArticles.Count > 0)
            {
                db.Articles.AddRange(newArticles);
                await db.SaveChangesAsync();
            }
        }

        var cutoff = DateTime.UtcNow.AddDays(-(retentionDays ?? 30));
        var oldArticles = await db.Articles
            .Where(a => a.UserId == userId && a.PublishDate < cutoff && !a.IsBookmarked)
            .ToListAsync();
        if (oldArticles.Count > 0)
        {
            db.Articles.RemoveRange(oldArticles);
            await db.SaveChangesAsync();
        }

        var articles = await db.Articles
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.PublishDate)
            .Select(a => new Article(a.Id, a.FeedTitle, a.Title, a.Link, a.PublishDate,
                a.Summary, a.AudioUrl, a.ImageUrl, a.IsBookmarked, a.IsRead))
            .ToListAsync();

        return Results.Ok(articles);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"CRASH IN /api/news: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
        return Results.Problem(detail: ex.ToString(), statusCode: 500);
    }
}).RequireAuthorization();

app.MapPost("/api/refresh-feed", async (
    FeedDto dto, AppDbContext db, FeedArticleService articleService,
    HttpContext http, [FromQuery] int? retentionDays, CancellationToken ct) =>
{
    try
    {
        var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var feed = await db.Feeds.FirstOrDefaultAsync(f => f.Url == dto.Url && f.UserId == userId);
        if (feed is null)
            return Results.BadRequest("This feed is not in your subscriptions.");

        List<ArticleEntity> freshArticles;
        try
        {
            freshArticles = await articleService.FetchArticlesAsync(
                feed.Url, feed.Title, feed.Username, feed.Password, ct);
        }
        catch (FeedFetchException ex)
        {
            return Results.BadRequest($"Failed to fetch the feed: {ex.Message}");
        }

        freshArticles = freshArticles.GroupBy(a => a.Link).Select(g => g.First()).ToList();
        foreach (var a in freshArticles)
            a.UserId = userId;

        var existing = (await db.Articles
            .Where(a => a.UserId == userId)
            .Select(a => a.Link).ToListAsync()).ToHashSet();

        var newArticles = freshArticles.Where(a => !existing.Contains(a.Link)).ToList();
        if (newArticles.Count > 0)
        {
            db.Articles.AddRange(newArticles);
            await db.SaveChangesAsync();
        }

        var cutoff = DateTime.UtcNow.AddDays(-(retentionDays ?? 30));
        var oldArticles = await db.Articles
            .Where(a => a.UserId == userId && a.PublishDate < cutoff && !a.IsBookmarked)
            .ToListAsync();
        if (oldArticles.Count > 0)
        {
            db.Articles.RemoveRange(oldArticles);
            await db.SaveChangesAsync();
        }

        return Results.Ok();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"CRASH IN /api/refresh-feed: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
        return Results.Problem(detail: ex.ToString(), statusCode: 500);
    }
}).RequireAuthorization();

app.MapPost("/api/news/summarize", async (SummarizeDto dto) =>
{
    var sanitizer = new Ganss.Xss.HtmlSanitizer();
    await Task.Delay(1500);
    var summary = $"✨ [AI Summary]: {dto.TextContent[..Math.Min(dto.TextContent.Length, 100)]}... This is a simulated summary.";
    var clean = sanitizer.Sanitize(summary);
    return Results.Ok(new { summary = clean });
}).RequireAuthorization();

app.MapPatch("/api/news/{id}/bookmark", async (int id, AppDbContext db, HttpContext http) =>
{
    var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var article = await db.Articles.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
    if (article is null) return Results.NotFound();

    article.IsBookmarked = !article.IsBookmarked;
    await db.SaveChangesAsync();

    return Results.Ok(new { id = article.Id, isBookmarked = article.IsBookmarked });
}).RequireAuthorization();

app.MapPatch("/api/news/{id}/read", async (int id, AppDbContext db, HttpContext http) =>
{
    var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var article = await db.Articles.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
    if (article is null) return Results.NotFound();

    article.IsRead = true;
    await db.SaveChangesAsync();

    return Results.Ok(new { id = article.Id, isRead = article.IsRead });
}).RequireAuthorization();

app.MapGet("/api/news/daily-briefing", async (
    AppDbContext db, IAiService ai, HttpContext http) =>
{
    var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var today = DateTime.UtcNow.Date;
    var articles = await db.Articles
        .Where(a => a.UserId == userId && !a.IsRead && a.PublishDate >= today)
        .OrderByDescending(a => a.PublishDate)
        .Take(10)
        .ToListAsync();

    var summary = await ai.GenerateDailySummaryAsync(articles);
    return Results.Ok(new { summary });
}).RequireAuthorization();

app.MapPost("/api/chat", async (ChatRequest request, IAiService ai, HttpContext http) =>
{
    var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var response = await ai.AskQuestionAsync(request.Message, userId);
    return Results.Ok(new { response });
}).RequireAuthorization();

app.MapGet("/api/hacker", () =>
{
    var xml = @"<?xml version=""1.0"" encoding=""UTF-8"" ?>
    <rss version=""2.0"">
    <channel>
      <title><![CDATA[<script>alert('Hacked Title!')</script>Dark Web News]]></title>
      <link>http://example.com/</link>
      <item>
        <title>You won a million dollars!</title>
        <link>javascript:alert('Stole your session tokens!')</link>
        <pubDate>Mon, 06 Jul 2026 12:00:00 GMT</pubDate>
        <description><![CDATA[<p>Click the link above! <script>alert('XSS Attack Execution!');</script> <b>This bold text is safe.</b></p>]]></description>
      </item>
    </channel>
    </rss>";
    return Results.Text(xml, "application/xml");
});

app.Run();

public class ArticleEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string FeedTitle { get; set; } = "";
    public DateTime PublishDate { get; set; }
    public string Link { get; set; } = "";
    public string? AudioUrl { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsBookmarked { get; set; } = false;
    public bool IsRead { get; set; } = false;
    public string UserId { get; set; } = "";
}

public class FeedSubscription
{
    public string Id { get; set; } = "";
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool IsFavorite { get; set; } = false;
    public string UserId { get; set; } = "";
}

public class AppDbContext : IdentityDbContext<IdentityUser>
{
    public DbSet<ArticleEntity> Articles => Set<ArticleEntity>();
    public DbSet<FeedSubscription> Feeds => Set<FeedSubscription>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ArticleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Link }).IsUnique();
            entity.HasIndex(e => e.PublishDate);
        });

        modelBuilder.Entity<FeedSubscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Url }).IsUnique();
        });
    }
}

record FeedDto(string Url, string? Username = null, string? Password = null);
record BatchFeedDto(string[] Urls, string? Username = null, string? Password = null);
record SummarizeDto(string Link, string TextContent);
record ChatRequest(string Message);
record Article(int Id, string FeedTitle, string Title, string Link, DateTime PublishDate, string Summary, string? AudioUrl = null, string? ImageUrl = null, bool IsBookmarked = false, bool IsRead = false);
