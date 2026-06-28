using GoDutchSnelStartWebApp.Application.BankAccounts.Dtos;
using GoDutchSnelStartWebApp.Application.BankAccounts.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GoDutchSnelStartWebApp.Web.Controllers;

[ApiController]
[Route("api/tenants/{tenantId:guid}/bankaccounts")]
public sealed class BankAccountsController : ControllerBase
{
    private readonly IBankAccountService _bankAccountService;
    private readonly IBankAccountResyncService _resyncService;

    public BankAccountsController(
        IBankAccountService bankAccountService,
        IBankAccountResyncService resyncService)
    {
        _bankAccountService = bankAccountService;
        _resyncService = resyncService;
    }
    [HttpGet("{bankAccountId:guid}")]
    public async Task<IActionResult> GetById(
    Guid tenantId,
    Guid bankAccountId,
    CancellationToken cancellationToken)
    {
        var bankAccount = await _bankAccountService.GetByIdAsync(
            tenantId,
            bankAccountId,
            cancellationToken);

        if (bankAccount is null)
        {
            return NotFound();
        }

        return Ok(bankAccount);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BankAccountDto>>> GetByTenantId(Guid tenantId, CancellationToken cancellationToken)
    {
        var bankAccounts = await _bankAccountService.GetByTenantIdAsync(tenantId, cancellationToken);
        return Ok(bankAccounts);
    }

    [HttpPost]
    public async Task<ActionResult> Create(Guid tenantId, [FromBody] CreateBankAccountRequest request, CancellationToken cancellationToken)
    {
        var id = await _bankAccountService.CreateAsync(tenantId, request, cancellationToken);
        return CreatedAtAction(nameof(GetByTenantId), new { tenantId }, new { id });
    }
    [HttpGet("{bankAccountId:guid}/sync-status")]
    public async Task<IActionResult> GetSyncStatus(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken)
    {
        var status = await _bankAccountService.GetSyncStatusAsync(tenantId, bankAccountId, cancellationToken);
        return Ok(status);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid tenantId, Guid id, [FromBody] UpdateBankAccountRequest request, CancellationToken cancellationToken)
    {
        await _bankAccountService.UpdateAsync(tenantId, id, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        await _bankAccountService.DeleteAsync(tenantId, id, cancellationToken);
        return NoContent();
    }
    [HttpPost("{bankAccountId:guid}/resync")]
    public async Task<IActionResult> ForceResync(
    Guid tenantId,
    Guid bankAccountId,
    [FromBody] ForceResyncRequest request,
    CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest("Request body is verplicht.");
        }

        var result = await _resyncService.ForceResyncFromDateAsync(
            tenantId,
            bankAccountId,
            request.FromUtc,
            cancellationToken);

        return Ok(result);
    }
}