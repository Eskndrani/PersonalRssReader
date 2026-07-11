using System.Text;
using System.Text.Json;
using CodeHollow.FeedReader;
using CodeHollow.FeedReader.Feeds;
using Microsoft.Extensions.Caching.Memory;
using Ganss.Xss;
using System.Net;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache(); // Enable Caching

var app = builder.Build();

app.UseDefaultFiles(); // Look for an index.html file
app.UseStaticFiles();  // Serve CSS, JS, and HTML files

var httpClient = new HttpClient();

// this user agent is used to avoid 403 errors from some websites that block requests from unknown clients
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");


app.MapGet("/api/feeds", async () =>
{
    var feeds = await ReadFeedsAsync();
    return Results.Ok(feeds);
});

app.MapPost("/api/feeds", async (FeedDto dto, IMemoryCache cache) =>
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
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching {dto.Url}: {ex.Message}");
        return Results.BadRequest("The URL does not point to a valid RSS/Atom feed.");
    }

    var feed = new Feed(Guid.NewGuid().ToString(), dto.Url, title, dto.Username, dto.Password);
    feeds.Add(feed);
    await WriteFeedsAsync(feeds);

    cache.Remove("cached_news");

    return Results.Created($"/api/feeds/{feed.Id}", feed);
});

app.MapDelete("/api/feeds/{id}", async (string id, IMemoryCache cache) =>
{
    var feeds = await ReadFeedsAsync();
    var feed = feeds.FirstOrDefault(f => f.Id == id);

    if (feed is null)
        return Results.NotFound();

    feeds.Remove(feed);
    await WriteFeedsAsync(feeds);

    cache.Remove("cached_news");

    return Results.NoContent();
});

app.MapGet("/api/news", async (IMemoryCache cache) =>
{
    if (cache.TryGetValue("cached_news", out List<Article>? cachedArticles))
    {
        return Results.Ok(cachedArticles);
    }

    var feeds = await ReadFeedsAsync();
    var sanitizer = new HtmlSanitizer();

    var anySucceeded = false;

    var tasks = feeds.Select(async feed =>
    {
        try
        {
            var feedXml = await FetchFeedXmlAsync(feed.Url, feed.Username, feed.Password);
            Console.WriteLine($"[DEBUG] {feed.Url} — first 500 chars:\n{feedXml[..Math.Min(500, feedXml.Length)]}");

            var feedData = FeedReader.ReadFromString(feedXml);
            Console.WriteLine($"[DEBUG] {feed.Url} — items found: {feedData.Items.Count}");

            anySucceeded = true;

            return feedData.Items.Select(item =>
            {
                var hasEnclosure = item.SpecificItem is Rss20FeedItem rss && rss.Enclosure != null;
                Console.WriteLine($"[DEBUG]   Item: \"{item.Title}\" — has RSS enclosure: {hasEnclosure}");

                return new Article(
                    FeedTitle: sanitizer.Sanitize(feed.Title),
                    Title: sanitizer.Sanitize(WebUtility.HtmlDecode(item.Title ?? "")),
                    Link: (Uri.TryCreate(item.Link, UriKind.Absolute, out var uri)
                           && (uri.Scheme == "http" || uri.Scheme == "https"))
                        ? uri.ToString()
                        : "#",
                    PublishDate: item.PublishingDate ?? DateTime.UtcNow,
                    Summary: sanitizer.Sanitize(WebUtility.HtmlDecode(item.Description ?? "")),
                    AudioUrl: GetEnclosureAudioUrl(item)
                );
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching {feed.Url}: {ex.Message}");
            return Enumerable.Empty<Article>();
        }
    });

    var results = await Task.WhenAll(tasks);
    var articles = results.SelectMany(a => a)
        .OrderByDescending(a => a.PublishDate)
        .ToList();

    if (anySucceeded)
    {
        cache.Set("cached_news", articles, TimeSpan.FromMinutes(5));
    }

    return Results.Ok(articles);
});

app.MapPost("/api/refresh-feed", async (FeedDto dto, IMemoryCache cache) =>
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
    catch (Exception ex)
    {
        Console.WriteLine($"Error fetching {dto.Url}: {ex.Message}");
        return Results.BadRequest("Failed to fetch the feed.");
    }

    var sanitizedTitle = sanitizer.Sanitize(feedData.Title ?? feed.Title);

    var freshArticles = feedData.Items.Select(item => new Article(
        FeedTitle: sanitizedTitle,
        Title: sanitizer.Sanitize(WebUtility.HtmlDecode(item.Title ?? "")),
        Link: (Uri.TryCreate(item.Link, UriKind.Absolute, out var uri)
               && (uri.Scheme == "http" || uri.Scheme == "https"))
            ? uri.ToString()
            : "#",
        PublishDate: item.PublishingDate ?? DateTime.UtcNow,
        Summary: sanitizer.Sanitize(WebUtility.HtmlDecode(item.Description ?? "")),
        AudioUrl: GetEnclosureAudioUrl(item)
    )).ToList();

    var cachedArticles = cache.TryGetValue("cached_news", out List<Article>? existing)
        ? existing ?? []
        : [];

    var updatedArticles = cachedArticles
        .Where(a => a.FeedTitle != feed.Title && a.FeedTitle != sanitizedTitle)
        .Concat(freshArticles)
        .OrderByDescending(a => a.PublishDate)
        .ToList();

    cache.Set("cached_news", updatedArticles, TimeSpan.FromMinutes(5));

    return Results.Ok();
});

// TEMPORARY ENDPOINT FOR SECURITY TESTING
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
    return await JsonSerializer.DeserializeAsync<List<Feed>>(stream) ?? [];
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
    // 1. Check Standard RSS Enclosure
    if (item.SpecificItem is CodeHollow.FeedReader.Feeds.Rss20FeedItem rssItem)
    {
        if (rssItem.Enclosure != null && rssItem.Enclosure.MediaType != null && rssItem.Enclosure.MediaType.StartsWith("audio"))
            return rssItem.Enclosure.Url;
            
        // Check for Media RSS (used by many professional podcasts)
        var mediaContent = rssItem.Element.Element("media:content");
        if (mediaContent != null && mediaContent.Attribute("medium")?.Value == "audio")
            return mediaContent.Attribute("url")?.Value;

        return rssItem.Enclosure?.Url;
    }
    
    // 2. Check Atom Fallback
    else if (item.SpecificItem is CodeHollow.FeedReader.Feeds.AtomFeedItem atomItem)
    {
        var audioLink = atomItem.Links?.FirstOrDefault(l => 
            l.Relation == "enclosure" || 
            (l.LinkType != null && l.LinkType.StartsWith("audio")));
            
        return audioLink?.Href;
    }

    return null;
}

record Feed(string Id, string Url, string Title, string? Username = null, string? Password = null);
record FeedDto(string Url, string? Username = null, string? Password = null);
record Article(string FeedTitle, string Title, string Link, DateTime PublishDate, string Summary, string? AudioUrl = null);
