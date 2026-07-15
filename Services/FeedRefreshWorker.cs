using Microsoft.EntityFrameworkCore;

namespace PersonalRssReader.Services;

public sealed class FeedRefreshWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FeedRefreshWorker> _logger;

    public FeedRefreshWorker(IServiceScopeFactory scopeFactory, ILogger<FeedRefreshWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));

        do
        {
            try
            {
                await RefreshAllFeedsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background feed refresh failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RefreshAllFeedsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var articleService = scope.ServiceProvider.GetRequiredService<FeedArticleService>();

        var feedGroups = await db.Feeds
            .Select(f => new { f.Url, f.Title, f.Username, f.Password, f.UserId, f.GuestSessionId })
            .GroupBy(f => f.Url)
            .ToListAsync(ct);

        foreach (var group in feedGroups)
        {
            var url = group.Key;
            var subscribers = group.ToList();

            try
            {
                var articles = await articleService.FetchArticlesAsync(
                    url, subscribers[0].Title, subscribers[0].Username, subscribers[0].Password, ct);

                if (articles.Count == 0) continue;

                var uniqueArticles = articles
                    .GroupBy(a => a.Link)
                    .Select(g => g.First())
                    .ToList();

                foreach (var subscriber in subscribers)
                {
                    var existingLinks = await db.Articles
                        .Where(a => a.UserId == subscriber.UserId || a.GuestSessionId == subscriber.GuestSessionId)
                        .Select(a => a.Link)
                        .ToListAsync(ct);

                    var existingSet = new HashSet<string>(existingLinks);

                    var newArticles = uniqueArticles
                        .Where(a => !existingSet.Contains(a.Link))
                        .Select(a => new ArticleEntity
                        {
                            FeedTitle = a.FeedTitle,
                            Title = a.Title,
                            Link = a.Link,
                            PublishDate = a.PublishDate,
                            Summary = a.Summary,
                            ImageUrl = a.ImageUrl,
                            AudioUrl = a.AudioUrl,
                            UserId = subscriber.UserId,
                            GuestSessionId = subscriber.GuestSessionId
                        })
                        .ToList();

                    if (newArticles.Count > 0)
                    {
                        db.Articles.AddRange(newArticles);
                    }
                }

                await db.SaveChangesAsync(ct);

                var cutoff = DateTime.UtcNow.AddDays(-30);
                var oldArticles = await db.Articles
                    .Where(a => a.PublishDate < cutoff && !a.IsBookmarked)
                    .ToListAsync(ct);
                if (oldArticles.Count > 0)
                {
                    db.Articles.RemoveRange(oldArticles);
                    await db.SaveChangesAsync(ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to refresh feed {Url}", url);
            }
        }
    }
}
