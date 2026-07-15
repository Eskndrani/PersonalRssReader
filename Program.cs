using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PersonalRssReader.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=articles.db"));

builder.Services.AddIdentityApiEndpoints<IdentityUser>(options =>
{
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
        options.SignInScheme = IdentityConstants.ApplicationScheme;

        options.Events = new OAuthEvents
        {
            OnTicketReceived = async ctx =>
            {
                var userManager = ctx.HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
                var email = ctx.Principal?.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(email)) return;

                var existingUser = await userManager.FindByEmailAsync(email);
                if (existingUser is null)
                {
                    var newUser = new IdentityUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(newUser);
                    if (!result.Succeeded)
                    {
                        foreach (var err in result.Errors)
                            Console.WriteLine($"[IDENTITY ERROR Google] {err.Code}: {err.Description}");
                    }
                }

                if (ctx.Principal?.Identity is ClaimsIdentity identity)
                {
                    var nameClaim = identity.FindFirst(ClaimTypes.Name);
                    if (nameClaim is not null)
                        identity.RemoveClaim(nameClaim);
                    identity.AddClaim(new Claim(ClaimTypes.Name, email));
                }
            }
        };
    });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.LoginPath = "/welcome.html";
});

builder.Services.AddAuthorization();

builder.Services.AddHttpClient("FeedReader", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
    client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
    client.DefaultRequestHeaders.Add("Referer", "https://www.google.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls13;

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddScoped<FeedArticleService>();
builder.Services.AddScoped<IAiService, AiService>();
builder.Services.AddHostedService<FeedRefreshWorker>();
builder.Services.AddSingleton<IEmailService, SmtpEmailService>();

builder.Services.AddHttpClient("AiClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseDeveloperExceptionPage();

app.MapGet("/", (HttpContext http) =>
{
    if (http.User.Identity?.IsAuthenticated == true)
        return Results.Redirect("/index.html");
    return Results.Redirect("/welcome.html");
});

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

app.MapGet("/api/auth/google-login", () =>
{
    var properties = new AuthenticationProperties { RedirectUri = "/index.html" };
    return Results.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
});

app.MapPost("/api/auth/register", async (
    RegisterRequest req, UserManager<IdentityUser> userManager,
    IEmailService emailService, HttpContext http) =>
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest("Email and password are required.");

    var user = new IdentityUser { UserName = req.Email, Email = req.Email };
    var result = await userManager.CreateAsync(user, req.Password);
    if (!result.Succeeded)
    {
        var errors = result.Errors.Select(e => e.Description);
        return Results.BadRequest(new { errors = errors });
    }

    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
    var encodedToken = Uri.EscapeDataString(token);
    var userId = user.Id;
    var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";
    var url = $"{baseUrl}/verify.html?userId={userId}&token={encodedToken}";

    await emailService.SendVerificationEmailAsync(req.Email, user.Id, token, baseUrl);

    return Results.Ok(new { message = "Account created. Check your email for the verification link.", userId, token = encodedToken });
});

app.MapPost("/api/auth/signup", async (
    RegisterRequest req, UserManager<IdentityUser> userManager,
    IEmailService emailService, HttpContext http) =>
{
    // alias — delegate to register
    return await new Func<Task<IResult>>(async () =>
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return Results.BadRequest("Email and password are required.");
        var user = new IdentityUser { UserName = req.Email, Email = req.Email };
        var result = await userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";
        var url = $"{baseUrl}/verify.html?userId={user.Id}&token={encodedToken}";
        await emailService.SendVerificationEmailAsync(req.Email, user.Id, token, baseUrl);
        return Results.Ok(new { message = "Account created. Check your email (or terminal) for the verification link.", userId = user.Id, token = encodedToken, url });
    })();
});

app.MapGet("/api/auth/login", async (
    [FromQuery] string email, [FromQuery] string password, [FromQuery] bool useCookies,
    SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager) =>
{
    var user = await userManager.FindByEmailAsync(email);
    if (user is null) return Results.Unauthorized();
    if (!await userManager.IsEmailConfirmedAsync(user))
        return Results.Json(new { error = "Please verify your email before logging in." }, statusCode: 403);

    var result = await signInManager.PasswordSignInAsync(user, password, true, false);
    if (!result.Succeeded) return Results.Unauthorized();

    return Results.Ok(new { message = "Login successful." });
});

app.MapGet("/api/auth/verify-email", async (
    [FromQuery] string userId, [FromQuery] string token,
    UserManager<IdentityUser> userManager) =>
{
    var user = await userManager.FindByIdAsync(userId);
    if (user is null) return Results.NotFound(new { error = "User not found." });

    var result = await userManager.ConfirmEmailAsync(user, Uri.UnescapeDataString(token));
    if (!result.Succeeded) return Results.BadRequest(new { error = "Invalid or expired verification token." });

    return Results.Ok(new { message = "Email verified successfully." });
});

app.MapPost("/api/auth/resend-verification", async (
    ResendRequest req, UserManager<IdentityUser> userManager,
    IEmailService emailService, HttpContext http) =>
{
    var user = await userManager.FindByEmailAsync(req.Email);
    if (user is null || user.EmailConfirmed)
        return Results.Ok(new { message = "If the account exists and is unverified, a new link has been sent." });

    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
    var encodedToken = Uri.EscapeDataString(token);
    var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";
    var url = $"{baseUrl}/verify.html?userId={user.Id}&token={encodedToken}";

    await emailService.SendVerificationEmailAsync(req.Email, user.Id, token, baseUrl);

    return Results.Ok(new { message = "A new verification link has been sent to your email." });
});

app.MapGet("/api/feeds", async (AppDbContext db, HttpContext http) =>
{
    var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var feeds = await db.Feeds
        .Where(f => f.UserId == userId)
        .OrderBy(f => f.Title)
        .Select(f => new
        {
            f.Id,
            f.Url,
            f.Title,
            f.Username,
            f.Password,
            f.IsFavorite,
            f.FaviconUrl,
            ArticleCount = db.Articles.Count(a => a.UserId == userId && a.FeedTitle == f.Title)
        })
        .ToListAsync();
    return Results.Ok(feeds);
}).RequireAuthorization();

app.MapPost("/api/feeds", async (
    FeedDto dto, AppDbContext db, FeedArticleService articleService,
    HttpContext http, CancellationToken ct) =>
{
    var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var normalizedUrl = dto.Url.Trim().TrimEnd('/').ToLowerInvariant();

    if (await db.Feeds.AnyAsync(f => f.Url.Trim().TrimEnd('/').ToLower() == normalizedUrl && f.UserId == userId))
        return Results.Conflict(new { message = "You have already subscribed to this feed." });

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
        UserId = userId,
        FaviconUrl = GetFaviconUrl(dto.Url)
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
    var existingSet = new HashSet<string>(existingUrls.Select(u => u.Trim().TrimEnd('/').ToLowerInvariant()));

    var newUrls = dto.Urls
        .Select(u => u.Trim().TrimEnd('/').ToLowerInvariant())
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
                UserId = userId,
                FaviconUrl = GetFaviconUrl(url)
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

app.MapPost("/api/chat", async (
    ChatRequest request, IAiService ai, HttpContext http,
    [FromQuery] string lang = "en") =>
{
    var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var response = await ai.AskQuestionAsync(request.Message, userId, lang);
    return Results.Ok(new { response });
}).RequireAuthorization();

app.MapGet("/api/news/daily-briefing", async (
    AppDbContext db, IAiService ai, HttpContext http,
    [FromQuery] string lang = "en") =>
{
    var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var today = DateTime.UtcNow.Date;
    var articles = await db.Articles
        .Where(a => a.UserId == userId && !a.IsRead && a.PublishDate >= today)
        .OrderByDescending(a => a.PublishDate)
        .Take(10)
        .ToListAsync();

    var summary = await ai.GenerateDailySummaryAsync(articles, lang);
    return Results.Ok(new { summary });
}).RequireAuthorization();

app.MapGet("/api/ai/summary", async (
    IAiService ai, AppDbContext db, HttpContext http,
    [FromQuery] string lang = "en") =>
{
    var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var today = DateTime.UtcNow.Date;
    var articles = await db.Articles
        .Where(a => a.UserId == userId && !a.IsRead && a.PublishDate >= today)
        .OrderByDescending(a => a.PublishDate)
        .Take(15)
        .ToListAsync();

    var summary = await ai.GenerateDailySummaryAsync(articles, lang);
    return Results.Ok(new { summary });
}).RequireAuthorization();

app.MapPost("/api/ai/chat", async (
    ChatRequest request, IAiService ai, HttpContext http,
    [FromQuery] string lang = "en") =>
{
    var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var response = await ai.AskQuestionAsync(request.Message, userId, lang);
    return Results.Ok(new { response });
}).RequireAuthorization();

app.MapGet("/api/ai/summary/article/{id:int}", async (
    int id, IAiService ai, HttpContext http,
    [FromQuery] string lang = "en") =>
{
    var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    var summary = await ai.SummarizeArticleAsync(id, userId, lang);
    return Results.Ok(new { summary });
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

static string? GetFaviconUrl(string feedUrl)
{
    if (!Uri.TryCreate(feedUrl, UriKind.Absolute, out var uri)) return null;
    return $"https://www.google.com/s2/favicons?domain={uri.Host}&sz=64";
}

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
    public string? FaviconUrl { get; set; }
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
record RegisterRequest(string Email, string Password);
record ResendRequest(string Email);
record Article(int Id, string FeedTitle, string Title, string Link, DateTime PublishDate, string Summary, string? AudioUrl = null, string? ImageUrl = null, bool IsBookmarked = false, bool IsRead = false);
