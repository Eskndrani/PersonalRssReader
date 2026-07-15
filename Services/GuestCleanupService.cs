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
        var cutoff = DateTime.UtcNow.AddHours(-24);

        var expiredAiUsage = await db.AiUsage
            .Where(u => u.UserId.StartsWith("guest-"))
            .ToListAsync(ct);
        if (expiredAiUsage.Count > 0)
        {
            db.AiUsage.RemoveRange(expiredAiUsage);
        }

        var expiredArticles = await db.Articles
            .Where(a => a.UserId == null && a.GuestSessionId != null && a.CreatedAt < cutoff)
            .ToListAsync(ct);
        if (expiredArticles.Count > 0)
        {
            db.Articles.RemoveRange(expiredArticles);
        }

        var expiredFeeds = await db.Feeds
            .Where(f => f.UserId == null && f.GuestSessionId != null && f.CreatedAt < cutoff)
            .ToListAsync(ct);
        if (expiredFeeds.Count > 0)
        {
            db.Feeds.RemoveRange(expiredFeeds);
        }

        await db.SaveChangesAsync(ct);

        var total = expiredFeeds.Count + expiredArticles.Count + expiredAiUsage.Count;
        if (total > 0)
            _logger.LogInformation("Cleaned up {Count} expired guest records", total);
    }
}
