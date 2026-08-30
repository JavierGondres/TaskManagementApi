using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagementApi.DTOs.Auth;
using TaskManagementApi.Interfaces;

namespace TaskManagementApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequestDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        if (result is null)
        {
            return Conflict(new { message = "Email already registered" });
        }

        return Created("/api/users/me", result);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        if (result is null)
        {
            return Unauthorized(new { message = "Invalid email or password" });
        }

        return Ok(result);
    }
}
