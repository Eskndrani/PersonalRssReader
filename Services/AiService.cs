namespace PersonalRssReader.Services;

public interface IAiService
{
    Task<string> GenerateDailySummaryAsync(List<ArticleEntity> articles);
}

public sealed class AiService : IAiService
{
    public Task<string> GenerateDailySummaryAsync(List<ArticleEntity> articles)
    {
        if (articles.Count == 0)
            return Task.FromResult("No articles found for today.");

        var titles = articles.Select(a => $"• {a.FeedTitle}: {a.Title}").ToList();
        var body = string.Join("\n", titles);

        return Task.FromResult(
            $"✨ AI Summary: Here is your briefing for today...\n\n{body}\n\nStay informed! 📰");
    }
}
