using GoDutchSnelStartWebApp.Application.GoDutchConnections.Dtos;
using GoDutchSnelStartWebApp.Application.GoDutchConnections.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GoDutchSnelStartWebApp.Web.Controllers;

[ApiController]
[Route("api/tenants/{tenantId:guid}/godutch/connection")]
public sealed class TenantGoDutchConnectionsController : ControllerBase
{
    private readonly ITenantGoDutchConnectionService _tenantGoDutchConnectionService;
    private readonly ILogger<TenantGoDutchConnectionsController> _logger;

    public TenantGoDutchConnectionsController(
        ITenantGoDutchConnectionService tenantGoDutchConnectionService,
        ILogger<TenantGoDutchConnectionsController> logger)
    {
        _tenantGoDutchConnectionService = tenantGoDutchConnectionService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(TenantGoDutchConnectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TenantGoDutchConnectionDto>> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting GoDutch connection for tenant {TenantId}", tenantId);

        var connection = await _tenantGoDutchConnectionService.GetByTenantIdAsync(
            tenantId,
            cancellationToken);

        if (connection is null)
        {
            return NotFound();
        }

        return Ok(connection);
    }

    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> CreateAsync(
        Guid tenantId,
        [FromBody] CreateTenantGoDutchConnectionRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _tenantGoDutchConnectionService.CreateAsync(
            tenantId,
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetByTenantIdAsync),
            new { tenantId },
            new { id });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(
        Guid tenantId,
        Guid id,
        [FromBody] UpdateTenantGoDutchConnectionRequest request,
        CancellationToken cancellationToken)
    {
        await _tenantGoDutchConnectionService.UpdateAsync(
            tenantId,
            id,
            request,
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        Guid tenantId,
        Guid id,
        CancellationToken cancellationToken)
    {
        await _tenantGoDutchConnectionService.DeleteAsync(
            tenantId,
            id,
            cancellationToken);

        return NoContent();
    }
}
