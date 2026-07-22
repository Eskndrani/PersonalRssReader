using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace PersonalRssReader.Services;

public sealed class DailyDigestService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailyDigestService> _logger;
    private DateTime _lastDigestDate = DateTime.MinValue;

    public DailyDigestService(IServiceScopeFactory scopeFactory, ILogger<DailyDigestService> logger)
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
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                var today = DateTime.UtcNow.Date;
                if (_lastDigestDate >= today) continue;
                _lastDigestDate = today;
                await SendDailyDigestsAsync(stoppingToken);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Daily digest service failed");
            }
        }
    }

    private async Task SendDailyDigestsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var profiles = await db.UserProfiles
            .Where(p => p.EmailFavoriteFeeds && p.UserId != null)
            .ToListAsync(ct);

        var since = DateTime.UtcNow.AddHours(-24);

        foreach (var profile in profiles)
        {
            try
            {
                var user = await userManager.FindByIdAsync(profile.UserId!);
                if (user?.Email is null || !await userManager.IsEmailConfirmedAsync(user))
                    continue;

                var favoriteFeedTitles = await db.Feeds
                    .Where(f => f.UserId == profile.UserId && f.IsFavorite)
                    .Select(f => f.Title)
                    .ToListAsync(ct);

                if (favoriteFeedTitles.Count == 0) continue;

                var articles = await db.Articles
                    .Where(a => a.UserId == profile.UserId
                        && a.PublishDate >= since
                        && favoriteFeedTitles.Contains(a.FeedTitle))
                    .OrderByDescending(a => a.PublishDate)
                    .Take(20)
                    .Select(a => new DailyDigestArticle(a.FeedTitle, a.Title, a.Link, a.Summary))
                    .ToListAsync(ct);

                if (articles.Count == 0) continue;

                await emailService.SendDailyDigestAsync(user.Email!, articles);
                _logger.LogInformation("Sent daily digest to {Email} with {Count} articles", user.Email, articles.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send digest for user {UserId}", profile.UserId);
            }
        }
    }
}
