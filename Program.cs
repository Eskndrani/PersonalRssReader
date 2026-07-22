using System.Security.Claims;
using System.Text.Json;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PersonalRssReader.Services;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=articles.db"));

builder.Services.AddIdentityApiEndpoints<IdentityUser>(options =>
{
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
        options.SignInScheme = IdentityConstants.ApplicationScheme;

        options.Events = new OAuthEvents
        {
            OnTicketReceived = async ctx =>
            {
                var userManager = ctx.HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
                var email = ctx.Principal?.FindFirst(ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(email)) return;

                var existingUser = await userManager.FindByEmailAsync(email);
                if (existingUser is null)
                {
                    var newUser = new IdentityUser
                    {
                        UserName = email,
                        Email = email,
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(newUser);
                    if (!result.Succeeded)
                    {
                        foreach (var err in result.Errors)
                            Console.WriteLine($"[IDENTITY ERROR Google] {err.Code}: {err.Description}");
                    }
                }

                if (ctx.Principal?.Identity is ClaimsIdentity identity)
                {
                    var nameClaim = identity.FindFirst(ClaimTypes.Name);
                    if (nameClaim is not null)
                        identity.RemoveClaim(nameClaim);
                    identity.AddClaim(new Claim(ClaimTypes.Name, email));
                }
            }
        };
        options.Events.OnRemoteFailure = ctx =>
        {
            ctx.Response.Redirect("/welcome.html?error=oauth_failed");
            ctx.HandleResponse();
            return Task.CompletedTask;
        };
    });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.LoginPath = "/welcome.html";
});

builder.Services.AddAuthorization();

builder.Services.AddHttpClient("FeedReader", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
    client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
    client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
    client.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
    client.DefaultRequestHeaders.Add("Referer", "https://www.google.com/");
    client.DefaultRequestVersion = new Version(2, 0);
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = true,
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

builder.Services.AddScoped<FeedArticleService>();
builder.Services.AddScoped<IAiService, AiService>();
builder.Services.AddHostedService<FeedRefreshWorker>();
builder.Services.AddHostedService<GuestCleanupService>();
builder.Services.AddHostedService<DailyDigestService>();
builder.Services.AddSingleton<IEmailService, SmtpEmailService>();

builder.Services.AddHttpClient("AiClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("AiEndpointPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? context.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"error\":\"Too many requests. Please slow down.\"}", ct);
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    db.Database.ExecuteSqlRaw(
        "CREATE TABLE IF NOT EXISTS UserProfiles (Id TEXT PRIMARY KEY, UserId TEXT, GuestSessionId TEXT, DisplayName TEXT, Bio TEXT, ProfilePictureUrl TEXT, CoverPhotoUrl TEXT, SocialLinks TEXT, KeepArticlesForDays INTEGER NOT NULL DEFAULT 30, RefreshIntervalMinutes INTEGER NOT NULL DEFAULT 30, EmailFavoriteFeeds INTEGER NOT NULL DEFAULT 0)");

    var existingColumns = new HashSet<string>();
    using (var cmd = db.Database.GetDbConnection().CreateCommand())
    {
        cmd.CommandText = "PRAGMA table_info(UserProfiles)";
        db.Database.GetDbConnection().Open();
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read()) existingColumns.Add(reader.GetString(1));
        }
        db.Database.GetDbConnection().Close();
    }

    if (!existingColumns.Contains("KeepArticlesForDays"))
        db.Database.ExecuteSqlRaw("ALTER TABLE UserProfiles ADD COLUMN KeepArticlesForDays INTEGER NOT NULL DEFAULT 30");
    if (!existingColumns.Contains("RefreshIntervalMinutes"))
        db.Database.ExecuteSqlRaw("ALTER TABLE UserProfiles ADD COLUMN RefreshIntervalMinutes INTEGER NOT NULL DEFAULT 30");
    if (!existingColumns.Contains("EmailFavoriteFeeds"))
        db.Database.ExecuteSqlRaw("ALTER TABLE UserProfiles ADD COLUMN EmailFavoriteFeeds INTEGER NOT NULL DEFAULT 0");

    var feedColumns = new HashSet<string>();
    using (var feedCmd = db.Database.GetDbConnection().CreateCommand())
    {
        feedCmd.CommandText = "PRAGMA table_info(Feeds)";
        db.Database.GetDbConnection().Open();
        using (var reader = feedCmd.ExecuteReader())
        {
            while (reader.Read()) feedColumns.Add(reader.GetString(1));
        }
        db.Database.GetDbConnection().Close();
    }
    if (!feedColumns.Contains("LastRefreshedAt"))
        db.Database.ExecuteSqlRaw("ALTER TABLE Feeds ADD COLUMN LastRefreshedAt TEXT NOT NULL DEFAULT '0001-01-01T00:00:00'");

    db.Database.ExecuteSqlRaw(
        "CREATE TABLE IF NOT EXISTS CommunityPosts (Id TEXT PRIMARY KEY, Content TEXT NOT NULL, MediaUrl TEXT, AuthorId TEXT NOT NULL, AuthorName TEXT, CreatedAt TEXT NOT NULL)");
    db.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS PostReactions");
    db.Database.ExecuteSqlRaw(
        "CREATE TABLE IF NOT EXISTS PostReactions (Id INTEGER PRIMARY KEY AUTOINCREMENT, PostId TEXT NOT NULL, UserId TEXT NOT NULL, ReactionType TEXT NOT NULL, FOREIGN KEY (PostId) REFERENCES CommunityPosts(Id) ON DELETE CASCADE)");
    db.Database.ExecuteSqlRaw(
        "CREATE TABLE IF NOT EXISTS UserBlocks (BlockerId TEXT NOT NULL, BlockedId TEXT NOT NULL, PRIMARY KEY (BlockerId, BlockedId))");
    db.Database.ExecuteSqlRaw(
        "CREATE TABLE IF NOT EXISTS PostComments (Id INTEGER PRIMARY KEY AUTOINCREMENT, PostId TEXT NOT NULL, AuthorId TEXT NOT NULL, AuthorName TEXT, Content TEXT NOT NULL, CreatedAt TEXT NOT NULL, FOREIGN KEY (PostId) REFERENCES CommunityPosts(Id) ON DELETE CASCADE)");
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseDeveloperExceptionPage();

app.MapGet("/", () => Results.Redirect("/index.html"));

app.MapIdentityApi<IdentityUser>();

static string GetUserKey(HttpContext http)
{
    var uid = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!string.IsNullOrEmpty(uid)) return uid;
    return "guest-" + (http.Request.Headers["X-Guest-Session"].FirstOrDefault() ?? "anon");
}

app.MapGet("/api/auth/me", async (HttpContext http) =>
{
    var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null) return Results.Ok(new { userId = "", email = "", isGuest = true });
    return Results.Ok(new { userId, email = http.User.Identity?.Name, isGuest = false });
});

