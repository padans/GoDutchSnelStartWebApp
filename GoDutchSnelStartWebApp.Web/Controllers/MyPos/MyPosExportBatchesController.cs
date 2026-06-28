using GoDutchSnelStartWebApp.Application.MyPos.Dtos;
using GoDutchSnelStartWebApp.Application.MyPos.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GoDutchSnelStartWebApp.Web.Controllers.MyPos;

[ApiController]
[Route("api/tenants/{tenantId:guid}/mypos/export-batches")]
public sealed class MyPosExportBatchesController : ControllerBase
{
    private readonly IMyPosExportBatchService _exportBatchService;
    private readonly ILogger<MyPosExportBatchesController> _logger;
    private readonly IMyPosExportBatchExportService _exportService;

    public MyPosExportBatchesController(
    IMyPosExportBatchService exportBatchService,
    IMyPosExportBatchExportService exportService,
    ILogger<MyPosExportBatchesController> logger)
    {
        _exportBatchService = exportBatchService;
        _exportService = exportService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MyPosExportBatchDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<MyPosExportBatchDto>>> GetByTenantAsync(
        Guid tenantId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
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

        var batches = await _exportBatchService.GetByTenantAsync(
            tenantId,
            fromUtc,
            toUtc,
            cancellationToken);

        return Ok(batches);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MyPosExportBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MyPosExportBatchDto>> GetByIdAsync(
        Guid tenantId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var batch = await _exportBatchService.GetByIdAsync(id, cancellationToken);

        if (batch is null || batch.TenantId != tenantId)
        {
            return NotFound();
        }

        return Ok(batch);
    }

    [HttpPost("concept")]
    [ProducesResponseType(typeof(MyPosExportBatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MyPosExportBatchDto>> CreateConceptAsync(
        Guid tenantId,
        [FromBody] CreateMyPosExportBatchRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.FromUtc == default || request.ToUtc == default)
        {
            return BadRequest("FromUtc en ToUtc zijn verplicht.");
        }

        if (request.ToUtc <= request.FromUtc)
        {
            return BadRequest("ToUtc moet later zijn dan FromUtc.");
        }

        _logger.LogInformation(
            "myPOS exportbatch concept aanmaken gestart. TenantId: {TenantId}, FromUtc: {FromUtc}, ToUtc: {ToUtc}, IncludeExported: {IncludeExported}.",
            tenantId,
            request.FromUtc,
            request.ToUtc,
            request.IncludeExported);

        var batch = await _exportBatchService.CreateConceptBatchAsync(
            tenantId,
            request,
            cancellationToken);

        _logger.LogInformation(
            "myPOS exportbatch concept aangemaakt. BatchId: {BatchId}, Status: {Status}, Ready: {Ready}, Lines: {LineCount}.",
            batch.Id,
            batch.Status,
            batch.IsReadyForExport,
            batch.LineCount);

        return Ok(batch);
    }

    [HttpPost("{batchId:guid}/export/snelstart-bankboek")]
    public async Task<ActionResult<MyPosExportBatchExportResultDto>> ExportToSnelStartBankboekAsync(
    Guid tenantId,
    Guid batchId,
    CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "myPOS exportbatch export naar SnelStart-bankboek gestart. TenantId: {TenantId}, BatchId: {BatchId}.",
            tenantId,
            batchId);

        var result = await _exportService.ExportToSnelStartBankboekAsync(
            tenantId,
            batchId,
            cancellationToken);

        return Ok(result);
    }
}
