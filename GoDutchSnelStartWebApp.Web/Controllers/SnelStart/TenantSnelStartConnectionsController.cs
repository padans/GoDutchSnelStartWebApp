using GoDutchSnelStartWebApp.Application.SnelStartConnections.Dtos;
using GoDutchSnelStartWebApp.Application.SnelStartConnections.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GoDutchSnelStartWebApp.Web.Controllers.SnelStart;

[ApiController]
[Route("api/tenants/{tenantId:guid}/snelstart/connection")]
public sealed class TenantSnelStartConnectionsController : ControllerBase
{
    private readonly ITenantSnelStartConnectionService _tenantSnelStartConnectionService;
    private readonly ILogger<TenantSnelStartConnectionsController> _logger;

    public TenantSnelStartConnectionsController(
        ITenantSnelStartConnectionService tenantSnelStartConnectionService,
        ILogger<TenantSnelStartConnectionsController> logger)
    {
        _tenantSnelStartConnectionService = tenantSnelStartConnectionService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(TenantSnelStartConnectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TenantSnelStartConnectionDto>> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting tenant SnelStart connection for tenant {TenantId}", tenantId);

        var connection = await _tenantSnelStartConnectionService.GetByTenantIdAsync(
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
        [FromBody] CreateTenantSnelStartConnectionRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _tenantSnelStartConnectionService.CreateAsync(
            tenantId,
            request,
            cancellationToken);

        return Created(
            $"/api/tenants/{tenantId}/snelstart/connection",
            new { id });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(
        Guid tenantId,
        Guid id,
        [FromBody] UpdateTenantSnelStartConnectionRequest request,
        CancellationToken cancellationToken)
    {
        await _tenantSnelStartConnectionService.UpdateAsync(
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
        await _tenantSnelStartConnectionService.DeleteAsync(
            tenantId,
            id,
            cancellationToken);

        return NoContent();
    }
}
