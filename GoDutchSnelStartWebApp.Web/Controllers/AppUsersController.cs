using GoDutchSnelStartWebApp.Application.AppUsers.Dtos;
using GoDutchSnelStartWebApp.Application.AppUsers.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GoDutchSnelStartWebApp.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AppUsersController : ControllerBase
{
    private readonly IAppUserService _appUserService;

    public AppUsersController(IAppUserService appUserService)
    {
        _appUserService = appUserService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppUserDto>>> GetAll(CancellationToken cancellationToken)
    {
        var users = await _appUserService.GetAllAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AppUserDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var user = await _appUserService.GetByIdAsync(id, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateAppUserRequest request, CancellationToken cancellationToken)
    {
        var id = await _appUserService.CreateAsync(request.Username, request.Password, request.Module, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, null);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, [FromBody] UpdateAppUserRequest request, CancellationToken cancellationToken)
    {
        await _appUserService.UpdateAsync(id, request.Username, request.Module, request.IsActive, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/password")]
    public async Task<ActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await _appUserService.ChangePasswordAsync(id, request.NewPassword, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _appUserService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("login")]
    public async Task<ActionResult<AppUserDto>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _appUserService.ValidateCredentialsAsync(request.Username, request.Password, cancellationToken);
        return user is null ? Unauthorized() : Ok(user);
    }
}
