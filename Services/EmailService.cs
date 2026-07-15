using System.Net;
using System.Net.Mail;

namespace PersonalRssReader.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string userId, string token, string baseUrl);
}

public sealed class SmtpEmailService : IEmailService
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _host = config["Email:Host"] ?? "";
        _port = int.TryParse(config["Email:Port"], out var p) ? p : 587;
        _username = config["Email:Username"] ?? "";
        _password = config["Email:Password"] ?? "";
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(string email, string userId, string token, string baseUrl)
    {
        var link = $"{baseUrl}/verify.html?userId={userId}&token={Uri.EscapeDataString(token)}";
        var html = $"""
            <div style="font-family:sans-serif;max-width:480px;margin:0 auto;background:#1e293b;color:#f1f5f9;padding:2rem;border-radius:16px">
              <h2 style="color:#818cf8">Verify your email</h2>
              <p style="color:#94a3b8">Click the button below to activate your Personal RSS Reader account.</p>
              <a href="{link}" style="display:inline-block;background:#818cf8;color:#fff;padding:0.75rem 1.5rem;border-radius:9999px;text-decoration:none;font-weight:600;margin:1rem 0">Verify Email</a>
              <p style="font-size:0.8rem;color:#64748b">Or copy this link: {link}</p>
            </div>
            """;

        if (string.IsNullOrEmpty(_host) || string.IsNullOrEmpty(_username))
        {
            _logger.LogInformation("[EMAIL] Verification link for {Email}: {Link}", email, link);
            return;
        }

        try
        {
            using var smtp = new SmtpClient(_host, _port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(_username, _password)
            };
            using var mail = new MailMessage(_username, email, "Verify your email — Personal RSS Reader", html)
            {
                IsBodyHtml = true
            };
            await smtp.SendMailAsync(mail);
            _logger.LogInformation("[EMAIL] Sent verification to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMAIL] Failed to send to {Email}", email);
            throw;
        }
    }
}
