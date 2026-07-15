using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace PersonalRssReader.Services;

public interface IAiService
{
    Task<string> GenerateDailySummaryAsync(List<ArticleEntity> articles, string lang = "en");
    Task<string> AskQuestionAsync(string question, string userId, string lang = "en");
    Task<string> SummarizeArticleAsync(int articleId, string userId, string lang = "en");
}

public sealed class AiService : IAiService
{
    private readonly AppDbContext _db;
    private readonly HttpClient _http;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly string _endpoint;
    private readonly IHttpClientFactory _httpFactory;

    public AiService(AppDbContext db, IHttpClientFactory httpFactory, IConfiguration config)
    {
        _db = db;
        _httpFactory = httpFactory;
        _http = httpFactory.CreateClient("AiClient");
        _apiKey = config["AiApiKey"];
        _model = config["AiModel"] ?? "gpt-4o-mini";
        _endpoint = config["AiEndpoint"] ?? "https://api.openai.com/v1/chat/completions";
    }

    public async Task<string> GenerateDailySummaryAsync(List<ArticleEntity> articles, string lang = "en")
    {
        if (articles.Count == 0)
            return lang == "ar" ? "لا توجد مقالات اليوم." : "No articles found for today.";

        var headlines = string.Join("\n", articles.Select((a, i) =>
            $"{i + 1}. [{a.FeedTitle}] {a.Title}\n   رابط: {a.Link}\n   ملخص: {a.Summary?.Truncate(200)}"));

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var source = articles[0].FeedTitle;

        var (label, prompt) = lang == "ar"
            ? ("اليوم", $"اليوم {today}. المصدر الأساسي: {source}.\n\nالمقالات:\n{headlines}")
            : ("today", $"Today is {today}. Primary source: {source}.\n\nArticles:\n{headlines}");

        return await CallAiApiAsync(BuildSystemPrompt(lang, "summary"), prompt)
               ?? FallbackSummary(articles, lang);
    }

    public async Task<string> AskQuestionAsync(string question, string userId, string lang = "en")
    {
        var articles = await _db.Articles
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.PublishDate)
            .Take(20)
            .ToListAsync();

        if (articles.Count == 0)
            return lang == "ar" ? "لا توجد مقالات حديثة في خلاصتك." : "No recent articles in your feed.";

        var context = string.Join("\n", articles.Select((a, i) =>
            $"{i + 1}. [{a.FeedTitle}] {a.Title} ({a.PublishDate:yyyy-MM-dd}): {a.Summary?.Truncate(200)}"));

        var prompt = lang == "ar"
            ? $"سؤال المستخدم: \"{question}\"\n\nالسياق:\n{context}"
            : $"User question: \"{question}\"\n\nContext:\n{context}";

