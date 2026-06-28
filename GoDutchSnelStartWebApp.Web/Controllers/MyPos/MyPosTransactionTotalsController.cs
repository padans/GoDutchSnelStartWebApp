using GoDutchSnelStartWebApp.Application.MyPos.Dtos;
using GoDutchSnelStartWebApp.Application.MyPos.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GoDutchSnelStartWebApp.Web.Controllers.MyPos;

[ApiController]
[Route("api/tenants/{tenantId:guid}/mypos/transaction-totals")]
public sealed class MyPosTransactionTotalsController : ControllerBase
{
    private readonly IMyPosTransactionTotalService _totalService;
    private readonly ILogger<MyPosTransactionTotalsController> _logger;

    public MyPosTransactionTotalsController(
        IMyPosTransactionTotalService totalService,
        ILogger<MyPosTransactionTotalsController> logger)
    {
        _totalService = totalService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MyPosTransactionTotalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<MyPosTransactionTotalDto>>> GetTotalsAsync(
        Guid tenantId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] bool includeExported = false,
        CancellationToken cancellationToken = default)
    {
        if (fromUtc == default || toUtc == default)
        {
            return BadRequest("fromUtc en toUtc zijn verplicht.");
        }

        if (toUtc <= fromUtc)
        {
            return BadRequest("toUtc moet later zijn dan fromUtc.");
        }

        _logger.LogInformation(
            "myPOS totalisaties ophalen gestart. TenantId: {TenantId}, FromUtc: {FromUtc}, ToUtc: {ToUtc}, IncludeExported: {IncludeExported}.",
            tenantId,
            fromUtc,
            toUtc,
            includeExported);

        var totals = await _totalService.GetTotalsAsync(
            tenantId,
            fromUtc,
            toUtc,
            includeExported,
            cancellationToken);

        _logger.LogInformation(
            "myPOS totalisaties ophalen afgerond. TenantId: {TenantId}, Count: {Count}.",
            tenantId,
            totals.Count);

        return Ok(totals);
    }
}
