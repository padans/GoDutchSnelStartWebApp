using System.Net;
using System.Net.Mail;
using GoDutchSnelStartWebApp.Application.Configuration;
using GoDutchSnelStartWebApp.Application.Notifications.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoDutchSnelStartWebApp.Infrastructure.ExternalServices.Email;

public sealed class SmtpEmailNotificationService : IEmailNotificationService
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailNotificationService> _logger;

    public SmtpEmailNotificationService(
        IOptions<EmailOptions> options,
        ILogger<SmtpEmailNotificationService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string subject, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.ToAddress))
        {
            _logger.LogWarning("E-mail niet verzonden: SMTP-host of ontvanger is niet geconfigureerd.");
            return;
        }

        try
        {
            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                Credentials = new NetworkCredential(_options.Username, _options.Password)
            };

            var message = new MailMessage(
                from: new MailAddress(_options.FromAddress, _options.FromName),
                to: new MailAddress(_options.ToAddress))
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            await client.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("E-mail verstuurd naar {To}: {Subject}", _options.ToAddress, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-mail verzenden mislukt naar {To}: {Subject}", _options.ToAddress, subject);
        }
    }
}
