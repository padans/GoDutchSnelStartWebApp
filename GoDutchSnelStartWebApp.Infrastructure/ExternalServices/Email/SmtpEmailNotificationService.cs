using GoDutchSnelStartWebApp.Application.Configuration;
using GoDutchSnelStartWebApp.Application.Notifications.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

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
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
            message.To.Add(new MailboxAddress(string.Empty, _options.ToAddress));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            var secureSocketOptions = _options.Port == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTlsWhenAvailable;

            using var client = new SmtpClient();
            await client.ConnectAsync(_options.Host, _options.Port, secureSocketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.Username))
                await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);

            _logger.LogInformation("E-mail verstuurd naar {To}: {Subject}", _options.ToAddress, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "E-mail verzenden mislukt naar {To}: {Subject}", _options.ToAddress, subject);
        }
    }
}
