using System.Text.Json;
using System.ServiceModel.Syndication;
using System.Xml;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var httpClient = new HttpClient();

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

    return Results.Created($"/api/feeds/{feed.Id}", feed);
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
