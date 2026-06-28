using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;
using GoDutchSnelStartWebApp.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace GoDutchSnelStartWebApp.Web.Controllers;

[ApiController]
[Route("api/godutch-transactions")]
public class GoDutchTransactionsController : ControllerBase
{
    private readonly IGoDutchMt940ExportService _mt940ExportService;
    private readonly IGoDutchCamt053ExportService _camt053ExportService;
    private readonly IGoDutchSnelStartImportService _snelStartImportService;
    private readonly IGoDutchSnelStartAutoImportService _snelStartAutoImportService;

    public GoDutchTransactionsController(
        IGoDutchMt940ExportService mt940ExportService,
        IGoDutchCamt053ExportService camt053ExportService,
        IGoDutchSnelStartImportService snelStartImportService,
        IGoDutchSnelStartAutoImportService snelStartAutoImportService)
    {
        _mt940ExportService = mt940ExportService;
        _camt053ExportService = camt053ExportService;
        _snelStartImportService = snelStartImportService;
        _snelStartAutoImportService = snelStartAutoImportService;
    }

    [HttpGet("{tenantId:guid}/{bankAccountId:guid}/mt940")]
    public async Task<IActionResult> GetMt940(
        Guid tenantId,
        Guid bankAccountId,
        [FromQuery] string iban,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct)
    {
        var result = await _mt940ExportService.GenerateMt940Async(
            tenantId,
            bankAccountId,
            iban,
            from,
            to,
            ct);

        return File(
            Encoding.UTF8.GetBytes(result),
            "text/plain",
            $"mt940_{DateTime.UtcNow:yyyyMMddHHmmss}.txt");
    }

    [HttpGet("{tenantId:guid}/{bankAccountId:guid}/camt053")]
    public async Task<IActionResult> GetCamt053(
        Guid tenantId,
        Guid bankAccountId,
        [FromQuery] string iban,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct)
    {
        var result = await _camt053ExportService.GenerateCamt053Async(
            tenantId,
            bankAccountId,
            iban,
            from,
            to,
            ct);

        return File(
            Encoding.UTF8.GetBytes(result),
            "application/xml",
            $"camt053_{DateTime.UtcNow:yyyyMMddHHmmss}.xml");
    }

    [HttpPost("{tenantId:guid}/{bankAccountId:guid}/snelstart/import")]
    public async Task<IActionResult> ImportIntoSnelStart(
        Guid tenantId,
        Guid bankAccountId,
        [FromQuery] string iban,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] SnelStartExportFormat format,
        CancellationToken ct)
    {
        var result = await _snelStartImportService.ImportAsync(
            tenantId,
            bankAccountId,
            iban,
            from,
            to,
            format,
            ct);

        return Ok(result);
    }

    [HttpPost("{tenantId:guid}/{bankAccountId:guid}/snelstart/auto-import")]
    public async Task<IActionResult> AutoImportIntoSnelStart(
        Guid tenantId,
        Guid bankAccountId,
        [FromQuery] string iban,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct)
    {
        var result = await _snelStartAutoImportService.ImportAsync(
            tenantId,
            bankAccountId,
            iban,
            from,
            to,
            ct);

        return Ok(result);
    }
}