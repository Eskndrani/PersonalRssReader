using System.Text.Json;
using System.ServiceModel.Syndication;
using Microsoft.Extensions.Caching.Memory;
using System.Xml;
using Ganss.Xss;

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
    try
    {
        await using var stream = await httpClient.GetStreamAsync(dto.Url);
        using var reader = XmlReader.Create(stream);
        var syndicationFeed = SyndicationFeed.Load(reader);
        title = syndicationFeed.Title?.Text ?? dto.Url;
    }
    catch
    {
        return Results.BadRequest("The URL does not point to a valid RSS/Atom feed.");
    }

    var feed = new Feed(Guid.NewGuid().ToString(), dto.Url, title);
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
            await using var stream = await httpClient.GetStreamAsync(feed.Url);
            using var reader = XmlReader.Create(stream);
            var syndicationFeed = SyndicationFeed.Load(reader);

            anySucceeded = true;

            return syndicationFeed.Items.Select(item => new Article(
                FeedTitle: sanitizer.Sanitize(feed.Title),
                Title: sanitizer.Sanitize(item.Title?.Text ?? ""),
                Link: sanitizer.Sanitize(item.Links.FirstOrDefault()?.Uri?.ToString() ?? ""),
                PublishDate: item.PublishDate.UtcDateTime,
                Summary: sanitizer.Sanitize(item.Summary?.Text ?? "")
            ));
        }
        catch
        {
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

record Feed(string Id, string Url, string Title);
record FeedDto(string Url);
record Article(string FeedTitle, string Title, string Link, DateTime PublishDate, string Summary);
