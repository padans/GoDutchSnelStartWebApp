using GoDutchSnelStartWebApp.Application.MyPos.Dtos;
using GoDutchSnelStartWebApp.Application.MyPos.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GoDutchSnelStartWebApp.Web.Controllers.MyPos;

[ApiController]
[Route("api/tenants/{tenantId:guid}/mypos/transaction-type-mappings")]
public sealed class MyPosTransactionTypeMappingsController : ControllerBase
{
    private readonly IMyPosTransactionTypeMappingService _mappingService;
    private readonly ILogger<MyPosTransactionTypeMappingsController> _logger;

    public MyPosTransactionTypeMappingsController(
        IMyPosTransactionTypeMappingService mappingService,
        ILogger<MyPosTransactionTypeMappingsController> logger)
    {
        _mappingService = mappingService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MyPosTransactionTypeMappingDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MyPosTransactionTypeMappingDto>>> GetByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Getting myPOS transaction type mappings for tenant {TenantId}",
            tenantId);

        var mappings = await _mappingService.GetByTenantAsync(
            tenantId,
            cancellationToken);

        return Ok(mappings);
    }

    [HttpPost]
    [ProducesResponseType(typeof(MyPosTransactionTypeMappingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MyPosTransactionTypeMappingDto>> UpsertAsync(
        Guid tenantId,
        [FromBody] UpsertMyPosTransactionTypeMappingRequest request,
        CancellationToken cancellationToken)
    {
        var mapping = await _mappingService.UpsertAsync(
            tenantId,
            request,
            cancellationToken);

        return Ok(mapping);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        Guid tenantId,
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mappingService.DeleteAsync(
            tenantId,
            id,
            cancellationToken);

        return NoContent();
    }
}