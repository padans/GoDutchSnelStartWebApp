using GoDutchSnelStartWebApp.Application.ConnectivityTests.Dtos;
using GoDutchSnelStartWebApp.Application.ConnectivityTests.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GoDutchSnelStartWebApp.Web.Controllers;

[ApiController]
[Route("api/tenants/{tenantId:guid}/bankaccounts/{bankAccountId:guid}/tests")]
public sealed class ConnectionTestsController : ControllerBase
{
    private readonly IConnectionTestService _connectionTestService;

    public ConnectionTestsController(IConnectionTestService connectionTestService)
    {
        _connectionTestService = connectionTestService;
    }

    [HttpPost("snelstart")]
    public async Task<ActionResult<ConnectionTestResultDto>> TestSnelStart(
        Guid tenantId,
        Guid bankAccountId,
        CancellationToken cancellationToken)
    {
        var result = await _connectionTestService.TestSnelStartAsync(tenantId, bankAccountId, cancellationToken);
        return Ok(result);
    }
}