        return await CallAiApiAsync(BuildSystemPrompt(lang, "chat"), prompt) ??
               FallbackOffline(context, lang);
    }

    public async Task<string> SummarizeArticleAsync(int articleId, string userId, string lang = "en")
    {
        var article = await _db.Articles
            .FirstOrDefaultAsync(a => a.Id == articleId && a.UserId == userId);

        if (article is null)
            return lang == "ar" ? "المقال غير موجود." : "Article not found.";

        var fullText = article.Summary ?? "";

        if (!string.IsNullOrEmpty(article.Link) && article.Link != "#" && !article.Link.StartsWith("__gen__"))
        {
            try
            {
                var scraper = _httpFactory.CreateClient("FeedReader");
                var html = await scraper.GetStringAsync(article.Link);
                var extracted = ExtractTextFromHtml(html);
                if (!string.IsNullOrWhiteSpace(extracted))
                    fullText = extracted;
            }
            catch { }
        }

        if (string.IsNullOrWhiteSpace(fullText))
            fullText = article.Summary ?? (lang == "ar" ? "لا يوجد نص متاح." : "No text available.");

        var body = lang == "ar"
            ? $"العنوان: {article.Title}\nالمصدر: {article.FeedTitle}\nالرابط: {article.Link}\nالتاريخ: {article.PublishDate:yyyy-MM-dd}\n\nالنص الكامل:\n{fullText}"
            : $"Title: {article.Title}\nSource: {article.FeedTitle}\nLink: {article.Link}\nDate: {article.PublishDate:yyyy-MM-dd}\n\nFull text:\n{fullText}";

        var task = lang == "ar"
            ? "قم بإنشاء ملخص عميق وشامل وموثق بناءً على النص الكامل للمقال التالي."
            : "Create a deep, comprehensive, well-cited summary based on the full text of the following article.";

        return await CallAiApiAsync(BuildSystemPrompt(lang, "deep"), task + "\n\n" + body, maxTokens: 1200) ??
               (lang == "ar" ? $"✨ [ملخص محلي]: {fullText.Truncate(500)}" : $"✨ [Local Summary]: {fullText.Truncate(500)}");
    }

    private static string BuildSystemPrompt(string lang, string mode)
    {
        if (lang == "ar")
        {
            return
                "You are a professional Arabic news assistant (مساعد أخباري). " +
                "Always respond in clean, structured Arabic Markdown (RTL-aware). " +
                "Rules:\n" +
                "- Open with: بناءً على أخبار [source] ليوم [date]، أبرز الأخبار هي:\n" +
                "- Use - **bold headings** for categories (e.g., **⚽ رياضة**)\n" +
                "- Use emojis: 📰 general news, ⚽ sports, ⚠️ breaking/urgent, 🔊 audio, 🔗 sources\n" +
                "- Separate sections with --- (horizontal rule), use - bullet points\n" +
                "- ALWAYS include inline citations like ([المصدر](رابط_المقال)) after each claim\n" +
                "- Never output raw HTML, only Markdown";
        }

        return
            "You are a professional English news assistant. " +
            "Always respond in clean, structured English Markdown. " +
            "Rules:\n" +
            "- Open with: Based on [source] news for [date], the main highlights are:\n" +
            "- Use - **bold headings** for categories (e.g., **⚽ Sports**)\n" +
            "- Use emojis: 📰 general news, ⚽ sports, ⚠️ breaking/urgent, 🔊 audio, 🔗 sources\n" +
            "- Separate sections with --- (horizontal rule), use - bullet points\n" +
            "- ALWAYS include inline citations like ([Source](article_link)) after each claim\n" +
            "- Never output raw HTML, only Markdown";
    }

    private async Task<string?> CallAiApiAsync(string system, string user, int maxTokens = 800)
    {
        if (string.IsNullOrEmpty(_apiKey)) return null;

        try
        {
            var body = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = system },
                    new { role = "user", content = user }
                },
                max_tokens = maxTokens,
                temperature = 0.7
            };

            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        }
        catch
        {
            return null;
        }
    }

    private static string ExtractTextFromHtml(string html)
    {
        var clean = Regex.Replace(html, @"<script[^>]*>.*?</script>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        clean = Regex.Replace(clean, @"<style[^>]*>.*?</style>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        clean = Regex.Replace(clean, @"<header[^>]*>.*?</header>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        clean = Regex.Replace(clean, @"<nav[^>]*>.*?</nav>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        clean = Regex.Replace(clean, @"<footer[^>]*>.*?</footer>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var matches = Regex.Matches(clean, @"<p[^>]*>(.*?)</p>", RegexOptions.Singleline);
        var paragraphs = new List<string>();
        foreach (Match match in matches)
        {
            var text = Regex.Replace(match.Groups[1].Value, @"<[^>]+>", "");
            text = System.Net.WebUtility.HtmlDecode(text).Trim();
            if (text.Length > 20) paragraphs.Add(text);
        }
        return paragraphs.Count > 0 ? string.Join("\n\n", paragraphs) : "";
    }

    private static string FallbackSummary(List<ArticleEntity> articles, string lang)
    {
        var titles = articles.Select(a => $"• {a.FeedTitle}: {a.Title}").ToList();
        return lang == "ar"
            ? $"✨ التقرير اليومي ({articles.Count} مقال):\n\n{string.Join("\n", titles)}"
            : $"✨ Daily Report ({articles.Count} articles):\n\n{string.Join("\n", titles)}";
    }

    private static string FallbackOffline(string context, string lang)
    {
        return lang == "ar"
            ? $"✨ [غير متصل]: تعذر الوصول إلى خدمة الذكاء الاصطناعي.\n\nأحدث المقالات:\n\n{context.Truncate(500)}"
            : $"✨ [Offline]: Unable to reach AI service.\n\nRecent articles:\n\n{context.Truncate(500)}";
    }
}

file static class StringExtensions
{
    public static string Truncate(this string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";
}
