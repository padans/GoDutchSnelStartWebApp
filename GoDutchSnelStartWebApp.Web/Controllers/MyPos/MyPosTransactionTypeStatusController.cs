using GoDutchSnelStartWebApp.Application.MyPos.Dtos;
using GoDutchSnelStartWebApp.Application.MyPos.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GoDutchSnelStartWebApp.Web.Controllers.MyPos;

[ApiController]
[Route("api/tenants/{tenantId:guid}/mypos/transaction-types/status")]
public sealed class MyPosTransactionTypeStatusController : ControllerBase
{
    private readonly IMyPosTransactionTypeMappingService _mappingService;
    private readonly ILogger<MyPosTransactionTypeStatusController> _logger;

    public MyPosTransactionTypeStatusController(
        IMyPosTransactionTypeMappingService mappingService,
        ILogger<MyPosTransactionTypeStatusController> logger)
    {
        _mappingService = mappingService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(MyPosTransactionTypeStatusResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MyPosTransactionTypeStatusResultDto>> GetAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting myPOS transaction type status for tenant {TenantId}", tenantId);

        try
        {
            var result = await _mappingService.GetTransactionTypeStatusAsync(tenantId, cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
