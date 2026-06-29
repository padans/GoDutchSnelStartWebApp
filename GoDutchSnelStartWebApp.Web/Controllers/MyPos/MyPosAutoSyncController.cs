using GoDutchSnelStartWebApp.Application.Abstractions.Repositories.MyPos;
using GoDutchSnelStartWebApp.Application.Configuration;
using GoDutchSnelStartWebApp.Application.MyPos.Dtos;
using GoDutchSnelStartWebApp.Application.MyPos.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GoDutchSnelStartWebApp.Web.Controllers.MyPos;

[ApiController]
[Route("api/mypos/auto-sync")]
public sealed class MyPosAutoSyncController : ControllerBase
{
    private readonly IMyPosAutoSyncService _autoSyncService;
    private readonly IMyPosAutoSyncSettingsRepository _settingsRepository;
    private readonly IOptions<MyPosAutoSyncOptions> _options;
    private readonly ILogger<MyPosAutoSyncController> _logger;

    public MyPosAutoSyncController(
        IMyPosAutoSyncService autoSyncService,
        IMyPosAutoSyncSettingsRepository settingsRepository,
        IOptions<MyPosAutoSyncOptions> options,
        ILogger<MyPosAutoSyncController> logger)
    {
        _autoSyncService = autoSyncService;
        _settingsRepository = settingsRepository;
        _options = options;
        _logger = logger;
    }

    [HttpGet("settings")]
    [ProducesResponseType(typeof(MyPosAutoSyncSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MyPosAutoSyncSettingsDto>> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var dbSettings = await _settingsRepository.GetAsync(cancellationToken);

        if (dbSettings is not null)
        {
            return Ok(dbSettings);
        }

        var opts = _options.Value;

        return Ok(new MyPosAutoSyncSettingsDto
        {
            Enabled = opts.Enabled,
            IntervalMinutes = opts.IntervalMinutes < 1 ? 1 : opts.IntervalMinutes,
            LookbackHours = opts.LookbackHours
        });
    }

    [HttpPut("settings")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSettingsAsync(
        [FromBody] MyPosAutoSyncSettingsDto request,
        CancellationToken cancellationToken)
    {
        if (request.IntervalMinutes < 1)
        {
            return BadRequest("IntervalMinutes moet minimaal 1 zijn.");
        }

        if (request.LookbackHours < 1)
        {
            return BadRequest("LookbackHours moet minimaal 1 zijn.");
        }

        await _settingsRepository.UpsertAsync(request, cancellationToken);

        _logger.LogInformation(
            "myPOS auto-sync instellingen bijgewerkt. Enabled: {Enabled}, Interval: {Interval} min, Lookback: {Lookback} uur.",
            request.Enabled,
            request.IntervalMinutes,
            request.LookbackHours);

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
