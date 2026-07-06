using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();


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
