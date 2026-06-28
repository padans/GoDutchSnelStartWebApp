using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Dtos;
using GoDutchSnelStartWebApp.Application.GoDutchTransactions.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GoDutchSnelStartWebApp.Web.Controllers;

[ApiController]
[Route("api/godutch-webhooks")]
public class GoDutchWebhooksController : ControllerBase
{
    private readonly IGoDutchWebhookImportService _webhookImportService;

    public GoDutchWebhooksController(
        IGoDutchWebhookImportService webhookImportService)
    {
        _webhookImportService = webhookImportService;
    }

    [HttpPost("import")]
    public async Task<IActionResult> Import(
        [FromBody] GoDutchWebhookImportRequest request,
        CancellationToken ct)
    {
        var result = await _webhookImportService.ImportAsync(request, ct);
        return Ok(result);
    }
}