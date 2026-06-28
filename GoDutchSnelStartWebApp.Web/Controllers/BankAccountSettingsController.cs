using GoDutchSnelStartWebApp.Application.BankAccountSettings.Dtos;
using GoDutchSnelStartWebApp.Application.BankAccountSettings.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GoDutchSnelStartWebApp.Web.Controllers;

[ApiController]
[Route("api/tenants/{tenantId:guid}/bankaccounts/{bankAccountId:guid}/settings")]
public sealed class BankAccountSettingsController : ControllerBase
{
    private readonly IBankAccountSettingsService _settingsService;

    public BankAccountSettingsController(IBankAccountSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<ActionResult<BankAccountSettingsDto>> GetByBankAccountId(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetByBankAccountIdAsync(tenantId, bankAccountId, cancellationToken);

        if (settings is null)
        {
            return NotFound();
        }

        return Ok(settings);
    }

    [HttpPost]
    public async Task<ActionResult> Create(
        Guid tenantId,
        Guid bankAccountId,
        [FromBody] CreateBankAccountSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _settingsService.CreateAsync(tenantId, bankAccountId, request, cancellationToken);
        return CreatedAtAction(nameof(GetByBankAccountId), new { tenantId, bankAccountId }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(
        Guid tenantId,
        Guid bankAccountId,
        Guid id,
        [FromBody] UpdateBankAccountSettingsRequest request,
        CancellationToken cancellationToken)
    {
        await _settingsService.UpdateAsync(tenantId, bankAccountId, id, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(
        Guid tenantId,
        Guid bankAccountId,
        Guid id,
        CancellationToken cancellationToken)
    {
        await _settingsService.DeleteAsync(tenantId, bankAccountId, id, cancellationToken);
        return NoContent();
    }

    [HttpPost("test-snelstart")]
    public async Task<ActionResult> TestSnelStart(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken)
    {
        var result = await _settingsService.TestSnelStartAsync(
            tenantId,
            bankAccountId,
            cancellationToken);

        return Ok(result);
    }
}
