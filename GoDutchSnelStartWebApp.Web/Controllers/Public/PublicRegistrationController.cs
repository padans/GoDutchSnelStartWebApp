using GoDutchSnelStartWebApp.Application.Tenants.Dtos;
using GoDutchSnelStartWebApp.Application.Tenants.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GoDutchSnelStartWebApp.Web.Controllers.Public;

[ApiController]
[Route("api/public/register")]
public sealed class PublicRegistrationController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<PublicRegistrationController> _logger;

    public PublicRegistrationController(ITenantService tenantService, ILogger<PublicRegistrationController> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult> Register([FromBody] PublicRegistrationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyName))
            return BadRequest(new { error = "Bedrijfsnaam is verplicht." });

        if (!request.GoDutchEnabled && !request.MyPosEnabled)
            return BadRequest(new { error = "Selecteer minimaal één module." });

        _logger.LogInformation(
            "Publieke aanmelding ontvangen voor bedrijf {CompanyName} (email: {Email})",
            request.CompanyName, request.Email);

        var createRequest = new CreateTenantRequest
        {
            Name = request.CompanyName.Trim(),
            CompanyName = request.CompanyName.Trim(),
            ContactName = request.ContactName?.Trim(),
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(),
            Address = request.Address?.Trim(),
            PostalCode = request.PostalCode?.Trim(),
            City = request.City?.Trim(),
            KvkNumber = request.KvkNumber?.Trim(),
            GoDutchEnabled = request.GoDutchEnabled,
            MyPosEnabled = request.MyPosEnabled,
            IsTrial = true,
            TrialDurationDays = 7,
            IsActive = true
        };

        var id = await _tenantService.CreateAsync(createRequest, cancellationToken);

        _logger.LogInformation("Tenant {TenantId} aangemaakt via publieke aanmelding voor {CompanyName}", id, request.CompanyName);

        return Ok(new { tenantId = id, message = "Aanmelding ontvangen. Uw proefperiode van 7 dagen is gestart." });
    }
}

public sealed class PublicRegistrationRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? KvkNumber { get; set; }
    public bool GoDutchEnabled { get; set; }
    public bool MyPosEnabled { get; set; }
}
