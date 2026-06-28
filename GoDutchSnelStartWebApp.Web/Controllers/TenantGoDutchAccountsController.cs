using GoDutchSnelStartWebApp.Application.GoDutchAccounts.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GoDutchSnelStartWebApp.Web.Controllers;

[ApiController]
[Route("api/tenants/{tenantId:guid}/godutch/accounts")]
public class TenantGoDutchAccountsController : ControllerBase
{
    private readonly IGoDutchAccountLookupService _accountLookupService;

    public TenantGoDutchAccountsController(IGoDutchAccountLookupService accountLookupService)
    {
        _accountLookupService = accountLookupService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAccounts(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var accounts = await _accountLookupService.GetAccountsAsync(
            tenantId,
            cancellationToken);

        return Ok(accounts);
    }
}
