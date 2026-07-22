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
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));

        do
        {
            try
            {
                await RefreshFeedsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background feed refresh failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RefreshFeedsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var articleService = scope.ServiceProvider.GetRequiredService<FeedArticleService>();

        var now = DateTime.UtcNow;

        var subscriptions = await db.Feeds.ToListAsync(ct);

        var userKeys = subscriptions
            .Select(s => s.UserId ?? s.GuestSessionId)
            .Where(k => k != null)
            .Distinct()
            .ToList();

        var profiles = await db.UserProfiles
            .Where(p => userKeys.Contains(p.UserId) || userKeys.Contains(p.GuestSessionId))
            .ToListAsync(ct);

        var profileMap = profiles
            .SelectMany(p => new[]
            {
                (Key: p.UserId, Profile: p),
                (Key: p.GuestSessionId, Profile: p)
            })
            .Where(x => x.Key != null)
            .GroupBy(x => x.Key!)
            .ToDictionary(g => g.Key, g => g.First().Profile);

        var dueSubscriptions = subscriptions
            .Where(s =>
            {
                var key = s.UserId ?? s.GuestSessionId;
                if (key == null) return false;
                var interval = profileMap.TryGetValue(key, out var profile)
                    ? profile.RefreshIntervalMinutes
                    : 30;
                return s.LastRefreshedAt.AddMinutes(interval) <= now;
            })
            .ToList();

        if (dueSubscriptions.Count == 0) return;

        var urlGroups = dueSubscriptions
            .GroupBy(s => s.Url)
            .ToList();

        foreach (var group in urlGroups)
        {
            var url = group.Key;
            var subscribers = subscriptions
                .Where(s => s.Url == url)
                .ToList();

            try
            {
                var firstSub = subscribers[0];
                var articles = await articleService.FetchArticlesAsync(
                    url, firstSub.Title, firstSub.Username, firstSub.Password, ct);

                if (articles.Count == 0)
                {
                    foreach (var sub in dueSubscriptions.Where(s => s.Url == url))
                        sub.LastRefreshedAt = now;
                    continue;
                }

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
                        db.Articles.AddRange(newArticles);
                }

                foreach (var sub in subscribers)
                    sub.LastRefreshedAt = now;

                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to refresh feed {Url}", url);
            }
        }

        await CleanupOldArticlesAsync(db, subscriptions, profileMap, now, ct);
    }

    private static async Task CleanupOldArticlesAsync(
        AppDbContext db,
        List<FeedSubscription> subscriptions,
        Dictionary<string, UserProfile> profileMap,
        DateTime now,
        CancellationToken ct)
    {
        var userKeys = subscriptions
            .Select(s => s.UserId)
            .Where(u => u != null)
            .Cast<string>()
            .Distinct()
            .ToList();

        var guestKeys = subscriptions
            .Select(s => s.GuestSessionId)
            .Where(g => g != null)
            .Cast<string>()
            .Distinct()
            .Where(g => !userKeys.Contains(g))
            .ToList();

        foreach (var userId in userKeys)
        {
            var days = profileMap.TryGetValue(userId, out var profile)
                ? profile.KeepArticlesForDays
                : 30;
            var cutoff = now.AddDays(-days);
            var old = await db.Articles
                .Where(a => a.UserId == userId && a.PublishDate < cutoff && !a.IsBookmarked)
                .ToListAsync(ct);
            if (old.Count > 0) db.Articles.RemoveRange(old);
        }

        foreach (var guestId in guestKeys)
        {
            var days = profileMap.TryGetValue(guestId, out var profile)
                ? profile.KeepArticlesForDays
                : 30;
            var cutoff = now.AddDays(-days);
            var old = await db.Articles
                .Where(a => a.GuestSessionId == guestId && a.PublishDate < cutoff && !a.IsBookmarked)
                .ToListAsync(ct);
            if (old.Count > 0) db.Articles.RemoveRange(old);
        }

        await db.SaveChangesAsync(ct);
    }
}
