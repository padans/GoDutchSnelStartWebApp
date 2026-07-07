using GoDutchSnelStartWebApp.Application.GoDutchLeads.Dtos;
using GoDutchSnelStartWebApp.Application.GoDutchLeads.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GoDutchSnelStartWebApp.Web.Controllers;

[ApiController]
[Route("api/godutch-leads")]
public sealed class GoDutchLeadsController : ControllerBase
{
    private readonly IGoDutchLeadService _service;
    private readonly ILogger<GoDutchLeadsController> _logger;

    public GoDutchLeadsController(
        IGoDutchLeadService service,
        ILogger<GoDutchLeadsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateGoDutchLeadRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BedrijfsNaam))
            return BadRequest("Bedrijfsnaam is verplicht.");
        if (string.IsNullOrWhiteSpace(request.ContactPersoon))
            return BadRequest("Contactpersoon is verplicht.");
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("E-mailadres is verplicht.");

        await _service.CreateAsync(request, cancellationToken);

        _logger.LogInformation("GoDutch lead aanmelding verwerkt: {Email}", request.Email);

        return StatusCode(StatusCodes.Status201Created);
    }
}
