using GoDutchSnelStartWebApp.Application.SnelStartAdministrations.Dtos;
using GoDutchSnelStartWebApp.Application.SnelStartAdministrations.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GoDutchSnelStartWebApp.Web.Controllers;

[ApiController]
[Route("api/bankaccount-snelstart-links")]
public class BankAccountSnelStartLinksController : ControllerBase
{
    private readonly IBankAccountSnelStartLinkService _service;

    public BankAccountSnelStartLinksController(
        IBankAccountSnelStartLinkService service)
    {
        _service = service;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpGet("by-bankaccount/{bankAccountId:guid}")]
    public async Task<IActionResult> GetByBankAccountId(
        Guid bankAccountId,
        CancellationToken ct)
    {
        var result = await _service.GetByBankAccountIdAsync(bankAccountId, ct);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateBankAccountSnelStartLinkRequest request,
        CancellationToken ct)
    {
        var id = await _service.CreateAsync(request, ct);
        return Ok(new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateBankAccountSnelStartLinkRequest request,
        CancellationToken ct)
    {
        if (id != request.Id)
        {
            return BadRequest("Route id and body id must match.");
        }

        await _service.UpdateAsync(request, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}