using GoDutchSnelStartWebApp.Application.MyPos.Dtos;
using GoDutchSnelStartWebApp.Application.MyPos.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GoDutchSnelStartWebApp.Web.Controllers.MyPos;

[ApiController]
[Route("api/tenants/{tenantId:guid}/mypos/transactions")]
public sealed class MyPosTransactionsController : ControllerBase
{
    private readonly IMyPosTransactionImportService _importService;
    private readonly ILogger<MyPosTransactionsController> _logger;

    public MyPosTransactionsController(
        IMyPosTransactionImportService importService,
        ILogger<MyPosTransactionsController> logger)
    {
        _importService = importService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MyPosRawTransactionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MyPosRawTransactionDto>>> GetRawTransactionsAsync(
        Guid tenantId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        CancellationToken cancellationToken)
    {
        if (fromUtc == default || toUtc == default || toUtc <= fromUtc)
        {
            return BadRequest("Gebruik geldige fromUtc en toUtc query parameters.");
        }

        var result = await _importService.GetRawTransactionsAsync(
            tenantId,
            DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(toUtc, DateTimeKind.Utc),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("import")]
    [ProducesResponseType(typeof(MyPosTransactionImportResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MyPosTransactionImportResultDto>> ImportAsync(
        Guid tenantId,
        [FromQuery] Guid tenantMyPosConnectionId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        CancellationToken cancellationToken)
    {
        if (tenantMyPosConnectionId == Guid.Empty)
        {
            return BadRequest("tenantMyPosConnectionId is verplicht.");
        }

        if (fromUtc == default || toUtc == default || toUtc <= fromUtc)
        {
            return BadRequest("Gebruik geldige fromUtc en toUtc query parameters.");
        }

        _logger.LogInformation(
            "myPOS import gestart via API. TenantId: {TenantId}, ConnectionId: {ConnectionId}, FromUtc: {FromUtc}, ToUtc: {ToUtc}.",
            tenantId,
            tenantMyPosConnectionId,
            fromUtc,
            toUtc);

        var result = await _importService.FetchAndStoreAsync(
            tenantId,
            tenantMyPosConnectionId,
            DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc),
            DateTime.SpecifyKind(toUtc, DateTimeKind.Utc),
            cancellationToken);

        return Ok(result);
    }
}
