using System.Security.Cryptography;
using System.Text;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.AppUsers.Interfaces;
using GoDutchSnelStartWebApp.Application.Configuration;
using GoDutchSnelStartWebApp.Application.Notifications.Interfaces;
using GoDutchSnelStartWebApp.Application.Tenants.Dtos;
using GoDutchSnelStartWebApp.Application.Tenants.Interfaces;
using GoDutchSnelStartWebApp.Domain.Enums;
using GoDutchSnelStartWebApp.Web.Contracts.Tenants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GoDutchSnelStartWebApp.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly IAppUserService _appUserService;
    private readonly IAppUserRepository _userRepository;
    private readonly IEmailNotificationService _emailService;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(
        ITenantService tenantService,
        IAppUserService appUserService,
        IAppUserRepository userRepository,
        IEmailNotificationService emailService,
        IOptions<EmailOptions> emailOptions,
        ILogger<TenantsController> logger)
    {
        _tenantService = tenantService;
        _appUserService = appUserService;
        _userRepository = userRepository;
        _emailService = emailService;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TenantDto>>> GetAll(CancellationToken cancellationToken)
    {
        var tenants = await _tenantService.GetAllAsync(cancellationToken);
        return Ok(tenants);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenantDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.GetByIdAsync(id, cancellationToken);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var id = await _tenantService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, null);
    }

    [HttpPost("onboard")]
    public async Task<ActionResult> Onboard([FromBody] CreateTenantRequest request, CancellationToken cancellationToken)
    {
        if (!request.GoDutchEnabled && !request.MyPosEnabled)
            return BadRequest("Selecteer minimaal één module.");

        var tenantId = await _tenantService.CreateAsync(request, cancellationToken);
        _logger.LogInformation("Onboarding tenant {TenantId} ({CompanyName})", tenantId, request.CompanyName);

        var baseUsername = GenerateBaseUsername(request.CompanyName ?? request.Name);
        var credentials = new List<(string Label, string Username, string Password)>();

        if (request.GoDutchEnabled)
        {
            var suffix = request.MyPosEnabled ? "_gd" : string.Empty;
            var username = await GetUniqueUsernameAsync(baseUsername + suffix, cancellationToken);
            var password = GeneratePassword();
            await _appUserService.CreateAsync(username, password, AppModule.GoDutch, requirePasswordChange: true, tenantId: tenantId, cancellationToken: cancellationToken);
            credentials.Add(("GoDutch → SnelStart", username, password));
            _logger.LogInformation("AppUser aangemaakt voor GoDutch: {Username}", username);
        }

        if (request.MyPosEnabled)
        {
            var suffix = request.GoDutchEnabled ? "_mp" : string.Empty;
            var username = await GetUniqueUsernameAsync(baseUsername + suffix, cancellationToken);
            var password = GeneratePassword();
            await _appUserService.CreateAsync(username, password, AppModule.MyPos, requirePasswordChange: true, tenantId: tenantId, cancellationToken: cancellationToken);
            credentials.Add(("MyPos → SnelStart", username, password));
            _logger.LogInformation("AppUser aangemaakt voor MyPos: {Username}", username);
        }

        var trialDays = request.IsTrial && request.TrialDurationDays > 0 ? request.TrialDurationDays : 30;
        var trialEnds = DateTime.Now.AddDays(trialDays).ToString("dd-MM-yyyy");

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var html = BuildWelcomeEmail(
                request.ContactName,
                request.CompanyName ?? request.Name,
                credentials,
                trialEnds);

            await _emailService.SendToAsync(
                request.Email,
                request.ContactName ?? string.Empty,
                "Uw inloggegevens — GoDutch MyPos SnelStart portaal",
                html,
                cancellationToken);
        }

        // Admin-notificatie
        var modules = string.Join(", ", credentials.Select(c => c.Label));
        var adminBody = $"""
            Nieuwe aanmelding via het portaal

            Bedrijf:       {request.CompanyName ?? request.Name}
            Contactpersoon:{request.ContactName}
            E-mail:        {request.Email}
            Telefoon:      {request.Phone ?? "—"}
            KvK:           {request.KvkNumber ?? "—"}
            Module(s):     {modules}
            Proefperiode:  tot {trialEnds}
            Aangemeld op:  {DateTime.Now:dd-MM-yyyy HH:mm}
            """;

        await _emailService.SendAsync(
            $"Nieuwe aanmelding: {request.CompanyName ?? request.Name}",
            adminBody,
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = tenantId }, null);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateTenantRequest request, CancellationToken cancellationToken)
    {
        await _tenantService.UpdateAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _tenantService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    // ── Abonnementsbeheer door de klant zelf ────────────────────────

    public sealed class SubscriptionChangeRequest
    {
        public bool GoDutchEnabled { get; set; }
        public bool MyPosEnabled { get; set; }
    }

    public sealed class NewModuleCredential
    {
        public string Label { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    [HttpPost("{tenantId:guid}/subscription/change")]
    public async Task<ActionResult<List<NewModuleCredential>>> ChangeSubscription(
        Guid tenantId,
        [FromBody] SubscriptionChangeRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.GoDutchEnabled && !request.MyPosEnabled)
            return BadRequest("Minimaal één module is vereist.");

        var tenant = await _tenantService.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
            return NotFound();

        var newCredentials = new List<NewModuleCredential>();
        var added = new List<string>();
        var removed = new List<string>();

        var baseUsername = GenerateBaseUsername(tenant.CompanyName ?? tenant.Name);

        // Nieuwe modules toevoegen
        if (request.GoDutchEnabled && !tenant.GoDutchEnabled)
        {
            var suffix = request.MyPosEnabled ? "_gd" : string.Empty;
            var username = await GetUniqueUsernameAsync(baseUsername + suffix, cancellationToken);
            var password = GeneratePassword();
            await _appUserService.CreateAsync(username, password, AppModule.GoDutch,
                requirePasswordChange: true, tenantId: tenantId, cancellationToken: cancellationToken);
            newCredentials.Add(new NewModuleCredential { Label = "GoDutch → SnelStart", Username = username, Password = password });
            added.Add("GoDutch → SnelStart");
            _logger.LogInformation("Abonnement uitgebreid: GoDutch user {Username} voor tenant {TenantId}", username, tenantId);
        }

        if (request.MyPosEnabled && !tenant.MyPosEnabled)
        {
            var suffix = request.GoDutchEnabled ? "_mp" : string.Empty;
            var username = await GetUniqueUsernameAsync(baseUsername + suffix, cancellationToken);
            var password = GeneratePassword();
            await _appUserService.CreateAsync(username, password, AppModule.MyPos,
                requirePasswordChange: true, tenantId: tenantId, cancellationToken: cancellationToken);
            newCredentials.Add(new NewModuleCredential { Label = "MyPos → SnelStart", Username = username, Password = password });
            added.Add("MyPos → SnelStart");
            _logger.LogInformation("Abonnement uitgebreid: MyPos user {Username} voor tenant {TenantId}", username, tenantId);
        }

        // Verwijderde modules deactiveren
        if (!request.GoDutchEnabled && tenant.GoDutchEnabled)
        {
            var tenantUsers = await _userRepository.GetByTenantIdAsync(tenantId, cancellationToken);
            foreach (var u in tenantUsers.Where(u => u.Module == AppModule.GoDutch && u.IsActive))
            {
                u.IsActive = false;
                await _userRepository.UpdateAsync(u, cancellationToken);
            }
            removed.Add("GoDutch → SnelStart");
        }

        if (!request.MyPosEnabled && tenant.MyPosEnabled)
        {
            var tenantUsers = await _userRepository.GetByTenantIdAsync(tenantId, cancellationToken);
            foreach (var u in tenantUsers.Where(u => u.Module == AppModule.MyPos && u.IsActive))
            {
                u.IsActive = false;
                await _userRepository.UpdateAsync(u, cancellationToken);
            }
            removed.Add("MyPos → SnelStart");
        }

        // Tenant bijwerken
        var updateReq = new UpdateTenantRequest
        {
            Name = tenant.Name,
            CompanyName = tenant.CompanyName,
            ContactName = tenant.ContactName,
            Email = tenant.Email,
            Phone = tenant.Phone,
            KvkNumber = tenant.KvkNumber,
            GoDutchEnabled = request.GoDutchEnabled,
            MyPosEnabled = request.MyPosEnabled,
            Status = tenant.Status,
            IsActive = tenant.IsActive,
            TrialStartsUtc = tenant.TrialStartsUtc,
            TrialEndsUtc = tenant.TrialEndsUtc,
            OnboardingCompletedUtc = tenant.OnboardingCompletedUtc
        };
        await _tenantService.UpdateAsync(tenantId, updateReq, cancellationToken);

        // E-mail aan klant
        if (!string.IsNullOrWhiteSpace(tenant.Email))
        {
            var html = BuildSubscriptionChangedEmail(
                tenant.ContactName,
                tenant.CompanyName ?? tenant.Name,
                added, removed, newCredentials,
                request.GoDutchEnabled, request.MyPosEnabled);

            await _emailService.SendToAsync(
                tenant.Email,
                tenant.ContactName ?? string.Empty,
                "Abonnement gewijzigd — GoDutch MyPos SnelStart",
                html,
                cancellationToken);
        }

        // Admin-notificatie
        var changes = string.Join(", ",
            added.Select(m => $"+{m}").Concat(removed.Select(m => $"-{m}")));
        await _emailService.SendAsync(
            $"Abonnement gewijzigd: {tenant.CompanyName ?? tenant.Name}",
            $"""
            Klant heeft abonnement gewijzigd.

            Bedrijf:   {tenant.CompanyName ?? tenant.Name}
            Wijziging: {changes}
            Modules nu: {(request.GoDutchEnabled ? "GoDutch " : "")}{(request.MyPosEnabled ? "MyPos" : "")}
            Tijdstip:  {DateTime.Now:dd-MM-yyyy HH:mm}
            """,
            cancellationToken);

        return Ok(newCredentials);
    }

    public sealed class SubscriptionCancelRequest
    {
        public string? Reason { get; set; }
    }

    [HttpPost("{tenantId:guid}/subscription/cancel")]
    public async Task<ActionResult> CancelSubscription(
        Guid tenantId,
        [FromBody] SubscriptionCancelRequest request,
        CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
            return NotFound();

        // Alle gebruikers deactiveren
        var tenantUsers = await _userRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        foreach (var u in tenantUsers.Where(u => u.IsActive))
        {
            u.IsActive = false;
            await _userRepository.UpdateAsync(u, cancellationToken);
        }

        // Tenant op opgezegd zetten
        var updateReq = new UpdateTenantRequest
        {
            Name = tenant.Name,
            CompanyName = tenant.CompanyName,
            ContactName = tenant.ContactName,
            Email = tenant.Email,
            Phone = tenant.Phone,
            KvkNumber = tenant.KvkNumber,
            GoDutchEnabled = tenant.GoDutchEnabled,
            MyPosEnabled = tenant.MyPosEnabled,
            Status = "Cancelled",
            IsActive = false,
            TrialStartsUtc = tenant.TrialStartsUtc,
            TrialEndsUtc = tenant.TrialEndsUtc,
            OnboardingCompletedUtc = tenant.OnboardingCompletedUtc
        };
        await _tenantService.UpdateAsync(tenantId, updateReq, cancellationToken);
        _logger.LogInformation("Abonnement opgezegd door tenant {TenantId} ({CompanyName})", tenantId, tenant.CompanyName);

        // E-mail aan klant
        if (!string.IsNullOrWhiteSpace(tenant.Email))
        {
            var html = BuildCancellationEmail(tenant.ContactName, tenant.CompanyName ?? tenant.Name);
            await _emailService.SendToAsync(
                tenant.Email,
                tenant.ContactName ?? string.Empty,
                "Abonnement opgezegd — GoDutch MyPos SnelStart",
                html,
                cancellationToken);
        }

        // Admin-notificatie
        await _emailService.SendAsync(
            $"Abonnement opgezegd: {tenant.CompanyName ?? tenant.Name}",
            $"""
            Klant heeft abonnement opgezegd.

            Bedrijf:   {tenant.CompanyName ?? tenant.Name}
            Reden:     {request.Reason ?? "—"}
            Tijdstip:  {DateTime.Now:dd-MM-yyyy HH:mm}
            """,
            cancellationToken);

        return NoContent();
    }

    // ── Hulpmethoden ────────────────────────────────────────────────

    private static string GenerateBaseUsername(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "gebruiker";
        var slug = new string(name.ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .Take(12)
            .ToArray());
        return string.IsNullOrEmpty(slug) ? "gebruiker" : slug;
    }

    private async Task<string> GetUniqueUsernameAsync(string baseUsername, CancellationToken cancellationToken)
    {
        if (await _userRepository.GetByUsernameAsync(baseUsername, cancellationToken) is null)
            return baseUsername;

        for (var i = 2; i <= 99; i++)
        {
            var candidate = $"{baseUsername}{i}";
            if (await _userRepository.GetByUsernameAsync(candidate, cancellationToken) is null)
                return candidate;
        }

        return $"{baseUsername}_{Guid.NewGuid():N}"[..16];
    }

    private static string GeneratePassword()
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$";
        var bytes = RandomNumberGenerator.GetBytes(14);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }

    private string BuildSubscriptionChangedEmail(
        string? contactName,
        string companyName,
        List<string> added,
        List<string> removed,
        List<NewModuleCredential> newCredentials,
        bool godutchActive,
        bool myposActive)
    {
        var portalUrl = _emailOptions.PortalUrl;
        var name = string.IsNullOrWhiteSpace(contactName) ? companyName : contactName;

        var addedHtml = added.Any()
            ? $"<p style='color:#166534;'>✅ Toegevoegd: {string.Join(", ", added)}</p>"
            : string.Empty;
        var removedHtml = removed.Any()
            ? $"<p style='color:#991B1B;'>❌ Verwijderd: {string.Join(", ", removed)}</p>"
            : string.Empty;

        var credHtml = new StringBuilder();
        foreach (var c in newCredentials)
        {
            credHtml.Append($"""
                <div style="background:#F0F7FF;border-left:4px solid #0066CC;border-radius:4px;padding:14px 18px;margin:14px 0;">
                  <div style="font-weight:700;color:#0057AD;margin-bottom:8px;">{c.Label} — nieuwe inloggegevens</div>
                  <table style="border-collapse:collapse;">
                    <tr><td style="color:#6B7280;font-size:13px;padding:3px 0;width:140px;">Gebruikersnaam:</td>
                        <td style="font-family:monospace;font-weight:600;">{c.Username}</td></tr>
                    <tr><td style="color:#6B7280;font-size:13px;padding:3px 0;">Wachtwoord:</td>
                        <td style="font-family:monospace;font-weight:600;">{c.Password}</td></tr>
                  </table>
                </div>
                """);
        }

        var activeModules = new List<string>();
        if (godutchActive) activeModules.Add("GoDutch → SnelStart");
        if (myposActive) activeModules.Add("MyPos → SnelStart");

        return $"""
            <!DOCTYPE html><html lang="nl"><head><meta charset="utf-8"></head>
            <body style="margin:0;padding:0;background:#F3F7FB;font-family:Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#F3F7FB;padding:32px 0;">
                <tr><td align="center">
                  <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,46,92,.10);">
                    <tr><td style="background:linear-gradient(135deg,#0066CC,#003D8A);padding:28px 40px;">
                      <div style="font-size:20px;font-weight:800;color:#fff;">GoDutch MyPos SnelStart</div>
                      <div style="font-size:12px;color:#A8CCEE;margin-top:3px;">Padans B.V. Nijmegen</div>
                    </td></tr>
                    <tr><td style="padding:32px 40px;">
                      <h1 style="font-size:18px;font-weight:700;color:#111827;margin:0 0 12px 0;">Abonnement gewijzigd</h1>
                      <p style="color:#374151;font-size:14px;line-height:1.6;margin:0 0 18px 0;">
                        Beste {name},<br><br>
                        Uw abonnement voor <strong>{companyName}</strong> is gewijzigd. Hieronder ziet u een overzicht.
                      </p>
                      {addedHtml}
                      {removedHtml}
                      {credHtml}
                      <div style="background:#F3F4F6;border-radius:8px;padding:14px 18px;margin:18px 0;">
                        <strong style="font-size:13px;color:#374151;">Actieve modules:</strong><br>
                        <span style="font-size:13px;color:#374151;">{string.Join(", ", activeModules)}</span>
                      </div>
                      {(newCredentials.Any() ? "<p style='color:#374151;font-size:13px;'>⚠️ Wijzig uw nieuwe wachtwoord na de eerste inlog via <em>Mijn account</em> in het portaal.</p>" : "")}
                      <div style="text-align:center;margin:24px 0;">
                        <a href="{portalUrl}" style="display:inline-block;background:#0066CC;color:#fff;text-decoration:none;padding:12px 28px;border-radius:8px;font-weight:700;font-size:14px;">Naar het portaal</a>
                      </div>
                    </td></tr>
                    <tr><td style="background:#F9FAFB;border-top:1px solid #E5E7EB;padding:16px 40px;text-align:center;">
                      <p style="color:#9CA3AF;font-size:11px;margin:0;">Padans B.V. Nijmegen &mdash; <a href="{portalUrl}" style="color:#9CA3AF;">{portalUrl}</a></p>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body></html>
            """;
    }

    private string BuildCancellationEmail(string? contactName, string companyName)
    {
        var portalUrl = _emailOptions.PortalUrl;
        var name = string.IsNullOrWhiteSpace(contactName) ? companyName : contactName;
        return $"""
            <!DOCTYPE html><html lang="nl"><head><meta charset="utf-8"></head>
            <body style="margin:0;padding:0;background:#F3F7FB;font-family:Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#F3F7FB;padding:32px 0;">
                <tr><td align="center">
                  <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,46,92,.10);">
                    <tr><td style="background:linear-gradient(135deg,#0066CC,#003D8A);padding:28px 40px;">
                      <div style="font-size:20px;font-weight:800;color:#fff;">GoDutch MyPos SnelStart</div>
                    </td></tr>
                    <tr><td style="padding:32px 40px;">
                      <h1 style="font-size:18px;font-weight:700;color:#111827;margin:0 0 12px 0;">Abonnement opgezegd</h1>
                      <p style="color:#374151;font-size:14px;line-height:1.6;margin:0 0 16px 0;">
                        Beste {name},<br><br>
                        Uw abonnement voor <strong>{companyName}</strong> is per direct opgezegd. Uw toegang is beëindigd.
                      </p>
                      <p style="color:#374151;font-size:14px;line-height:1.6;margin:0 0 24px 0;">
                        Wilt u in de toekomst opnieuw starten? U kunt altijd een nieuw proefaccount aanmaken via
                        <a href="{portalUrl}?register=1" style="color:#0066CC;">{portalUrl}</a>.
                      </p>
                      <p style="color:#374151;font-size:14px;">Bedankt voor het gebruik van onze dienst. We wensen u succes.</p>
                    </td></tr>
                    <tr><td style="background:#F9FAFB;border-top:1px solid #E5E7EB;padding:16px 40px;text-align:center;">
                      <p style="color:#9CA3AF;font-size:11px;margin:0;">Padans B.V. Nijmegen &mdash; <a href="{portalUrl}" style="color:#9CA3AF;">{portalUrl}</a></p>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body></html>
            """;
    }

    private string BuildWelcomeEmail(
        string? contactName,
        string companyName,
        IReadOnlyList<(string Label, string Username, string Password)> credentials,
        string trialEnds)
    {
        var portalUrl = _emailOptions.PortalUrl;
        var name = string.IsNullOrWhiteSpace(contactName) ? companyName : contactName;

        var credentialsHtml = new StringBuilder();
        foreach (var (label, username, password) in credentials)
        {
            credentialsHtml.Append($"""
                <div style="background:#F0F7FF;border-left:4px solid #0066CC;border-radius:4px;padding:16px 20px;margin:16px 0;">
                  <div style="font-weight:700;color:#0057AD;margin-bottom:10px;font-size:15px;">{label}</div>
                  <table style="border-collapse:collapse;width:100%;">
                    <tr>
                      <td style="padding:4px 0;color:#6B7280;font-size:13px;width:140px;">Module:</td>
                      <td style="padding:4px 0;font-weight:600;font-size:13px;">{label}</td>
                    </tr>
                    <tr>
                      <td style="padding:4px 0;color:#6B7280;font-size:13px;">Gebruikersnaam:</td>
                      <td style="padding:4px 0;font-weight:600;font-size:14px;font-family:monospace;color:#111;">{username}</td>
                    </tr>
                    <tr>
                      <td style="padding:4px 0;color:#6B7280;font-size:13px;">Wachtwoord:</td>
                      <td style="padding:4px 0;font-weight:600;font-size:14px;font-family:monospace;color:#111;">{password}</td>
                    </tr>
                  </table>
                </div>
                """);
        }

        return $"""
            <!DOCTYPE html>
            <html lang="nl">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
            <body style="margin:0;padding:0;background:#F3F7FB;font-family:Arial,Helvetica,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#F3F7FB;padding:32px 0;">
                <tr><td align="center">
                  <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 4px 20px rgba(0,46,92,.10);">

                    <!-- Header -->
                    <tr>
                      <td style="background:linear-gradient(135deg,#0066CC,#003D8A);padding:32px 40px;">
                        <div style="font-size:22px;font-weight:800;color:#ffffff;letter-spacing:-.01em;">GoDutch MyPos SnelStart</div>
                        <div style="font-size:13px;color:#A8CCEE;margin-top:4px;">Padans B.V. Nijmegen</div>
                      </td>
                    </tr>

                    <!-- Body -->
                    <tr>
                      <td style="padding:36px 40px;">
                        <h1 style="font-size:20px;font-weight:700;color:#111827;margin:0 0 8px 0;">Welkom, {name}!</h1>
                        <p style="color:#6B7280;font-size:14px;line-height:1.6;margin:0 0 24px 0;">
                          Uw account voor <strong style="color:#111827;">{companyName}</strong> is aangemaakt.
                          Hieronder vindt u uw inloggegevens. Bewaar deze e-mail goed.
                        </p>

                        {credentialsHtml}

                        <!-- Login knop -->
                        <div style="text-align:center;margin:28px 0;">
                          <a href="{portalUrl}" style="display:inline-block;background:#0066CC;color:#ffffff;text-decoration:none;padding:14px 32px;border-radius:8px;font-weight:700;font-size:15px;">
                            Inloggen op het portaal
                          </a>
                        </div>

                        <!-- Proefperiode -->
                        <div style="background:#FFF8E1;border:1px solid #FFD54F;border-radius:6px;padding:14px 18px;margin-bottom:24px;">
                          <strong style="color:#E65100;font-size:13px;">⏱ Proefperiode</strong>
                          <p style="color:#5D4037;font-size:13px;margin:6px 0 8px 0;line-height:1.5;">
                            Uw gratis proefaccount is geldig tot <strong>{trialEnds}</strong>.
                          </p>
                          <p style="color:#5D4037;font-size:13px;margin:0;line-height:1.5;">
                            Wilt u daarna doorgaan? Bekijk onze abonnementsprijzen en kies het abonnement dat bij u past:<br>
                            <a href="https://landing.padans.eu/abonnementen.html" style="color:#0066CC;font-weight:600;">landing.padans.eu/abonnementen.html</a>
                          </p>
                        </div>

                        <!-- Tip -->
                        <p style="color:#9CA3AF;font-size:12px;line-height:1.6;margin:0;">
                          🔒 Tip: Wijzig uw wachtwoord na de eerste inlog voor extra beveiliging.<br>
                          Heeft u vragen? Stuur een e-mail naar <a href="mailto:{_emailOptions.FromAddress}" style="color:#0066CC;">{_emailOptions.FromAddress}</a>.
                        </p>
                      </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                      <td style="background:#F9FAFB;border-top:1px solid #E5E7EB;padding:20px 40px;text-align:center;">
                        <p style="color:#9CA3AF;font-size:11px;margin:0;">
                          Padans B.V. Nijmegen &mdash; <a href="{portalUrl}" style="color:#9CA3AF;">{portalUrl}</a>
                        </p>
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
