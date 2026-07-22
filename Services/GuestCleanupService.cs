using Microsoft.EntityFrameworkCore;

namespace PersonalRssReader.Services;

public sealed class GuestCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GuestCleanupService> _logger;

    public GuestCleanupService(IServiceScopeFactory scopeFactory, ILogger<GuestCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
                await CleanupGuestDataAsync(stoppingToken);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Guest cleanup failed");
            }
        }
    }

    private async Task CleanupGuestDataAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var sessionCutoff = now.AddHours(-24);

        var expiredAiUsage = await db.AiUsage
            .Where(u => u.UserId.StartsWith("guest-"))
            .ToListAsync(ct);
        if (expiredAiUsage.Count > 0)
            db.AiUsage.RemoveRange(expiredAiUsage);

        var guestProfiles = await db.UserProfiles
            .Where(p => p.UserId == null && p.GuestSessionId != null)
            .ToListAsync(ct);
        var profileMap = guestProfiles.ToDictionary(p => p.GuestSessionId!);

        var guestArticles = await db.Articles
            .Where(a => a.UserId == null && a.GuestSessionId != null && !a.IsBookmarked)
            .ToListAsync(ct);

        var articlesToRemove = guestArticles
            .Where(a =>
            {
                var days = profileMap.TryGetValue(a.GuestSessionId!, out var profile)
                    ? profile.KeepArticlesForDays
                    : 1;
                var cutoff = now.AddDays(-days);
                return a.PublishDate < cutoff;
            })
            .ToList();

        if (articlesToRemove.Count > 0)
            db.Articles.RemoveRange(articlesToRemove);

        var expiredFeeds = await db.Feeds
            .Where(f => f.UserId == null && f.GuestSessionId != null && f.CreatedAt < sessionCutoff)
            .ToListAsync(ct);
        if (expiredFeeds.Count > 0)
            db.Feeds.RemoveRange(expiredFeeds);

        await db.SaveChangesAsync(ct);

        var total = expiredFeeds.Count + articlesToRemove.Count + expiredAiUsage.Count;
        if (total > 0)
            _logger.LogInformation("Cleaned up {Count} expired guest records", total);
    }
}
