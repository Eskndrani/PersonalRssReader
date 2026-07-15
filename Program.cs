using System.Text;
using System.Text.Json;
using CodeHollow.FeedReader;
using CodeHollow.FeedReader.Feeds;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ganss.Xss;
using System.Net;
using System.Net.Http.Headers;
using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=articles.db"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseDefaultFiles();
app.UseStaticFiles();

var httpClient = new HttpClient();

httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");


app.MapGet("/api/feeds", async () =>
{
    var feeds = await ReadFeedsAsync();
    return Results.Ok(feeds);
});

app.MapPost("/api/feeds", async (FeedDto dto) =>
{
    var feeds = await ReadFeedsAsync();

    if (feeds.Any(f => f.Url == dto.Url))
        return Results.BadRequest("A feed with this URL already exists.");

    string title;
    var sanitizer = new HtmlSanitizer();
    try
    {
        var feedXml = await FetchFeedXmlAsync(dto.Url, dto.Username, dto.Password);
        var feedData = FeedReader.ReadFromString(feedXml);
        title = sanitizer.Sanitize(feedData.Title ?? dto.Url);
    }
        catch (Exception)
        {
            return Results.BadRequest("The URL does not point to a valid RSS/Atom feed.");
    }

    var feed = new Feed(Guid.NewGuid().ToString(), dto.Url, title, dto.Username, dto.Password);
    feeds.Add(feed);
    await WriteFeedsAsync(feeds);

    return Results.Created($"/api/feeds/{feed.Id}", feed);
});

app.MapPost("/api/feeds/batch", async (BatchFeedDto dto) =>
{
    var feeds = await ReadFeedsAsync();
    var existingUrls = new HashSet<string>(feeds.Select(f => f.Url));
    var sanitizer = new HtmlSanitizer();

    var newUrls = dto.Urls
        .Select(u => u.Trim())
        .Where(u => !string.IsNullOrEmpty(u) && !existingUrls.Contains(u))
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
            var feedXml = await FetchFeedXmlAsync(url, dto.Username, dto.Password);
            var feedData = FeedReader.ReadFromString(feedXml);
            var title = sanitizer.Sanitize(feedData.Title ?? url);

            lock (feeds)
            {
                feeds.Add(new Feed(Guid.NewGuid().ToString(), url, title, dto.Username, dto.Password));
                Interlocked.Increment(ref added);
            }
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
        await WriteFeedsAsync(feeds);

    return Results.Ok(new { Added = added, Failed = failed });
});

app.MapPatch("/api/feeds/{id}/favorite", async (string id) =>
{
    var feeds = await ReadFeedsAsync();
    var feed = feeds.FirstOrDefault(f => f.Id == id);

    if (feed is null)
        return Results.NotFound();

    var updatedFeed = feed with { IsFavorite = !feed.IsFavorite };
    var index = feeds.IndexOf(feed);
    feeds[index] = updatedFeed;
    await WriteFeedsAsync(feeds);

    return Results.Ok(updatedFeed);
});

app.MapDelete("/api/feeds/{id}", async (string id) =>
{
    var feeds = await ReadFeedsAsync();
    var feed = feeds.FirstOrDefault(f => f.Id == id);

    if (feed is null)
        return Results.NotFound();

    feeds.Remove(feed);
    await WriteFeedsAsync(feeds);

    return Results.NoContent();
});