app.MapGet("/api/quota", async (AppDbContext db, HttpContext http) =>
{
    var (userId, limit, _) = GetQuotaParams(http);
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var usage = await db.AiUsage.FirstOrDefaultAsync(u => u.UserId == userId && u.Date == today);
    var nextReset = DateTime.UtcNow.Date.AddDays(1);
    return Results.Ok(new { used = usage?.RequestCount ?? 0, limit, nextReset = nextReset.ToString("o") });
});

app.MapGet("/api/settings", async (AppDbContext db, HttpContext http) =>
{
    try
    {
        var key = GetUserKey(http);
        var isGuest = http.User.FindFirstValue(ClaimTypes.NameIdentifier) is null;

        var profile = await db.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == key || p.GuestSessionId == key);

        if (profile is null)
        {
            profile = new UserProfile
            {
                Id = Guid.NewGuid().ToString(),
                DisplayName = isGuest ? "Guest" : http.User.Identity?.Name
            };
            if (isGuest) profile.GuestSessionId = key;
            else profile.UserId = key;
            db.UserProfiles.Add(profile);
            await db.SaveChangesAsync();
        }

        var blocked = await db.UserBlocks
            .Where(b => b.BlockerId == key)
            .Select(b => new { b.BlockedId })
            .ToListAsync();

        return Results.Ok(new
        {
            profile.Id,
            profile.DisplayName,
            profile.Bio,
            profile.ProfilePictureUrl,
            profile.CoverPhotoUrl,
            profile.SocialLinks,
            profile.KeepArticlesForDays,
            profile.RefreshIntervalMinutes,
            profile.EmailFavoriteFeeds,
            BlockedUsers = blocked
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = "Could not load settings.", detail = ex.Message }, statusCode: 500);
    }
});

app.MapPut("/api/settings", async (UpdateProfileDto dto, AppDbContext db, HttpContext http) =>
{
    try
    {
        var key = GetUserKey(http);
        var profile = await db.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == key || p.GuestSessionId == key);

        if (profile is null) return Results.Json(new { error = "Settings not found." }, statusCode: 404);

        var isGuest = http.User.FindFirstValue(ClaimTypes.NameIdentifier) is null;

        if (!isGuest)
        {
            if (dto.DisplayName is not null) profile.DisplayName = dto.DisplayName;
            if (dto.ProfilePictureUrl is not null) profile.ProfilePictureUrl = dto.ProfilePictureUrl;
            if (dto.EmailFavoriteFeeds.HasValue) profile.EmailFavoriteFeeds = dto.EmailFavoriteFeeds.Value;
        }
        if (dto.Bio is not null) profile.Bio = dto.Bio;
        if (dto.CoverPhotoUrl is not null) profile.CoverPhotoUrl = dto.CoverPhotoUrl;
        if (dto.SocialLinks is not null) profile.SocialLinks = dto.SocialLinks;
        if (dto.KeepArticlesForDays.HasValue) profile.KeepArticlesForDays = dto.KeepArticlesForDays.Value;
        if (dto.RefreshIntervalMinutes.HasValue) profile.RefreshIntervalMinutes = dto.RefreshIntervalMinutes.Value;

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            profile.Id,
            profile.DisplayName,
            profile.Bio,
            profile.ProfilePictureUrl,
            profile.CoverPhotoUrl,
            profile.SocialLinks,
            profile.KeepArticlesForDays,
            profile.RefreshIntervalMinutes,
            profile.EmailFavoriteFeeds
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { error = "Could not save settings.", detail = ex.Message }, statusCode: 500);
    }
});

app.MapGet("/api/profile/insights", async (
    AppDbContext db, IAiService ai, HttpContext http,
    [FromQuery] string lang = "en") =>
{
    var key = GetUserKey(http);
    var readArticles = await db.Articles
        .Where(a => (a.UserId == key || a.GuestSessionId == key) && a.IsRead)
        .OrderByDescending(a => a.PublishDate)
        .Take(50)
        .ToListAsync();

    var totalRead = await db.Articles
        .CountAsync(a => (a.UserId == key || a.GuestSessionId == key) && a.IsRead);

    var totalBookmarks = await db.Articles
        .CountAsync(a => (a.UserId == key || a.GuestSessionId == key) && a.IsBookmarked);

    var subscribedUrls = await db.Feeds
        .Where(f => f.UserId == key || f.GuestSessionId == key)
        .Select(f => f.Url)
        .ToListAsync();

    var insights = await ai.GenerateReadingInsightsAsync(readArticles, totalRead, totalBookmarks, subscribedUrls, lang);

    return Results.Ok(new { readingPersona = insights.Persona, recommendedFeeds = insights.Recommendations, totalRead, totalBookmarks });
});

app.MapGet("/api/community/posts", async (AppDbContext db, HttpContext http) =>
{
    var key = GetUserKey(http);
    var blockedIds = await db.UserBlocks
        .Where(b => b.BlockerId == key)
        .Select(b => b.BlockedId)
        .ToListAsync();

    var blockedByIds = await db.UserBlocks
        .Where(b => b.BlockedId == key)
        .Select(b => b.BlockerId)
        .ToListAsync();

    var allBlocked = blockedIds.Concat(blockedByIds).Distinct().ToList();

    var posts = await db.CommunityPosts
        .Include(p => p.Reactions)
        .Include(p => p.Comments)
        .Where(p => !allBlocked.Contains(p.AuthorId))
        .OrderByDescending(p => p.CreatedAt)
        .Take(50)
        .ToListAsync();

    foreach (var post in posts)
    {
        post.Comments = post.Comments
            .Where(c => !allBlocked.Contains(c.AuthorId))
            .OrderBy(c => c.CreatedAt)
            .ToList();
    }

    var result = posts.Select(p => new
    {
        p.Id,
        p.Content,
        p.MediaUrl,
        p.AuthorId,
        p.AuthorName,
        p.CreatedAt,
        ReactionCounts = p.Reactions.GroupBy(r => r.ReactionType)
            .Select(g => new { Type = g.Key, Count = g.Count() }).ToList(),
        Comments = p.Comments.OrderBy(c => c.CreatedAt).Select(c => new
        {
            c.Id,
            c.AuthorId,
            c.AuthorName,
            c.Content,
            c.CreatedAt
        }).ToList()
    }).ToList();

    return Results.Ok(result);
});

