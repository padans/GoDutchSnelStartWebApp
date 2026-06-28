using GoDutchSnelStartWebApp.Application.MyPos.Dtos;
using GoDutchSnelStartWebApp.Application.MyPos.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GoDutchSnelStartWebApp.Web.Controllers.MyPos;

[ApiController]
[Route("api/tenants/{tenantId:guid}/mypos/connection")]
public sealed class TenantMyPosConnectionsController : ControllerBase
{
    private readonly ITenantMyPosConnectionService _tenantMyPosConnectionService;
    private readonly ILogger<TenantMyPosConnectionsController> _logger;

    public TenantMyPosConnectionsController(
        ITenantMyPosConnectionService tenantMyPosConnectionService,
        ILogger<TenantMyPosConnectionsController> logger)
    {
        _tenantMyPosConnectionService = tenantMyPosConnectionService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(TenantMyPosConnectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TenantMyPosConnectionDto>> GetByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting myPOS connection for tenant {TenantId}", tenantId);

        var connection = await _tenantMyPosConnectionService.GetByTenantIdAsync(
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
     [FromBody] CreateTenantMyPosConnectionRequest request,
     CancellationToken cancellationToken)
    {
        var id = await _tenantMyPosConnectionService.CreateAsync(
            tenantId,
            request,
            cancellationToken);

        return Created(
            $"/api/tenants/{tenantId}/mypos/connection",
            new { id });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAsync(
        Guid tenantId,
        Guid id,
        [FromBody] UpdateTenantMyPosConnectionRequest request,
        CancellationToken cancellationToken)
    {
        await _tenantMyPosConnectionService.UpdateAsync(
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
        await _tenantMyPosConnectionService.DeleteAsync(
            tenantId,
            id,
            cancellationToken);

        return NoContent();
    }
}