app.MapGet("/api/news", async (AppDbContext db, [FromQuery] int? retentionDays) =>
{
    var feeds = await ReadFeedsAsync();
    var sanitizer = new HtmlSanitizer();
    var now = DateTime.UtcNow;

    var tasks = feeds.Select(async feed =>
    {
        try
        {
            var feedXml = await FetchFeedXmlAsync(feed.Url, feed.Username, feed.Password);

            var feedData = FeedReader.ReadFromString(feedXml);

            var feedTitle = sanitizer.Sanitize(feed.Title);
            var items = new List<ArticleEntity>();

            foreach (var item in feedData.Items)
            {                var link = (Uri.TryCreate(item.Link, UriKind.Absolute, out var uri)
                           && (uri.Scheme == "http" || uri.Scheme == "https"))
                    ? uri.ToString()
                    : GenerateLinkId(feedTitle, item.Title, item.PublishingDate ?? now);

                items.Add(new ArticleEntity
                {
                    FeedTitle = feedTitle,
                    Title = sanitizer.Sanitize(WebUtility.HtmlDecode(item.Title ?? "")),
                    Link = link,
                    PublishDate = item.PublishingDate ?? now,
                    Summary = sanitizer.Sanitize(WebUtility.HtmlDecode(item.Description ?? "")),
                    ImageUrl = GetImageUrl(item),
                    AudioUrl = GetEnclosureAudioUrl(item)
                });
            }

            return items;
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

    if (parsedArticles.Count > 0)
    {
        var existing = (await db.Articles.Select(a => a.Link).ToListAsync()).ToHashSet();

        var newArticles = parsedArticles.Where(a => !existing.Contains(a.Link)).ToList();
        if (newArticles.Count > 0)
        {
            db.Articles.AddRange(newArticles);
            await db.SaveChangesAsync();
        }
    }

    var cutoff = DateTime.UtcNow.AddDays(-(retentionDays ?? 30));
    var oldArticles = await db.Articles.Where(a => a.PublishDate < cutoff && !a.IsBookmarked).ToListAsync();
    if (oldArticles.Count > 0)
    {
        db.Articles.RemoveRange(oldArticles);
        await db.SaveChangesAsync();
    }

    var articles = await db.Articles
        .OrderByDescending(a => a.PublishDate)
        .Select(a => new Article(a.Id, a.FeedTitle, a.Title, a.Link, a.PublishDate, a.Summary, a.AudioUrl, a.ImageUrl, a.IsBookmarked, a.IsRead))
        .ToListAsync();

    return Results.Ok(articles);
});

app.MapPost("/api/news/summarize", async (SummarizeDto dto) =>
{
    var sanitizer = new HtmlSanitizer();
    var summary = await GenerateAiSummaryAsync(dto.TextContent);
    var clean = sanitizer.Sanitize(summary);
    return Results.Ok(new { summary = clean });
});

app.MapPatch("/api/news/{id}/bookmark", async (int id, AppDbContext db) =>
{
    var article = await db.Articles.FindAsync(id);
    if (article is null) return Results.NotFound();

    article.IsBookmarked = !article.IsBookmarked;
    await db.SaveChangesAsync();

    return Results.Ok(new { id = article.Id, isBookmarked = article.IsBookmarked });
});

app.MapPatch("/api/news/{id}/read", async (int id, AppDbContext db) =>
{
    var article = await db.Articles.FindAsync(id);
    if (article is null) return Results.NotFound();

    article.IsRead = true;
    await db.SaveChangesAsync();

    return Results.Ok(new { id = article.Id, isRead = article.IsRead });
});

app.MapPost("/api/refresh-feed", async (FeedDto dto, AppDbContext db, [FromQuery] int? retentionDays) =>
{
    var feeds = await ReadFeedsAsync();
    var feed = feeds.FirstOrDefault(f => f.Url == dto.Url);

    if (feed is null)
        return Results.BadRequest("This feed is not in your subscriptions.");

    var sanitizer = new HtmlSanitizer();
    CodeHollow.FeedReader.Feed feedData;
    try
    {
        var feedXml = await FetchFeedXmlAsync(feed.Url, feed.Username, feed.Password);
        feedData = FeedReader.ReadFromString(feedXml);
    }
    catch (Exception)
    {
        return Results.BadRequest("Failed to fetch the feed.");
    }

    var sanitizedTitle = sanitizer.Sanitize(feedData.Title ?? feed.Title);
    var now = DateTime.UtcNow;

    var freshArticles = new List<ArticleEntity>();
    foreach (var item in feedData.Items)
    {
        var link = (Uri.TryCreate(item.Link, UriKind.Absolute, out var uri)
                   && (uri.Scheme == "http" || uri.Scheme == "https"))
            ? uri.ToString()
            : GenerateLinkId(sanitizedTitle, item.Title, item.PublishingDate ?? now);

        freshArticles.Add(new ArticleEntity
        {
            FeedTitle = sanitizedTitle,
            Title = sanitizer.Sanitize(WebUtility.HtmlDecode(item.Title ?? "")),
            Link = link,
            PublishDate = item.PublishingDate ?? now,
            Summary = sanitizer.Sanitize(WebUtility.HtmlDecode(item.Description ?? "")),
            AudioUrl = GetEnclosureAudioUrl(item)
        });
    }

    freshArticles = freshArticles.GroupBy(a => a.Link).Select(g => g.First()).ToList();

    var existing = (await db.Articles.Select(a => a.Link).ToListAsync()).ToHashSet();

    var newArticles = freshArticles.Where(a => !existing.Contains(a.Link)).ToList();
    if (newArticles.Count > 0)
    {
        db.Articles.AddRange(newArticles);
        await db.SaveChangesAsync();
    }

    var cutoff = DateTime.UtcNow.AddDays(-(retentionDays ?? 30));
    var oldArticles = await db.Articles.Where(a => a.PublishDate < cutoff && !a.IsBookmarked).ToListAsync();
    if (oldArticles.Count > 0)
    {
        db.Articles.RemoveRange(oldArticles);
        await db.SaveChangesAsync();
    }

    return Results.Ok();
});

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

async Task<List<Feed>> ReadFeedsAsync()
{
    if (!File.Exists("feeds.json"))
        return [];

    await using var stream = File.OpenRead("feeds.json");
    var feeds = await JsonSerializer.DeserializeAsync<List<Feed>>(stream) ?? [];

    var cleaned = false;
    for (var i = 0; i < feeds.Count; i++)
    {
        var f = feeds[i];
        if (f.Url != f.Url.Trim())
        {
            feeds[i] = f with { Url = f.Url.Trim() };
            cleaned = true;
        }
    }

    if (cleaned)
        await WriteFeedsAsync(feeds);

    return feeds;
}

async Task WriteFeedsAsync(List<Feed> feeds)
{
    await using var stream = File.Create("feeds.json");
    await JsonSerializer.SerializeAsync(stream, feeds, new JsonSerializerOptions { WriteIndented = true });
}

async Task<string> FetchFeedXmlAsync(string url, string? username, string? password)
{
    var request = new HttpRequestMessage(HttpMethod.Get, url);

    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    var response = await httpClient.SendAsync(request);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadAsStringAsync();
}

string? GetEnclosureAudioUrl(CodeHollow.FeedReader.FeedItem item)
{
    string? url = null;
    string? type = null;

    if (item.SpecificItem is CodeHollow.FeedReader.Feeds.Rss20FeedItem rssItem && rssItem.Enclosure != null)
    {
        url = rssItem.Enclosure.Url;
        type = rssItem.Enclosure.MediaType;
    }
    else if (item.SpecificItem is CodeHollow.FeedReader.Feeds.AtomFeedItem atomItem)
    {
        var audioLink = atomItem.Links?.FirstOrDefault(l => l.Relation == "enclosure" || (l.LinkType != null && l.LinkType.StartsWith("audio")));
        url = audioLink?.Href;
        type = audioLink?.LinkType;
    }

    if (string.IsNullOrEmpty(url)) return null;

    var lowerUrl = url.ToLowerInvariant();
    var lowerType = type?.ToLowerInvariant() ?? "";

    if (lowerType.Contains("video") || lowerUrl.Contains(".webm") || lowerUrl.Contains(".mp4")) return null;

    if (lowerType.Contains("audio") || lowerUrl.EndsWith(".mp3") || lowerUrl.EndsWith(".m4a") || lowerUrl.EndsWith(".wav") || lowerUrl.EndsWith(".ogg")) return url;

    return null;
}

string? GetImageUrl(CodeHollow.FeedReader.FeedItem item)
{
    if (item.SpecificItem is CodeHollow.FeedReader.Feeds.Rss20FeedItem rssItem && rssItem.Enclosure != null)
    {
        if (rssItem.Enclosure.MediaType != null && rssItem.Enclosure.MediaType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
            return rssItem.Enclosure.Url;
    }

    var mediaContents = item.SpecificItem is CodeHollow.FeedReader.Feeds.Rss20FeedItem rss
        ? rss.Element.Elements().Where(e => e.Name.LocalName == "content")
        : Enumerable.Empty<XElement>();

    foreach (var mc in mediaContents)
    {
        var type = mc.Attribute("type")?.Value ?? "";
        var medium = mc.Attribute("medium")?.Value ?? "";
        if (type.StartsWith("image", StringComparison.OrdinalIgnoreCase) || medium == "image")
        {
            var url = mc.Attribute("url")?.Value;
            if (!string.IsNullOrEmpty(url)) return url;
        }
    }

    if (item.SpecificItem is CodeHollow.FeedReader.Feeds.Rss20FeedItem rssItem2)
    {
        var itunesImage = rssItem2.Element.Element("{http://www.itunes.com/dtds/podcast-1.0.dtd}image");
        if (itunesImage != null)
        {
            var href = itunesImage.Attribute("href")?.Value;
            if (!string.IsNullOrEmpty(href)) return href;
        }
    }

    return null;
}

async Task<string> GenerateAiSummaryAsync(string input)
{
    await Task.Delay(1500);
    return "✨ [AI Summary]: " + input[..Math.Min(input.Length, 100)] + "... This is a simulated summary.";
}

string GenerateLinkId(string feedTitle, string? title, DateTime publishDate)
{
    var raw = $"{feedTitle}|{title}|{publishDate.Ticks}";
    var bytes = Encoding.UTF8.GetBytes(raw);
    var hash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(bytes))
        .Replace("/", "_").Replace("+", "-")[..32];
    return $"__gen__/{hash}";
}

class ArticleEntity
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
}

class AppDbContext : DbContext
{
    public DbSet<ArticleEntity> Articles => Set<ArticleEntity>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ArticleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Link).IsUnique();
            entity.HasIndex(e => e.PublishDate);
        });
    }
}

record Feed(string Id, string Url, string Title, string? Username = null, string? Password = null, bool IsFavorite = false);
record FeedDto(string Url, string? Username = null, string? Password = null);
record BatchFeedDto(string[] Urls, string? Username = null, string? Password = null);
record SummarizeDto(string Link, string TextContent);
record Article(int Id, string FeedTitle, string Title, string Link, DateTime PublishDate, string Summary, string? AudioUrl = null, string? ImageUrl = null, bool IsBookmarked = false, bool IsRead = false);