app.MapPost("/api/community/posts", async (CreatePostDto dto, AppDbContext db, HttpContext http) =>
{
    var key = GetUserKey(http);
    if (string.IsNullOrWhiteSpace(dto.Content))
        return Results.BadRequest(new { error = "Content is required." });

    var isGuest = http.User.FindFirstValue(ClaimTypes.NameIdentifier) is null;
    if (isGuest)
    {
        var guestPostCount = await db.CommunityPosts.CountAsync(p => p.AuthorId == key);
        if (guestPostCount >= 3)
            return Results.BadRequest(new { error = "Guests are limited to 3 posts. Please create an account to post more." });
    }

    var profile = await db.UserProfiles
        .FirstOrDefaultAsync(p => p.UserId == key || p.GuestSessionId == key);
    var authorName = profile?.DisplayName
        ?? (isGuest ? "Guest-" + (http.Request.Headers["X-Guest-Session"].FirstOrDefault() ?? "anon").Substring(0, Math.Min(6, (http.Request.Headers["X-Guest-Session"].FirstOrDefault() ?? "anon").Length))
            : http.User.Identity?.Name ?? "User");

    var post = new CommunityPost
    {
        Id = Guid.NewGuid().ToString(),
        Content = dto.Content.Trim(),
        MediaUrl = dto.MediaUrl,
        AuthorId = key,
        AuthorName = authorName,
        CreatedAt = DateTime.UtcNow
    };

    db.CommunityPosts.Add(post);
    await db.SaveChangesAsync();

    return Results.Created($"/api/community/posts/{post.Id}", new
    {
        post.Id,
        post.Content,
        post.MediaUrl,
        post.AuthorId,
        post.AuthorName,
        post.CreatedAt,
        ReactionCounts = new List<object>(),
        CurrentUserReaction = (string?)null
    });
});

