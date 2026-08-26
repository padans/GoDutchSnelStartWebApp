using System.Security.Cryptography;
using GoDutchSnelStartWebApp.Application.Abstractions.Repositories;
using GoDutchSnelStartWebApp.Application.AppUsers.Dtos;
using GoDutchSnelStartWebApp.Application.AppUsers.Interfaces;
using GoDutchSnelStartWebApp.Application.Configuration;
using GoDutchSnelStartWebApp.Application.Notifications.Interfaces;
using GoDutchSnelStartWebApp.Application.Tenants.Interfaces;
using GoDutchSnelStartWebApp.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GoDutchSnelStartWebApp.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AppUsersController : ControllerBase
{
    private readonly IAppUserService _appUserService;
    private readonly IEmailNotificationService _emailService;
    private readonly ITenantService _tenantService;
    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<AppUsersController> _logger;

    public AppUsersController(
        IAppUserService appUserService,
        IEmailNotificationService emailService,
        ITenantService tenantService,
        IPasswordResetTokenRepository tokenRepository,
        IOptions<EmailOptions> emailOptions,
        ILogger<AppUsersController> logger)
    {
        _appUserService = appUserService;
        _emailService = emailService;
        _tenantService = tenantService;
        _tokenRepository = tokenRepository;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppUserDto>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _appUserService.GetAllAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AppUserDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _appUserService.GetByIdAsync(id, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateAppUserRequest request, CancellationToken cancellationToken)
    {
        var id = await _appUserService.CreateAsync(request.Username, request.Password, request.Module, cancellationToken: cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, null);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateAppUserRequest request, CancellationToken cancellationToken)
    {
        await _appUserService.UpdateAsync(id, request.Username, request.Module, request.IsActive, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/password")]
    public async Task<ActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await _appUserService.ChangePasswordAsync(id, request.NewPassword, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _appUserService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("login")]
    public async Task<ActionResult<AppUserDto>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _appUserService.ValidateCredentialsAsync(request.Username, request.Password, cancellationToken);
        return user is null ? Unauthorized() : Ok(user);
    }

    [HttpPost("self/change-password")]
    public async Task<ActionResult> SelfChangePassword([FromBody] SelfChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            return BadRequest("Nieuw wachtwoord moet minimaal 8 tekens zijn.");

        var user = await _appUserService.ValidateCredentialsAsync(request.Username, request.CurrentPassword, cancellationToken);
        if (user is null)
            return Unauthorized("Huidig wachtwoord is onjuist.");

        await _appUserService.ChangePasswordAsync(user.Id, request.NewPassword, cancellationToken);
        _logger.LogInformation("Wachtwoord zelfgewijzigd door gebruiker {Username}", request.Username);

        // Tenant email opzoeken via de TenantId van de gebruiker (server-side, client hoeft niets mee te sturen)
        string? customerEmail = null;
        if (user.TenantId.HasValue)
        {
            var tenant = await _tenantService.GetByIdAsync(user.TenantId.Value, cancellationToken);
            customerEmail = tenant?.Email;
        }

        if (!string.IsNullOrWhiteSpace(customerEmail))
        {
            var html = BuildPasswordChangedEmail(request.Username);
            await _emailService.SendToAsync(
                customerEmail,
                request.Username,
                "Wachtwoord gewijzigd — GoDutch MyPos SnelStart portaal",
                html,
                cancellationToken);
        }

        await _emailService.SendAsync(
            $"Wachtwoord gewijzigd: {request.Username}",
            $"Gebruiker {request.Username} heeft het wachtwoord gewijzigd op {DateTime.Now:dd-MM-yyyy HH:mm}.",
            cancellationToken);

        return NoContent();
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        // Altijd 200 teruggeven om email-enumeration te voorkomen
        if (string.IsNullOrWhiteSpace(request.Email))
            return Ok();

        try
        {
            var tenant = await _tenantService.GetByEmailAsync(request.Email.Trim(), cancellationToken);
            if (tenant is null)
            {
                _logger.LogDebug("Forgot password: geen tenant gevonden voor email {Email}", request.Email);
                return Ok();
            }

            // Zoek een actieve gebruiker voor dit tenant
            var allUsers = await _appUserService.GetAllAsync(cancellationToken);
            var user = allUsers.FirstOrDefault(u => u.TenantId == tenant.Id && u.IsActive);
            if (user is null)
            {
                _logger.LogDebug("Forgot password: geen actieve gebruiker voor tenant {TenantId}", tenant.Id);
                return Ok();
            }

            var tokenValue = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            var now = DateTime.UtcNow;
            var resetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = tokenValue,
                ExpiresUtc = now.AddHours(1),
                CreatedUtc = now
            };

            await _tokenRepository.CreateAsync(resetToken, cancellationToken);

            var resetUrl = $"{_emailOptions.PortalUrl}/reset-password?token={tokenValue}";
            var html = BuildPasswordResetEmail(user.Username, resetUrl);

            await _emailService.SendToAsync(
                tenant.Email!,
                tenant.ContactName ?? tenant.Name,
                "Wachtwoord opnieuw instellen — GoDutch MyPos SnelStart portaal",
                html,
                cancellationToken);

            _logger.LogInformation("Wachtwoord-resetlink verzonden naar {Email} voor gebruiker {Username}", request.Email, user.Username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fout bij verwerken forgot-password voor {Email}", request.Email);
        }

        return Ok();
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest("Token en nieuw wachtwoord zijn verplicht.");

        if (request.NewPassword.Length < 8)
            return BadRequest("Wachtwoord moet minimaal 8 tekens zijn.");

        var token = await _tokenRepository.GetByTokenAsync(request.Token, cancellationToken);
        if (token is null || !token.IsValid)
        {
            _logger.LogWarning("Ongeldige of verlopen resettoken ontvangen");
            return BadRequest("De link is ongeldig of verlopen. Vraag een nieuwe resetlink aan.");
        }

        await _appUserService.ChangePasswordAsync(token.UserId, request.NewPassword, cancellationToken);
        await _tokenRepository.MarkUsedAsync(token.Id, DateTime.UtcNow, cancellationToken);

        var user = await _appUserService.GetByIdAsync(token.UserId, cancellationToken);
        _logger.LogInformation("Wachtwoord gereset voor gebruiker {UserId} via reset-token", token.UserId);

        if (user?.TenantId.HasValue == true)
        {
            var tenant = await _tenantService.GetByIdAsync(user.TenantId.Value, cancellationToken);
            if (!string.IsNullOrWhiteSpace(tenant?.Email))
            {
                var html = BuildPasswordChangedEmail(user.Username);
                await _emailService.SendToAsync(
                    tenant.Email,
                    tenant.ContactName ?? tenant.Name,
                    "Wachtwoord gewijzigd — GoDutch MyPos SnelStart portaal",
                    html,
                    cancellationToken);
            }
        }

        return Ok();
    }

    private string BuildPasswordResetEmail(string username, string resetUrl)
    {
        var portalUrl = _emailOptions.PortalUrl;
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
                      <td style="padding:32px 40px;">
                        <h1 style="font-size:18px;font-weight:700;color:#111827;margin:0 0 12px 0;">Wachtwoord vergeten?</h1>
                        <p style="color:#374151;font-size:14px;line-height:1.6;margin:0 0 16px 0;">
                          Er is een verzoek ontvangen om het wachtwoord van account
                          <strong style="font-family:monospace;color:#111;">{username}</strong> opnieuw in te stellen.
                        </p>
                        <p style="color:#374151;font-size:14px;line-height:1.6;margin:0 0 24px 0;">
                          Klik op onderstaande knop om een nieuw wachtwoord in te stellen.
                          De link is <strong>1 uur geldig</strong> en kan slechts eenmaal worden gebruikt.
                        </p>
                        <div style="text-align:center;margin-bottom:24px;">
                          <a href="{resetUrl}" style="display:inline-block;background:#0066CC;color:#ffffff;text-decoration:none;padding:14px 32px;border-radius:8px;font-weight:700;font-size:14px;">
                            Nieuw wachtwoord instellen
                          </a>
                        </div>
                        <p style="color:#6B7280;font-size:12px;line-height:1.6;margin:0 0 8px 0;">
                          Werkt de knop niet? Kopieer dan deze link naar uw browser:<br>
                          <a href="{resetUrl}" style="color:#0066CC;word-break:break-all;">{resetUrl}</a>
                        </p>
                        <p style="color:#6B7280;font-size:12px;line-height:1.6;margin:0;">
                          Heeft u dit verzoek niet zelf gedaan? Dan kunt u deze e-mail veilig negeren.
                          Neem bij twijfel contact op via <a href="mailto:{_emailOptions.FromAddress}" style="color:#0066CC;">{_emailOptions.FromAddress}</a>.
                        </p>
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

    private string BuildPasswordChangedEmail(string username)
    {
        var portalUrl = _emailOptions.PortalUrl;
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
                      <td style="padding:32px 40px;">
                        <h1 style="font-size:18px;font-weight:700;color:#111827;margin:0 0 12px 0;">Wachtwoord gewijzigd</h1>
                        <p style="color:#374151;font-size:14px;line-height:1.6;margin:0 0 16px 0;">
                          Het wachtwoord voor account <strong style="font-family:monospace;color:#111;">{username}</strong>
                          is gewijzigd op <strong>{DateTime.Now:dd-MM-yyyy HH:mm}</strong>.
                        </p>
                        <p style="color:#374151;font-size:14px;line-height:1.6;margin:0 0 24px 0;">
                          Was dit niet u? Neem dan direct contact op via
                          <a href="mailto:{_emailOptions.FromAddress}" style="color:#0066CC;">{_emailOptions.FromAddress}</a>.
                        </p>
                        <div style="text-align:center;">
                          <a href="{portalUrl}" style="display:inline-block;background:#0066CC;color:#ffffff;text-decoration:none;padding:12px 28px;border-radius:8px;font-weight:700;font-size:14px;">
                            Naar het portaal
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
