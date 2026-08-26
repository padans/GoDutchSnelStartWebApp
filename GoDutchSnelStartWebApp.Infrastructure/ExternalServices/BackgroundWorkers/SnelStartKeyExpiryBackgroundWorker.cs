using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories.SnelStart;
using GoDutchSnelStartWebApp.Application.Configuration;
using GoDutchSnelStartWebApp.Application.Notifications.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoDutchSnelStartWebApp.Infrastructure.ExternalServices.BackgroundWorkers;

public sealed class SnelStartKeyExpiryBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<SnelStartKeyExpiryBackgroundWorker> _logger;
    private readonly EmailOptions _emailOptions;

    public SnelStartKeyExpiryBackgroundWorker(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<EmailOptions> emailOptions,
        ILogger<SnelStartKeyExpiryBackgroundWorker> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SnelStart sleutelverval-checker gestart.");

        // Eerste check kort na opstarten
        await RunOnceAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("SnelStart sleutelverval-checker gestopt.");
                break;
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();

            var connectionRepository = scope.ServiceProvider
                .GetRequiredService<ITenantSnelStartConnectionRepository>();
            var tenantRepository = scope.ServiceProvider
                .GetRequiredService<ITenantRepository>();
            var emailService = scope.ServiceProvider
                .GetRequiredService<IEmailNotificationService>();

            var expiring = await connectionRepository.GetExpiringCustomKeyConnectionsAsync(cancellationToken);

            if (expiring.Count == 0)
            {
                _logger.LogDebug("Geen verlopende SnelStart sleutels gevonden.");
                return;
            }

            _logger.LogInformation("{Aantal} verlopende SnelStart sleutel(s) gevonden.", expiring.Count);

            var now = DateTime.UtcNow;

            foreach (var conn in expiring)
            {
                try
                {
                    var tenant = await tenantRepository.GetByIdAsync(conn.TenantId, cancellationToken);
                    if (tenant is null)
                    {
                        _logger.LogWarning("Tenant {TenantId} niet gevonden voor verlopende sleutel {ConnectionId}.", conn.TenantId, conn.Id);
                        continue;
                    }

                    var daysLeft = conn.KeyExpiresUtc.HasValue
                        ? (int)Math.Ceiling((conn.KeyExpiresUtc.Value - now).TotalDays)
                        : 0;

                    var isExpired = daysLeft <= 0;

                    _logger.LogInformation(
                        "SnelStart sleutel voor tenant {TenantName} verloopt over {DaysLeft} dag(en). Email: {Email}",
                        tenant.Name, daysLeft, tenant.Email ?? "(geen)");

                    if (!string.IsNullOrWhiteSpace(tenant.Email))
                    {
                        var html = BuildExpiryWarningEmail(tenant.Name ?? tenant.CustomerCode ?? conn.TenantId.ToString(), daysLeft, conn.KeyExpiresUtc, isExpired);
                        await emailService.SendToAsync(
                            tenant.Email,
                            tenant.ContactName ?? tenant.Name ?? "Klant",
                            isExpired
                                ? "SnelStart maatwerksleutel verlopen — actie vereist"
                                : $"SnelStart maatwerksleutel verloopt over {daysLeft} dag(en)",
                            html,
                            cancellationToken);
                    }

                    // Admin-melding
                    await emailService.SendAsync(
                        $"SnelStart sleutel {(isExpired ? "verlopen" : $"verloopt over {daysLeft} dag(en)")}: {tenant.Name}",
                        $"Tenant: {tenant.Name}\nEmail: {tenant.Email ?? "(geen)"}\nVerloopt: {conn.KeyExpiresUtc:dd-MM-yyyy HH:mm} UTC\nDagen resterend: {daysLeft}",
                        cancellationToken);

                    await connectionRepository.MarkExpirySentAsync(conn.Id, now, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fout bij verwerken verlopende sleutel {ConnectionId}.", conn.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout in SnelStart sleutelverval-check.");
        }
    }

    private string BuildExpiryWarningEmail(string tenantName, int daysLeft, DateTime? expiresUtc, bool isExpired)
    {
        var portalUrl = _emailOptions.PortalUrl;
        var adminEmail = _emailOptions.FromAddress;

        var urgencyColor = isExpired ? "#DC2626" : "#D97706";
        var urgencyBg = isExpired ? "#FEF2F2" : "#FFFBEB";
        var urgencyBorder = isExpired ? "#FECACA" : "#FCD34D";

        var statusText = isExpired
            ? "uw SnelStart maatwerksleutel is <strong>verlopen</strong>. De koppeling met SnelStart werkt niet meer totdat u een nieuwe sleutel instelt."
            : $"uw SnelStart maatwerksleutel verloopt over <strong>{daysLeft} dag{(daysLeft == 1 ? "" : "en")}</strong> (op {expiresUtc:dd-MM-yyyy}). Vernieuw de sleutel op tijd om onderbreking te voorkomen.";

        return $"""
            <!DOCTYPE html>
            <html lang="nl">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="margin:0;padding:0;background:#F3F7FB;font-family:Arial,Helvetica,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#F3F7FB;padding:32px 0;">
                <tr><td align="center">
                  <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,46,92,.10);">
                    <tr>
                      <td style="background:linear-gradient(135deg,#0066CC,#003D8A);padding:28px 40px;">
                        <div style="font-size:20px;font-weight:800;color:#ffffff;">GoDutch MyPos SnelStart</div>
                        <div style="font-size:12px;color:#A8CCEE;margin-top:3px;">Padans B.V. Nijmegen</div>
                      </td>
                    </tr>
                    <tr>
                      <td style="padding:28px 40px 8px 40px;">
                        <div style="background:{urgencyBg};border:2px solid {urgencyBorder};border-radius:10px;padding:16px 20px;margin-bottom:20px;">
                          <span style="font-size:16px;font-weight:800;color:{urgencyColor};">{(isExpired ? "⛔ Sleutel verlopen" : "⚠️ Sleutel verloopt binnenkort")}</span>
                        </div>
                        <p style="color:#374151;font-size:14px;line-height:1.6;margin:0 0 14px 0;">
                          Beste <strong>{tenantName}</strong>,
                        </p>
                        <p style="color:#374151;font-size:14px;line-height:1.6;margin:0 0 20px 0;">
                          {statusText}
                        </p>
                        <p style="color:#374151;font-size:14px;line-height:1.6;margin:0 0 24px 0;">
                          Log in op het portaal, ga naar <em>SnelStart koppeling</em> en voer een nieuwe maatwerksleutel in.
                          Neem bij vragen contact op via
                          <a href="mailto:{adminEmail}" style="color:#0066CC;">{adminEmail}</a>.
                        </p>
                        <div style="text-align:center;margin-bottom:28px;">
                          <a href="{portalUrl}/snelstart/connection" style="display:inline-block;background:#0066CC;color:#ffffff;text-decoration:none;padding:13px 32px;border-radius:8px;font-weight:700;font-size:14px;">
                            Sleutel vernieuwen
                          </a>
                        </div>
                      </td>
                    </tr>
                    <tr>
                      <td style="background:#F9FAFB;border-top:1px solid #E5E7EB;padding:16px 40px;text-align:center;">
                        <p style="color:#9CA3AF;font-size:11px;margin:0;">Padans B.V. Nijmegen &mdash; <a href="{portalUrl}" style="color:#9CA3AF;">{portalUrl}</a></p>
                      </td>
                    </tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }
}