app.MapDelete("/api/community/posts/{id}", async (string id, AppDbContext db, HttpContext http) =>
{
    var key = GetUserKey(http);
    var post = await db.CommunityPosts.FirstOrDefaultAsync(p => p.Id == id && p.AuthorId == key);
    if (post is null) return Results.NotFound();

    db.CommunityPosts.Remove(post);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapPut("/api/community/posts/{id}", async (string id, CreatePostDto dto, AppDbContext db, HttpContext http) =>
{
    var key = GetUserKey(http);
    var post = await db.CommunityPosts.FirstOrDefaultAsync(p => p.Id == id && p.AuthorId == key);
    if (post is null) return Results.NotFound();

    if (string.IsNullOrWhiteSpace(dto.Content))
        return Results.BadRequest(new { error = "Content is required." });

    post.Content = dto.Content.Trim();
    await db.SaveChangesAsync();

    return Results.Ok(new { post.Id, post.Content });
});

app.MapPost("/api/community/posts/{id}/react", async (string id, ReactDto dto, AppDbContext db, HttpContext http) =>
{
    var key = GetUserKey(http);
    if (string.IsNullOrWhiteSpace(dto.ReactionType))
        return Results.BadRequest(new { error = "Reaction type is required." });

    var post = await db.CommunityPosts.FirstOrDefaultAsync(p => p.Id == id);
    if (post is null) return Results.NotFound();

    var existing = await db.PostReactions
        .FirstOrDefaultAsync(r => r.PostId == id && r.UserId == key && r.ReactionType == dto.ReactionType);

    if (existing is not null)
    {
        db.PostReactions.Remove(existing);
        await db.SaveChangesAsync();
    }
    else
    {
        db.PostReactions.Add(new PostReaction { PostId = id, UserId = key, ReactionType = dto.ReactionType });
        await db.SaveChangesAsync();
    }

    var counts = await db.PostReactions
        .Where(r => r.PostId == id)
        .GroupBy(r => r.ReactionType)
        .Select(g => new { Type = g.Key, Count = g.Count() })
        .ToListAsync();

    return Results.Ok(new { reactionCounts = counts });
});

app.MapPost("/api/users/{blockedId}/block", async (string blockedId, AppDbContext db, HttpContext http) =>
{
    var key = GetUserKey(http);
    if (key == blockedId)
        return Results.BadRequest(new { error = "You cannot block yourself." });

    var exists = await db.UserBlocks.AnyAsync(b => b.BlockerId == key && b.BlockedId == blockedId);
    if (exists)
        return Results.Ok(new { blocked = true });

    db.UserBlocks.Add(new UserBlock { BlockerId = key, BlockedId = blockedId });
    await db.SaveChangesAsync();

    return Results.Ok(new { blocked = true });
});

app.MapPost("/api/community/posts/{id}/comments", async (string id, CreatePostDto dto, AppDbContext db, HttpContext http) =>
{
    var key = GetUserKey(http);
    if (string.IsNullOrWhiteSpace(dto.Content))
        return Results.BadRequest(new { error = "Comment content is required." });

    var post = await db.CommunityPosts.FirstOrDefaultAsync(p => p.Id == id);
    if (post is null) return Results.NotFound();

    var isGuest = http.User.FindFirstValue(ClaimTypes.NameIdentifier) is null;
    var profile = await db.UserProfiles
        .FirstOrDefaultAsync(p => p.UserId == key || p.GuestSessionId == key);
    var authorName = profile?.DisplayName
        ?? (isGuest ? "Guest-" + (http.Request.Headers["X-Guest-Session"].FirstOrDefault() ?? "anon").Substring(0, Math.Min(6, (http.Request.Headers["X-Guest-Session"].FirstOrDefault() ?? "anon").Length))
            : http.User.Identity?.Name ?? "User");

    var comment = new PostComment
    {
        PostId = id,
        AuthorId = key,
        AuthorName = authorName,
        Content = dto.Content.Trim(),
        CreatedAt = DateTime.UtcNow
    };

    db.PostComments.Add(comment);
    await db.SaveChangesAsync();

    return Results.Created($"/api/community/posts/{id}/comments/{comment.Id}", new
    {
        comment.Id,
        comment.AuthorId,
        comment.AuthorName,
        comment.Content,
        comment.CreatedAt
    });
});

app.MapDelete("/api/users/{blockedId}/block", async (string blockedId, AppDbContext db, HttpContext http) =>
{
    var key = GetUserKey(http);
    var block = await db.UserBlocks.FirstOrDefaultAsync(b => b.BlockerId == key && b.BlockedId == blockedId);
    if (block is null) return Results.NotFound();

    db.UserBlocks.Remove(block);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapGet("/api/users/blocked", async (AppDbContext db, HttpContext http) =>
{
    var key = GetUserKey(http);
    var blocked = await db.UserBlocks
        .Where(b => b.BlockerId == key)
        .Select(b => new { b.BlockedId })
        .ToListAsync();
    return Results.Ok(blocked);
});

app.MapPost("/api/auth/logout", async (SignInManager<IdentityUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok();
}).RequireAuthorization();

app.MapGet("/api/auth/google-login", () =>
{
    var properties = new AuthenticationProperties { RedirectUri = "/index.html" };
    return Results.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
});

app.MapPost("/api/auth/register", async (
    RegisterRequest req, UserManager<IdentityUser> userManager,
    IEmailService emailService, HttpContext http) =>
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest("Email and password are required.");

    var user = new IdentityUser { UserName = req.Email, Email = req.Email };
    var result = await userManager.CreateAsync(user, req.Password);
    if (!result.Succeeded)
    {
        var errors = result.Errors.Select(e => e.Description);
        return Results.BadRequest(new { errors = errors });
    }

    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
    var encodedToken = Uri.EscapeDataString(token);
    var userId = user.Id;
    var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";
    var url = $"{baseUrl}/verify.html?userId={userId}&token={encodedToken}";

    await emailService.SendVerificationEmailAsync(req.Email, user.Id, token, baseUrl);

    return Results.Ok(new { message = "Account created. Check your email for the verification link.", userId, token = encodedToken });
});

app.MapPost("/api/auth/signup", async (
    RegisterRequest req, UserManager<IdentityUser> userManager,
    IEmailService emailService, HttpContext http) =>
{
    // alias — delegate to register
    return await new Func<Task<IResult>>(async () =>
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return Results.BadRequest("Email and password are required.");
        var user = new IdentityUser { UserName = req.Email, Email = req.Email };
        var result = await userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";
        var url = $"{baseUrl}/verify.html?userId={user.Id}&token={encodedToken}";
        await emailService.SendVerificationEmailAsync(req.Email, user.Id, token, baseUrl);
        return Results.Ok(new { message = "Account created. Check your email (or terminal) for the verification link.", userId = user.Id, token = encodedToken, url });
    })();
});

app.MapGet("/api/auth/login", async (
    [FromQuery] string email, [FromQuery] string password, [FromQuery] bool useCookies,
    SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager) =>
{
    var user = await userManager.FindByEmailAsync(email);
    if (user is null) return Results.Unauthorized();
    if (!await userManager.IsEmailConfirmedAsync(user))
        return Results.Json(new { error = "Please verify your email before logging in." }, statusCode: 403);

    var result = await signInManager.PasswordSignInAsync(user, password, true, false);
    if (!result.Succeeded) return Results.Unauthorized();

    return Results.Ok(new { message = "Login successful." });
});

app.MapGet("/api/auth/verify-email", async (
    [FromQuery] string userId, [FromQuery] string token,
    UserManager<IdentityUser> userManager) =>
{
    var user = await userManager.FindByIdAsync(userId);
    if (user is null) return Results.NotFound(new { error = "User not found." });

    var result = await userManager.ConfirmEmailAsync(user, Uri.UnescapeDataString(token));
    if (!result.Succeeded) return Results.BadRequest(new { error = "Invalid or expired verification token." });

    return Results.Ok(new { message = "Email verified successfully." });
});

app.MapPost("/api/auth/resend-verification", async (
    ResendRequest req, UserManager<IdentityUser> userManager,
    IEmailService emailService, HttpContext http) =>
{
    var user = await userManager.FindByEmailAsync(req.Email);
    if (user is null || user.EmailConfirmed)
        return Results.Ok(new { message = "If the account exists and is unverified, a new link has been sent." });

    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
    var encodedToken = Uri.EscapeDataString(token);
    var baseUrl = $"{http.Request.Scheme}://{http.Request.Host}";
    var url = $"{baseUrl}/verify.html?userId={user.Id}&token={encodedToken}";

    await emailService.SendVerificationEmailAsync(req.Email, user.Id, token, baseUrl);

    return Results.Ok(new { message = "A new verification link has been sent to your email." });
});

(string Title, string Url, string Category)[] DefaultFeeds = {
    ("Hacker News", "https://news.ycombinator.com/rss", "Tech & Engineering"),
    ("Herb Sutter on Software", "https://herbsutter.com/feed/", "Tech & Engineering"),
    ("Hackaday", "https://hackaday.com/blog/feed/", "Tech & Engineering"),
    ("Arduino Blog", "https://blog.arduino.cc/feed/", "Tech & Engineering"),
    ("BBC Arabic", "http://feeds.bbci.co.uk/arabic/rss.xml", "Global News"),
    ("Elbalad News", "https://www.elbalad.news/rss.aspx", "Global News")
};

async Task SeedDefaultFeedsAsync(AppDbContext db, string? userId, string? guestSessionId)
{
    var categories = DefaultFeeds.Select(f => f.Category).Distinct();
    var playlistMap = new Dictionary<string, string>();
    foreach (var cat in categories)
    {
        var playlist = new Playlist
        {
            Id = Guid.NewGuid().ToString(),
            Name = cat,
            UserId = userId,
            GuestSessionId = guestSessionId
        };
        db.Playlists.Add(playlist);
        playlistMap[cat] = playlist.Id;
    }

    foreach (var (title, url, category) in DefaultFeeds)
    {
        db.Feeds.Add(new FeedSubscription
        {
            Id = Guid.NewGuid().ToString(),
            Url = url,
            Title = title,
            UserId = userId,
            GuestSessionId = guestSessionId,
            PlaylistId = playlistMap[category],
            FaviconUrl = GetFaviconUrl(url)
        });
    }
    await db.SaveChangesAsync();
}

app.MapGet("/api/feeds", async (AppDbContext db, HttpContext http, FeedArticleService articleService) =>
{
    var key = GetUserKey(http);
    var isGuest = http.User.FindFirstValue(ClaimTypes.NameIdentifier) is null;

    var feedCount = await db.Feeds.CountAsync(f => f.UserId == key || f.GuestSessionId == key);
    if (isGuest && feedCount == 0)
        await SeedDefaultFeedsAsync(db, null, key);

    var feeds = await db.Feeds.Where(f => f.UserId == key || f.GuestSessionId == key).OrderBy(f => f.Title)
        .Select(f => new { f.Id, f.Url, f.Title, f.Username, f.Password, f.IsFavorite, f.FaviconUrl, f.PlaylistId, ArticleCount = db.Articles.Count(a => (a.UserId == key || a.GuestSessionId == key) && a.FeedTitle == f.Title && !a.IsRead) })
        .ToListAsync();
    return Results.Ok(feeds);
});

app.MapPost("/api/feeds", async (
    FeedDto dto, AppDbContext db, FeedArticleService articleService,
    HttpContext http, CancellationToken ct) =>
{
    var key = GetUserKey(http);
    var isGuest = http.User.FindFirstValue(ClaimTypes.NameIdentifier) is null;
    var normalizedUrl = NormalizeUrl(dto.Url);

    var userFeedUrls = await db.Feeds
        .Where(f => (f.UserId == key || f.GuestSessionId == key))
        .Select(f => f.Url)
        .ToListAsync(ct);
    if (userFeedUrls.Any(u => NormalizeUrl(u) == normalizedUrl))
        return Results.Conflict(new { message = "This feed is already in your subscriptions." });

    string title;
    try
    {
        title = await articleService.FetchFeedTitleAsync(dto.Url, dto.Username, dto.Password, ct);
    }
    catch (FeedFetchException ex)
    {
        return Results.BadRequest(ex.Message);
    }

    var feed = new FeedSubscription
    {
        Id = Guid.NewGuid().ToString(),
        Url = dto.Url,
        Title = title,
        Username = dto.Username,
        Password = dto.Password,
        PlaylistId = dto.PlaylistId,
        FaviconUrl = GetFaviconUrl(dto.Url)
    };
    if (isGuest) feed.GuestSessionId = key;
    else feed.UserId = key;

    db.Feeds.Add(feed);
    await db.SaveChangesAsync();

    try
    {
        var articles = await articleService.FetchArticlesAsync(dto.Url, title, dto.Username, dto.Password, ct);
        if (articles.Count > 0)
        {
            var existingLinks = await db.Articles
                .Where(a => a.UserId == key || a.GuestSessionId == key)
                .Select(a => a.Link)
                .ToListAsync(ct);
            var existingSet = new HashSet<string>(existingLinks);
            var newArticles = articles
                .Where(a => !existingSet.Contains(a.Link))
                .ToList();
            foreach (var a in newArticles)
            {
                a.UserId = isGuest ? null : key;
                a.GuestSessionId = isGuest ? key : null;
            }
            if (newArticles.Count > 0)
            {
                db.Articles.AddRange(newArticles);
                await db.SaveChangesAsync(ct);
            }
        }
    }
    catch (Exception) { }

    return Results.Created($"/api/feeds/{feed.Id}", feed);
});

app.MapPost("/api/feeds/batch", async (
    BatchFeedDto dto, AppDbContext db, FeedArticleService articleService,
    HttpContext http, CancellationToken ct) =>
{
    var key = GetUserKey(http);
    var isGuest = http.User.FindFirstValue(ClaimTypes.NameIdentifier) is null;
    var existingUrls = await db.Feeds.Where(f => f.UserId == key || f.GuestSessionId == key).Select(f => f.Url).ToListAsync();
    var existingSet = new HashSet<string>(existingUrls.Select(u => NormalizeUrl(u)));

    var newUrls = dto.Urls
        .Select(u => NormalizeUrl(u))
        .Where(u => !string.IsNullOrEmpty(u) && !existingSet.Contains(u))
        .Distinct()
        .ToArray();

    if (newUrls.Length == 0)
        return Results.Ok(new { Added = 0, Failed = 0 });

    var semaphore = new SemaphoreSlim(10);
    var added = 0;
    var failed = 0;

    var tasks = newUrls.Select(async url =>
    {
        await semaphore.WaitAsync();
        try
        {
            var title = await articleService.FetchFeedTitleAsync(url, dto.Username, dto.Password, ct);
                var feed = new FeedSubscription
                {
                    Id = Guid.NewGuid().ToString(),
                    Url = url,
                    Title = title,
                    Username = dto.Username,
                    Password = dto.Password,
                    PlaylistId = dto.PlaylistId,
                    FaviconUrl = GetFaviconUrl(url)
                };
            if (isGuest) feed.GuestSessionId = key;
            else feed.UserId = key;
            db.Feeds.Add(feed);
            Interlocked.Increment(ref added);
        }
        catch (Exception)
        {
            Interlocked.Increment(ref failed);
        }
        finally
        {
            semaphore.Release();
        }
    });

    await Task.WhenAll(tasks);

    if (added > 0)
        await db.SaveChangesAsync();

    if (added > 0)
    {
        var newFeeds = await db.Feeds
            .Where(f => (f.UserId == key || f.GuestSessionId == key))
            .OrderByDescending(f => f.CreatedAt)
            .Take(added)
            .ToListAsync(ct);
        foreach (var nf in newFeeds)
        {
            try
            {
                var articles = await articleService.FetchArticlesAsync(nf.Url, nf.Title, nf.Username, nf.Password, ct);
                if (articles.Count > 0)
                {
                    var existingLinks = await db.Articles
                        .Where(a => a.UserId == key || a.GuestSessionId == key)
                        .Select(a => a.Link)
                        .ToListAsync(ct);
                    var syncExistingSet = new HashSet<string>(existingLinks);
                    var newArticles = articles
                        .Where(a => !syncExistingSet.Contains(a.Link))
                        .ToList();
                    foreach (var a in newArticles)
                    {
                        a.UserId = isGuest ? null : key;
                        a.GuestSessionId = isGuest ? key : null;
                    }
                    if (newArticles.Count > 0)
                    {
                        db.Articles.AddRange(newArticles);
                        await db.SaveChangesAsync(ct);
                    }
                }
            }
            catch (Exception) { }
        }
    }

    return Results.Ok(new { Added = added, Failed = failed });
});

app.MapPost("/api/feeds/seed-defaults", async (AppDbContext db, HttpContext http) =>
{
    var key = GetUserKey(http);
    var isGuest = http.User.FindFirstValue(ClaimTypes.NameIdentifier) is null;

    var feedCount = await db.Feeds.CountAsync(f => f.UserId == key || f.GuestSessionId == key);
    if (feedCount > 0)
        return Results.BadRequest(new { error = "You already have subscribed feeds." });

    if (isGuest)
        await SeedDefaultFeedsAsync(db, null, key);
    else
        await SeedDefaultFeedsAsync(db, key, null);

    return Results.Ok(new { seeded = true });
});

app.MapPatch("/api/feeds/{id}/favorite", async (string id, AppDbContext db, HttpContext http) =>
{
    var key = GetUserKey(http);
    var feed = await db.Feeds.FirstOrDefaultAsync(f => f.Id == id && (f.UserId == key || f.GuestSessionId == key));
    if (feed is null) return Results.NotFound();

    feed.IsFavorite = !feed.IsFavorite;
    await db.SaveChangesAsync();

    return Results.Ok(feed);
});

app.MapDelete("/api/feeds/{id}", async (string id, AppDbContext db, HttpContext http) =>
{
    var key = GetUserKey(http);
    var feed = await db.Feeds.FirstOrDefaultAsync(f => f.Id == id && (f.UserId == key || f.GuestSessionId == key));
    if (feed is null) return Results.NotFound();

    db.Feeds.Remove(feed);

    var articlesToDelete = await db.Articles
        .Where(a => (a.UserId == key || a.GuestSessionId == key) && a.FeedTitle == feed.Title)
        .ToListAsync();
    if (articlesToDelete.Count > 0)
    {
        db.Articles.RemoveRange(articlesToDelete);
        await db.SaveChangesAsync();
    }

    return Results.NoContent();
});

app.MapGet("/api/playlists", async (AppDbContext db, HttpContext http) =>
{
    var key = GetUserKey(http);
    var playlists = await db.Playlists
        .Where(p => p.UserId == key || p.GuestSessionId == key)
        .OrderBy(p => p.Name)
        .Select(p => new { p.Id, p.Name, FeedCount = p.Feeds.Count })
        .ToListAsync();
    return Results.Ok(playlists);
});

app.MapPost("/api/playlists", async (CreatePlaylistDto dto, AppDbContext db, HttpContext http) =>
{
    var key = GetUserKey(http);
    var isGuest = http.User.FindFirstValue(ClaimTypes.NameIdentifier) is null;

    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Playlist name is required." });

    var playlist = new Playlist
    {
        Id = Guid.NewGuid().ToString(),
        Name = dto.Name.Trim()
    };
    if (isGuest) playlist.GuestSessionId = key;
    else playlist.UserId = key;

    db.Playlists.Add(playlist);
    await db.SaveChangesAsync();

    return Results.Created($"/api/playlists/{playlist.Id}", new { playlist.Id, playlist.Name, FeedCount = 0 });
});

app.MapPut("/api/playlists/{id}", async (string id, UpdatePlaylistDto dto, AppDbContext db, HttpContext http) =>
{
    var key = GetUserKey(http);
    var playlist = await db.Playlists
        .FirstOrDefaultAsync(p => p.Id == id && (p.UserId == key || p.GuestSessionId == key));

    if (playlist is null) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(dto.Name))
        return Results.BadRequest(new { error = "Playlist name is required." });

    playlist.Name = dto.Name.Trim();
    await db.SaveChangesAsync();

    return Results.Ok(new { playlist.Id, playlist.Name });
});

app.MapDelete("/api/playlists/{id}", async (string id, AppDbContext db, HttpContext http, [FromQuery] bool deleteFeeds = false) =>
{
    var key = GetUserKey(http);
    var playlist = await db.Playlists
        .Include(p => p.Feeds)
        .FirstOrDefaultAsync(p => p.Id == id && (p.UserId == key || p.GuestSessionId == key));

    if (playlist is null) return Results.NotFound();

    if (deleteFeeds)
    {
        var feedTitles = playlist.Feeds.Select(f => f.Title).Distinct().ToList();
        db.Feeds.RemoveRange(playlist.Feeds);
        await db.SaveChangesAsync();
        if (feedTitles.Count > 0)
        {
            var articles = await db.Articles
                .Where(a => (a.UserId == key || a.GuestSessionId == key) && feedTitles.Contains(a.FeedTitle))
                .ToListAsync();
            if (articles.Count > 0) db.Articles.RemoveRange(articles);
            await db.SaveChangesAsync();
        }
    }
    else
    {
        foreach (var feed in playlist.Feeds)
            feed.PlaylistId = null;
        await db.SaveChangesAsync();
    }

    db.Playlists.Remove(playlist);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapPatch("/api/playlists/{id}/favorite", async (string id, AppDbContext db, HttpContext http) =>
{
    var key = GetUserKey(http);
    var playlist = await db.Playlists
        .FirstOrDefaultAsync(p => p.Id == id && (p.UserId == key || p.GuestSessionId == key));
    if (playlist is null) return Results.NotFound();

    var feeds = await db.Feeds
        .Where(f => f.PlaylistId == id && (f.UserId == key || f.GuestSessionId == key))
        .ToListAsync();

    foreach (var feed in feeds)
        feed.IsFavorite = true;

    await db.SaveChangesAsync();
    return Results.Ok(new { favorited = feeds.Count });
});

app.MapPatch("/api/feeds/{id}/playlist", async (string id, AssignPlaylistDto dto, AppDbContext db, HttpContext http) =>
{
    var key = GetUserKey(http);
    var feed = await db.Feeds.FirstOrDefaultAsync(f => f.Id == id && (f.UserId == key || f.GuestSessionId == key));
    if (feed is null) return Results.NotFound();

    if (dto.PlaylistId is not null)
    {
        var playlist = await db.Playlists
            .FirstOrDefaultAsync(p => p.Id == dto.PlaylistId && (p.UserId == key || p.GuestSessionId == key));
        if (playlist is null)
            return Results.BadRequest(new { error = "Playlist not found." });
    }

    feed.PlaylistId = dto.PlaylistId;
    await db.SaveChangesAsync();

    return Results.Ok(new { feed.Id, feed.PlaylistId });
});

app.MapGet("/api/news", async (
    AppDbContext db, FeedArticleService articleService,
    HttpContext http, [FromQuery] int? retentionDays, [FromQuery] string? playlist, CancellationToken ct) =>
{
    try
    {
        var userId = GetUserKey(http);
        var feeds = await db.Feeds
            .Where(f => f.UserId == userId || f.GuestSessionId == userId)
            .ToListAsync(ct);

        if (!string.IsNullOrEmpty(playlist))
        {
            var playlistIds = await db.Playlists
                .Where(p => (p.UserId == userId || p.GuestSessionId == userId) && p.Name == playlist)
                .Select(p => p.Id)
                .ToListAsync(ct);
            feeds = feeds.Where(f => f.PlaylistId != null && playlistIds.Contains(f.PlaylistId)).ToList();
        }

        var tasks = feeds.Select(async feed =>
        {
            try
            {
                return await articleService.FetchArticlesAsync(
                    feed.Url, feed.Title, feed.Username, feed.Password, ct);
            }
            catch (Exception)
            {
                return new List<ArticleEntity>();
            }
        });

        var results = await Task.WhenAll(tasks);
        var parsedArticles = results.SelectMany(x => x)
            .GroupBy(a => a.Link)
            .Select(g => g.First())
            .ToList();

        var firstFeed = feeds.FirstOrDefault();
        foreach (var a in parsedArticles)
        {
            a.UserId = firstFeed?.UserId;
            a.GuestSessionId = firstFeed?.GuestSessionId;
        }

        if (parsedArticles.Count > 0)
        {
            var existing = (await db.Articles
                .Where(a => a.UserId == userId || a.GuestSessionId == userId)
                .Select(a => a.Link).ToListAsync()).ToHashSet();

            var newArticles = parsedArticles.Where(a => !existing.Contains(a.Link)).ToList();
            if (newArticles.Count > 0)
            {
                db.Articles.AddRange(newArticles);
                await db.SaveChangesAsync();
            }
        }

        var cutoff = DateTime.UtcNow.AddDays(-(retentionDays ?? 30));
        var oldArticles = await db.Articles
            .Where(a => (a.UserId == userId || a.GuestSessionId == userId) && a.PublishDate < cutoff && !a.IsBookmarked)
            .ToListAsync();
        if (oldArticles.Count > 0)
        {
            db.Articles.RemoveRange(oldArticles);
            await db.SaveChangesAsync();
        }

        var articles = await db.Articles
            .Where(a => a.UserId == userId || a.GuestSessionId == userId)
            .OrderByDescending(a => a.PublishDate)
            .Select(a => new Article(a.Id, a.FeedTitle, a.Title, a.Link, a.PublishDate,
                a.Summary, a.AudioUrl, a.ImageUrl, a.IsBookmarked, a.IsRead))
            .ToListAsync();

        return Results.Ok(articles);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"CRASH IN /api/news: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
        return Results.Problem(detail: ex.ToString(), statusCode: 500);
    }
});

app.MapPost("/api/refresh-feed", async (
    FeedDto dto, AppDbContext db, FeedArticleService articleService,
    HttpContext http, [FromQuery] int? retentionDays, CancellationToken ct) =>
{
    try
    {
        var key = GetUserKey(http);
        var feed = await db.Feeds.FirstOrDefaultAsync(f => f.Url == dto.Url && (f.UserId == key || f.GuestSessionId == key));
        if (feed is null)
            return Results.BadRequest("This feed is not in your subscriptions.");

        List<ArticleEntity> freshArticles;
        try
        {
            freshArticles = await articleService.FetchArticlesAsync(
                feed.Url, feed.Title, feed.Username, feed.Password, ct);
        }
        catch (FeedFetchException ex)
        {
            return Results.BadRequest($"Failed to fetch the feed: {ex.Message}");
        }

        freshArticles = freshArticles.GroupBy(a => a.Link).Select(g => g.First()).ToList();
        foreach (var a in freshArticles)
        {
            a.UserId = feed.UserId;
            a.GuestSessionId = feed.GuestSessionId;
        }

        var existing = (await db.Articles
            .Where(a => a.UserId == key || a.GuestSessionId == key)
            .Select(a => a.Link).ToListAsync()).ToHashSet();

        var newArticles = freshArticles.Where(a => !existing.Contains(a.Link)).ToList();
        if (newArticles.Count > 0)
        {
            db.Articles.AddRange(newArticles);
            await db.SaveChangesAsync();
        }

        var cutoff = DateTime.UtcNow.AddDays(-(retentionDays ?? 30));
        var oldArticles = await db.Articles
            .Where(a => (a.UserId == key || a.GuestSessionId == key) && a.PublishDate < cutoff && !a.IsBookmarked)
            .ToListAsync();
        if (oldArticles.Count > 0)
        {
            db.Articles.RemoveRange(oldArticles);
            await db.SaveChangesAsync();
        }

        return Results.Ok();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"CRASH IN /api/refresh-feed: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
        return Results.Problem(detail: ex.ToString(), statusCode: 500);
    }
});

app.MapPost("/api/news/summarize", async (SummarizeDto dto) =>
{
    var sanitizer = new Ganss.Xss.HtmlSanitizer();
    await Task.Delay(1500);
    var summary = $"✨ [AI Summary]: {dto.TextContent[..Math.Min(dto.TextContent.Length, 100)]}... This is a simulated summary.";
    var clean = sanitizer.Sanitize(summary);
    return Results.Ok(new { summary = clean });
});

app.MapPatch("/api/news/{id}/bookmark", async (int id, AppDbContext db, HttpContext http) =>
{
    var key = GetUserKey(http);
    var article = await db.Articles.FirstOrDefaultAsync(a => a.Id == id && (a.UserId == key || a.GuestSessionId == key));
    if (article is null) return Results.NotFound();
    article.IsBookmarked = !article.IsBookmarked;
    await db.SaveChangesAsync();
    return Results.Ok(new { id = article.Id, isBookmarked = article.IsBookmarked });
});

app.MapPatch("/api/news/{id}/read", async (int id, AppDbContext db, HttpContext http) =>
{
    var key = GetUserKey(http);
    var article = await db.Articles.FirstOrDefaultAsync(a => a.Id == id && (a.UserId == key || a.GuestSessionId == key));
    if (article is null) return Results.NotFound();
    article.IsRead = true;
    await db.SaveChangesAsync();

    return Results.Ok(new { id = article.Id, isRead = article.IsRead });
});

app.MapGet("/api/articles/history", async (AppDbContext db, HttpContext http) =>
{
    var key = GetUserKey(http);
    var articles = await db.Articles
        .Where(a => (a.UserId == key || a.GuestSessionId == key) && a.IsRead)
        .OrderByDescending(a => a.PublishDate)
        .Take(50)
        .Select(a => new Article(a.Id, a.FeedTitle, a.Title, a.Link, a.PublishDate,
            a.Summary, a.AudioUrl, a.ImageUrl, a.IsBookmarked, a.IsRead))
        .ToListAsync();
    return Results.Ok(articles);
});

app.MapPost("/api/chat", async (
    ChatRequest request, IAiService ai, HttpContext http, AppDbContext db,
    [FromQuery] string lang = "en") =>
{
    var (quotaKey, limit, errMsg) = GetQuotaParams(http);
    if (!await CheckAiQuotaAsync(db, quotaKey, limit)) return Results.Json(new { error = errMsg }, statusCode: 429);
    var userKey = GetUserKey(http);
    var response = await ai.AskQuestionAsync(request.Message, userKey, lang);
    return Results.Ok(new { response });
}).RequireRateLimiting("AiEndpointPolicy");

app.MapGet("/api/news/daily-briefing", async (
    AppDbContext db, IAiService ai, HttpContext http,
    [FromQuery] string lang = "en") =>
{
    var (quotaKey, limit, errMsg) = GetQuotaParams(http);
    if (!await CheckAiQuotaAsync(db, quotaKey, limit)) return Results.Json(new { error = errMsg }, statusCode: 429);
    var userKey = GetUserKey(http);
    var articles = await db.Articles
        .Where(a => (a.UserId == userKey || a.GuestSessionId == userKey) && !a.IsRead)
        .OrderByDescending(a => a.PublishDate)
        .Take(10)
        .ToListAsync();
    var summary = await ai.GenerateDailySummaryAsync(articles, lang);
    return Results.Ok(new { summary });
}).RequireRateLimiting("AiEndpointPolicy");

app.MapGet("/api/ai/summary", async (
    IAiService ai, AppDbContext db, HttpContext http,
    [FromQuery] string lang = "en") =>
{
    var (quotaKey, limit, errMsg) = GetQuotaParams(http);
    if (!await CheckAiQuotaAsync(db, quotaKey, limit)) return Results.Json(new { error = errMsg }, statusCode: 429);
    var userKey = GetUserKey(http);
    var articles = await db.Articles
        .Where(a => (a.UserId == userKey || a.GuestSessionId == userKey) && !a.IsRead)
        .OrderByDescending(a => a.PublishDate)
        .Take(15)
        .ToListAsync();
    var summary = await ai.GenerateDailySummaryAsync(articles, lang);
    return Results.Ok(new { summary });
}).RequireRateLimiting("AiEndpointPolicy");

app.MapPost("/api/ai/chat", async (
    ChatRequest request, IAiService ai, HttpContext http, AppDbContext db,
    [FromQuery] string lang = "en") =>
{
    var (quotaKey, limit, errMsg) = GetQuotaParams(http);
    if (!await CheckAiQuotaAsync(db, quotaKey, limit)) return Results.Json(new { error = errMsg }, statusCode: 429);
    var userKey = GetUserKey(http);
    var response = await ai.AskQuestionAsync(request.Message, userKey, lang);
    return Results.Ok(new { response });
}).RequireRateLimiting("AiEndpointPolicy");

app.MapGet("/api/ai/summary/article/{id:int}", async (
    int id, IAiService ai, AppDbContext db, HttpContext http,
    [FromQuery] string lang = "en") =>
{
    var (quotaKey, limit, errMsg) = GetQuotaParams(http);
    if (!await CheckAiQuotaAsync(db, quotaKey, limit)) return Results.Json(new { error = errMsg }, statusCode: 429);
    var userKey = GetUserKey(http);
    var summary = await ai.SummarizeArticleAsync(id, userKey, lang);
    return Results.Ok(new { summary });
}).RequireRateLimiting("AiEndpointPolicy");

app.MapGet("/api/hacker", () =>
{
    var xml = @"<?xml version=""1.0"" encoding=""UTF-8"" ?>
    <rss version=""2.0"">
    <channel>
      <title><![CDATA[<script>alert('Hacked Title!')</script>Dark Web News]]></title>
      <link>http://example.com/</link>
      <item>
        <title>You won a million dollars!</title>
        <link>javascript:alert('Stole your session tokens!')</link>
        <pubDate>Mon, 06 Jul 2026 12:00:00 GMT</pubDate>
        <description><![CDATA[<p>Click the link above! <script>alert('XSS Attack Execution!');</script> <b>This bold text is safe.</b></p>]]></description>
      </item>
    </channel>
    </rss>";
    return Results.Text(xml, "application/xml");
});

static string? GetFaviconUrl(string feedUrl)
{
    if (!Uri.TryCreate(feedUrl, UriKind.Absolute, out var uri)) return null;
    return $"https://www.google.com/s2/favicons?domain={uri.Host}&sz=64";
}

static string NormalizeUrl(string url) =>
    url.Trim().TrimEnd('/').ToLowerInvariant().Replace("https://", "http://");

static (string userId, int limit, string errMsg) GetQuotaParams(HttpContext http)
{
    var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is not null) return (userId, 15, "Daily AI quota of 15 requests exceeded. Please try again tomorrow.");
    var ip = http.Connection.RemoteIpAddress?.ToString() ?? "anon";
    if (ip is "::1" or "127.0.0.1") return ("guest-local", 999, "");
    return ("guest-" + ip, 3, "Guest quota reached. Please sign in to continue using AI.");
}

static async Task<bool> CheckAiQuotaAsync(AppDbContext db, string userId, int limit = 15)
{
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var usage = await db.AiUsage.FirstOrDefaultAsync(u => u.UserId == userId && u.Date == today);
    if (usage is null)
    {
        db.AiUsage.Add(new UserAiUsage { UserId = userId, Date = today, RequestCount = 1 });
        await db.SaveChangesAsync();
        return true;
    }
    if (usage.RequestCount >= limit) return false;
    usage.RequestCount++;
    await db.SaveChangesAsync();
    return true;
}

app.Run();

public class ArticleEntity
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string FeedTitle { get; set; } = "";
    public DateTime PublishDate { get; set; }
    public string Link { get; set; } = "";
    public string? AudioUrl { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsBookmarked { get; set; } = false;
    public bool IsRead { get; set; } = false;
    public string? UserId { get; set; }
    public string? GuestSessionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class FeedSubscription
{
    public string Id { get; set; } = "";
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool IsFavorite { get; set; } = false;
    public string? UserId { get; set; }
    public string? GuestSessionId { get; set; }
    public string? FaviconUrl { get; set; }
    public string? PlaylistId { get; set; }
    public Playlist? Playlist { get; set; }
    public DateTime LastRefreshedAt { get; set; } = DateTime.MinValue;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Playlist
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? UserId { get; set; }
    public string? GuestSessionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<FeedSubscription> Feeds { get; set; } = new List<FeedSubscription>();
}

public class UserProfile
{
    public string Id { get; set; } = "";
    public string? UserId { get; set; }
    public string? GuestSessionId { get; set; }
    public string? DisplayName { get; set; }
    public string? Bio { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string? CoverPhotoUrl { get; set; }
    public string? SocialLinks { get; set; }
    public int KeepArticlesForDays { get; set; } = 30;
    public int RefreshIntervalMinutes { get; set; } = 30;
    public bool EmailFavoriteFeeds { get; set; } = false;
}

public class CommunityPost
{
    public string Id { get; set; } = "";
    public string Content { get; set; } = "";
    public string? MediaUrl { get; set; }
    public string AuthorId { get; set; } = "";
    public string? AuthorName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<PostReaction> Reactions { get; set; } = new List<PostReaction>();
    public ICollection<PostComment> Comments { get; set; } = new List<PostComment>();
}

public class PostReaction
{
    public int Id { get; set; }
    public string PostId { get; set; } = "";
    public string UserId { get; set; } = "";
    public string ReactionType { get; set; } = "";
    public CommunityPost? Post { get; set; }
}

public class PostComment
{
    public int Id { get; set; }
    public string PostId { get; set; } = "";
    public string AuthorId { get; set; } = "";
    public string? AuthorName { get; set; }
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public CommunityPost? Post { get; set; }
}

public class UserBlock
{
    public string BlockerId { get; set; } = "";
    public string BlockedId { get; set; } = "";
}

public class AppDbContext : IdentityDbContext<IdentityUser>
{
    public DbSet<ArticleEntity> Articles => Set<ArticleEntity>();
    public DbSet<FeedSubscription> Feeds => Set<FeedSubscription>();
    public DbSet<UserAiUsage> AiUsage => Set<UserAiUsage>();
    public DbSet<Playlist> Playlists => Set<Playlist>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<CommunityPost> CommunityPosts => Set<CommunityPost>();
    public DbSet<PostReaction> PostReactions => Set<PostReaction>();
    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();
    public DbSet<PostComment> PostComments => Set<PostComment>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ArticleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PublishDate);
        });

        modelBuilder.Entity<FeedSubscription>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(f => f.Playlist)
                .WithMany(p => p.Feeds)
                .HasForeignKey(f => f.PlaylistId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Playlist>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<CommunityPost>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasMany(e => e.Reactions)
                .WithOne(r => r.Post)
                .HasForeignKey(r => r.PostId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Comments)
                .WithOne(c => c.Post)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PostReaction>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<PostComment>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<UserBlock>(entity =>
        {
            entity.HasKey(e => new { e.BlockerId, e.BlockedId });
        });

        modelBuilder.Entity<UserAiUsage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Date }).IsUnique();
        });
    }
}

public class UserAiUsage
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public DateOnly Date { get; set; }
    public int RequestCount { get; set; }
}

record FeedDto(string Url, string? Username = null, string? Password = null, string? PlaylistId = null);
record ParseUrlRequest(string Url);
record BatchFeedDto(string[] Urls, string? Username = null, string? Password = null, string? PlaylistId = null);
record SummarizeDto(string Link, string TextContent);
record ChatRequest(string Message);
record RegisterRequest(string Email, string Password);
record ResendRequest(string Email);
record CreatePlaylistDto(string Name);
record UpdatePlaylistDto(string Name);
record AssignPlaylistDto(string? PlaylistId);
record UpdateProfileDto(string? DisplayName, string? Bio, string? ProfilePictureUrl, string? CoverPhotoUrl, string? SocialLinks, int? KeepArticlesForDays = null, int? RefreshIntervalMinutes = null, bool? EmailFavoriteFeeds = null);
record CreatePostDto(string Content, string? MediaUrl = null);
record ReactDto(string ReactionType);
record Article(int Id, string FeedTitle, string Title, string Link, DateTime PublishDate, string Summary, string? AudioUrl = null, string? ImageUrl = null, bool IsBookmarked = false, bool IsRead = false);
