using Microsoft.EntityFrameworkCore;

namespace PersonalRssReader.Services;

public interface IAiService
{
    Task<string> GenerateDailySummaryAsync(List<ArticleEntity> articles);
    Task<string> AskQuestionAsync(string question, string userId);
}

public sealed class AiService : IAiService
{
    private readonly AppDbContext _db;

    public AiService(AppDbContext db)
    {
        _db = db;
    }

    public Task<string> GenerateDailySummaryAsync(List<ArticleEntity> articles)
    {
        if (articles.Count == 0)
            return Task.FromResult("No articles found for today.");

        var titles = articles.Select(a => $"• {a.FeedTitle}: {a.Title}").ToList();
        var body = string.Join("\n", titles);

        return Task.FromResult(
            $"✨ AI Summary: Here is your briefing for today...\n\n{body}\n\nStay informed! 📰");
    }

    public async Task<string> AskQuestionAsync(string question, string userId)
    {
        var articles = await _db.Articles
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.PublishDate)
            .Take(20)
            .ToListAsync();

        var context = string.Join(" | ", articles.Select(a =>
            $"{a.FeedTitle}: {a.Title} ({a.PublishDate:yyyy-MM-dd})"));

        await Task.Delay(1200);

        return $"✨ [Simulated AI]: Based on your feeds, here is the answer to: \"{question}\"\n\n" +
               $"Context analyzed: {articles.Count} recent articles.\n" +
               $"Latest: {context[..Math.Min(context.Length, 300)]}...";
    }
}
