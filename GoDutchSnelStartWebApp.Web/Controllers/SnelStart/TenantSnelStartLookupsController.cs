using GoDutchSnelStartWebApp.Application.SnelStartLookups;
using GoDutchSnelStartWebApp.Application.SnelStartLookups.Dtos;
using GoDutchSnelStartWebApp.Application.SnelStartLookups.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GoDutchSnelStartWebApp.Web.Controllers.SnelStart;

[ApiController]
[Route("api/tenants/{tenantId:guid}/snelstart/lookups")]
public sealed class TenantSnelStartLookupsController : ControllerBase
{
    private readonly ISnelStartLookupService _snelStartLookupService;
    private readonly ILogger<TenantSnelStartLookupsController> _logger;

    public TenantSnelStartLookupsController(
        ISnelStartLookupService snelStartLookupService,
        ILogger<TenantSnelStartLookupsController> logger)
    {
        _snelStartLookupService = snelStartLookupService;
        _logger = logger;
    }

    [HttpGet("grootboeken")]
    [ProducesResponseType(typeof(IReadOnlyList<SnelStartGrootboekLookupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IReadOnlyList<SnelStartGrootboekLookupDto>>> GetGrootboeken(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug(
                "Tenantgerichte SnelStart grootboeken lookup gestart. TenantId: {TenantId}.",
                tenantId);

            var result = await _snelStartLookupService.GetTenantGrootboekenAsync(
                tenantId,
                cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (SnelStartLookupException ex)
        {
            return Problem(
                title: "SnelStart lookup mislukt",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    [HttpPost("grootboeken")]
    [ProducesResponseType(typeof(SnelStartGrootboekLookupDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<SnelStartGrootboekLookupDto>> CreateGrootboek(
        Guid tenantId,
        [FromBody] CreateSnelStartGrootboekRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug(
                "Tenantgericht SnelStart grootboek aanmaken gestart. TenantId: {TenantId}, Nummer: {Nummer}.",
                tenantId,
                request.Nummer);

            var result = await _snelStartLookupService.CreateTenantGrootboekAsync(
                tenantId,
                request,
                cancellationToken);

            return Created(
                $"/api/tenants/{tenantId}/snelstart/lookups/grootboeken",
                result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (SnelStartLookupException ex)
        {
            return Problem(
                title: "SnelStart grootboek aanmaken mislukt",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

}
