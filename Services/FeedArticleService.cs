using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Ganss.Xss;

namespace PersonalRssReader.Services;

public sealed class FeedArticleService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HtmlSanitizer _sanitizer = new();

    private static readonly Regex ImgRegex = new(
        @"<img[^>]+src\s*=\s*[""']?([^""'>\s]+)[""']?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly XNamespace ContentNs = "http://purl.org/rss/1.0/modules/content/";
    private static readonly XNamespace MrssNs = "http://search.yahoo.com/mrss/";
    private static readonly XNamespace AtomNs = "http://www.w3.org/2005/Atom";

    private static readonly XmlReaderSettings XmlSettings = new()
    {
        IgnoreWhitespace = true,
        IgnoreComments = true,
        DtdProcessing = DtdProcessing.Ignore,
        Async = false
    };

    public FeedArticleService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<List<ArticleEntity>> FetchArticlesAsync(
        string url, string feedTitle, string? username, string? password, CancellationToken ct)
    {
        var xml = await FetchXmlAsync(url, username, password, ct);
        var (_, items) = ParseXmlToItems(xml);

        var articles = new List<ArticleEntity>();
        foreach (var p in items)
        {
            var link = (p.Link is not null
                       && Uri.TryCreate(p.Link, UriKind.Absolute, out var parsed)
                       && (parsed.Scheme == "http" || parsed.Scheme == "https"))
                ? parsed.ToString()
                : CreateLinkId(feedTitle, p.Title, p.PublishedDate);

            articles.Add(new ArticleEntity
            {
                FeedTitle = feedTitle,
                Title = _sanitizer.Sanitize(System.Net.WebUtility.HtmlDecode(p.Title ?? "")),
                Link = link,
                PublishDate = p.PublishedDate,
                Summary = _sanitizer.Sanitize(System.Net.WebUtility.HtmlDecode(
                    p.SummaryText ?? "")),
                ImageUrl = ExtractImageUrl(p),
                AudioUrl = ExtractAudioUrl(p)
            });
        }

        return articles;
    }

    public async Task<string> FetchFeedTitleAsync(
        string url, string? username, string? password, CancellationToken ct)
    {
        var xml = await FetchXmlAsync(url, username, password, ct);
        var (title, _) = ParseXmlToItems(xml);
        return title ?? url;
    }

    private async Task<string> FetchXmlAsync(
        string url, string? username, string? password, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("FeedReader");
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }

        var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            Console.WriteLine($"[FEED FETCH] {url} → HTTP {response.StatusCode}");

        response.EnsureSuccessStatusCode();
        var rawXml = await response.Content.ReadAsStringAsync(ct);
        rawXml = rawXml.Replace("\u200C", "").Replace("\u200D", "");
        rawXml = System.Text.RegularExpressions.Regex.Replace(rawXml, @"[\x00-\x08\x0B\x0C\x0E-\x1F]", "");
        return rawXml;
    }

    private static (string? title, List<ParsedItem> items) ParseXmlToItems(string xml)
    {
        XDocument doc;
        using (var reader = XmlReader.Create(new StringReader(xml), XmlSettings))
        {
            doc = XDocument.Load(reader);
        }

        var root = doc.Root!;
        var isAtom = root.Name.LocalName == "feed" && root.Name.Namespace == AtomNs;

        string? title = isAtom
            ? root.Element(AtomNs + "title")?.Value
            : root.Element("channel")?.Element("title")?.Value;

        var items = new List<ParsedItem>();
        var itemElements = isAtom
            ? root.Elements(AtomNs + "entry")
            : root.Element("channel")!.Elements("item");

        foreach (var el in itemElements)
        {
            try
            {
            var enclosures = new List<ParsedEnclosure>();
            var mediaItems = new List<ParsedMedia>();

            foreach (var enc in el.Elements())
            {
                if (enc.Name.LocalName != "enclosure") continue;
                var url = enc.Attribute("url")?.Value;
                if (!string.IsNullOrEmpty(url))
                    enclosures.Add(new ParsedEnclosure(url, enc.Attribute("type")?.Value));
            }

            foreach (var mc in el.Elements(MrssNs + "content"))
            {
                var mcUrl = mc.Attribute("url")?.Value;
                if (string.IsNullOrEmpty(mcUrl)) continue;
                var mcType = mc.Attribute("type")?.Value ?? "";
                var mcMedium = mc.Attribute("medium")?.Value ?? "";
                mediaItems.Add(new ParsedMedia(mcUrl,
                    mcType.StartsWith("image") || mcMedium == "image",
                    mcType.StartsWith("audio") || mcMedium == "audio"));
            }

            foreach (var mt in el.Elements(MrssNs + "thumbnail"))
            {
                var mtUrl = mt.Attribute("url")?.Value;
                if (!string.IsNullOrEmpty(mtUrl))
                    mediaItems.Add(new ParsedMedia(mtUrl, true, false));
            }

            string? itemTitle = isAtom
                ? el.Element(AtomNs + "title")?.Value
                : el.Element("title")?.Value;
            string? itemLink = isAtom
                ? el.Elements(AtomNs + "link")
                    .FirstOrDefault(l => l.Attribute("rel")?.Value != "self")
                    ?.Attribute("href")?.Value
                  ?? el.Element(AtomNs + "link")?.Attribute("href")?.Value
                : el.Element("link")?.Value;
            string? rawDesc = isAtom
                ? el.Element(AtomNs + "summary")?.Value ?? el.Element(AtomNs + "content")?.Value
                : el.Element("description")?.Value;
            string? rawContentEncoded = el.Element(ContentNs + "encoded")?.Value;

            items.Add(new ParsedItem(
                Title: itemTitle,
                Link: itemLink,
                PublishedDate: ParseDate(isAtom
                    ? el.Element(AtomNs + "published")?.Value ?? el.Element(AtomNs + "updated")?.Value
                    : el.Element("pubDate")?.Value) ?? DateTime.UtcNow,
                SummaryText: rawDesc ?? rawContentEncoded,
                ContentHtml: rawContentEncoded,
                Enclosures: enclosures,
                Media: mediaItems,
                Element: el));
            }
            catch { }
        }

        return (title, items);
    }

    private static DateTime? ParseDate(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        try
        {
            if (DateTimeOffset.TryParse(raw, out var dto)) return dto.UtcDateTime;
            if (DateTime.TryParse(raw, out var dt)) return dt;
        }
        catch { }
        return null;
    }

    private static string CreateLinkId(string feedTitle, string? itemTitle, DateTime publishDate)
    {
        var raw = $"{feedTitle}|{itemTitle}|{publishDate.Ticks}";
        var bytes = Encoding.UTF8.GetBytes(raw);
        var hash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(bytes))
            .Replace("/", "_").Replace("+", "-")[..32];
        return $"__gen__/{hash}";
    }

    private string? ExtractImageUrl(ParsedItem item)
    {
        foreach (var enc in item.Enclosures)
        {
            if (enc.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
                return enc.Url;
        }

        foreach (var m in item.Media)
        {
            if (m.IsImage && !string.IsNullOrEmpty(m.Url))
                return m.Url;
        }

        var element = item.Element;
        if (element is not null)
        {
            var thumb = element.Element(MrssNs + "thumbnail")?.Attribute("url")?.Value;
            if (!string.IsNullOrEmpty(thumb)) return thumb;
            var content = element.Element(MrssNs + "content")?.Attribute("url")?.Value;
            if (!string.IsNullOrEmpty(content)) return content;
        }

        var html = item.ContentHtml ?? item.SummaryText ?? "";
        var match = ImgRegex.Match(html);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ExtractAudioUrl(ParsedItem item)
    {
        foreach (var enc in item.Enclosures)
        {
            if (enc.MediaType?.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) == true
                && !string.IsNullOrEmpty(enc.Url))
                return enc.Url;
        }

        foreach (var m in item.Media)
        {
            if (m.IsAudio && !string.IsNullOrEmpty(m.Url))
                return m.Url;
        }

        return null;
    }

    private sealed record ParsedItem(
        string? Title,
        string? Link,
        DateTime PublishedDate,
        string? SummaryText,
        string? ContentHtml,
        List<ParsedEnclosure> Enclosures,
        List<ParsedMedia> Media,
        XElement? Element);

    private sealed record ParsedEnclosure(string? Url, string? MediaType);

    private sealed record ParsedMedia(string? Url, bool IsImage, bool IsAudio);
}

public class FeedFetchException : Exception
{
    public FeedFetchException(string message) : base(message) { }
}
