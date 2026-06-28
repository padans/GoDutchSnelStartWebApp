using GoDutchSnelStartWebApp.Application.SnelStartLookups;
using GoDutchSnelStartWebApp.Application.SnelStartLookups.Dtos;
using GoDutchSnelStartWebApp.Application.SnelStartLookups.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Serilog.Core;

namespace GoDutchSnelStartWebApp.Web.Controllers;

[ApiController]
[Route("api/tenants/{tenantId:guid}/bankaccounts/{bankAccountId:guid}/snelstart/lookups")]
public sealed class SnelStartLookupsController : ControllerBase
{
    private readonly ILogger<SnelStartLookupsController> _logger;
    private readonly ISnelStartLookupService _snelStartLookupService;

    public SnelStartLookupsController(ISnelStartLookupService snelStartLookupService, ILogger<SnelStartLookupsController> logger)
    {
        _snelStartLookupService = snelStartLookupService;
        _logger = logger;
    }

    [HttpGet("dagboeken")]
    [ProducesResponseType(typeof(IReadOnlyList<SnelStartDagboekLookupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IReadOnlyList<SnelStartDagboekLookupDto>>> GetDagboeken(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _snelStartLookupService.GetDagboekenAsync(
                tenantId,
                bankAccountId,
                cancellationToken);

            return Ok(result);
        }
        catch (SnelStartLookupException ex)
        {
            return Problem(
                title: "SnelStart lookup mislukt",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    [HttpGet("/api/tenants/{tenantId:guid}/snelstart/lookups/btw-tarieven")]
    [ProducesResponseType(typeof(IReadOnlyList<SnelStartBtwTariefLookupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IReadOnlyList<SnelStartBtwTariefLookupDto>>> GetTenantBtwTarieven(
    Guid tenantId,
    CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug(
                "Tenantgerichte SnelStart btw-tarieven lookup gestart. TenantId: {TenantId}.",
                tenantId);

            var result = await _snelStartLookupService.GetTenantBtwTarievenAsync(
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
                title: "SnelStart btw-tarieven lookup mislukt",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    [HttpGet("grootboeken")]
    [ProducesResponseType(typeof(IReadOnlyList<SnelStartGrootboekLookupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IReadOnlyList<SnelStartGrootboekLookupDto>>> GetGrootboeken(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _snelStartLookupService.GetGrootboekenAsync(
                tenantId,
                bankAccountId,
                cancellationToken);

            return Ok(result);
        }
        catch (SnelStartLookupException ex)
        {
            return Problem(
                title: "SnelStart lookup mislukt",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    [HttpGet("btw-tarieven")]
    [ProducesResponseType(typeof(IReadOnlyList<SnelStartBtwTariefLookupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IReadOnlyList<SnelStartBtwTariefLookupDto>>> GetBtwTarieven(
    Guid tenantId,
    CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug(
                "Tenantgerichte SnelStart btw-tarieven lookup gestart. TenantId: {TenantId}.",
                tenantId);

            var result = await _snelStartLookupService.GetTenantBtwTarievenAsync(
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
                title: "SnelStart btw-tarieven lookup mislukt",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    [HttpGet("/api/tenants/{tenantId:guid}/snelstart/lookups/dagboeken")]
    [ProducesResponseType(typeof(IReadOnlyList<SnelStartDagboekLookupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<IReadOnlyList<SnelStartDagboekLookupDto>>> GetTenantDagboeken(
    Guid tenantId,
    CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug(
                "Tenantgerichte SnelStart dagboeken lookup gestart. TenantId: {TenantId}.",
                tenantId);

            var result = await _snelStartLookupService.GetTenantDagboekenAsync(
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
                title: "SnelStart dagboeken lookup mislukt",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }
}