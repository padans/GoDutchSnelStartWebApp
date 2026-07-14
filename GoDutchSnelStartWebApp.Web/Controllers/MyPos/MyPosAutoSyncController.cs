using GoDutchSnelStartWebApp.Application.MyPos.Dtos;
using GoDutchSnelStartWebApp.Application.MyPos.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GoDutchSnelStartWebApp.Web.Controllers.MyPos;

[ApiController]
[Route("api/mypos/auto-sync")]
public sealed class MyPosAutoSyncController : ControllerBase
{
    private readonly IMyPosAutoSyncService _autoSyncService;
    private readonly ILogger<MyPosAutoSyncController> _logger;

    public MyPosAutoSyncController(
        IMyPosAutoSyncService autoSyncService,
        ILogger<MyPosAutoSyncController> logger)
    {
        _autoSyncService = autoSyncService;
        _logger = logger;
    }

    [HttpGet("settings")]
    [ProducesResponseType(typeof(MyPosAutoSyncSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MyPosAutoSyncSettingsDto>> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await _autoSyncService.GetSettingsAsync(cancellationToken);
        return Ok(settings);
    }

    [HttpPut("settings")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSettingsAsync(
        [FromBody] MyPosAutoSyncSettingsDto request,
        CancellationToken cancellationToken)
    {
        if (request.IntervalMinutes < 1)
            return BadRequest("IntervalMinutes moet minimaal 1 zijn.");

        await _autoSyncService.UpdateSettingsAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("run")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("myPOS auto-sync handmatig gestart via API.");

        await _autoSyncService.RunOnceAsync(cancellationToken);

        _logger.LogInformation("myPOS auto-sync handmatig afgerond via API.");

        return Ok();
    }
}
