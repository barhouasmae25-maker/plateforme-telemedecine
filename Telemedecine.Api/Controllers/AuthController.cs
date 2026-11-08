
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Telemedecine.Api.DTOs;
using Telemedecine.Api.Services.Interfaces;

namespace Telemedecine.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);

        if (!result.Succeeded)
        {
            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                errors = result.Data
            });
        }

        return StatusCode(result.StatusCode, new
        {
            message = result.Message,
            user = result.Data
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        if (!result.Succeeded)
        {
            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                errors = result.Data
            });
        }

        return StatusCode(result.StatusCode, result.Data);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var result = await _authService
            .GetCurrentUserAsync(userId);

        if (!result.Succeeded)
        {
            return StatusCode(result.StatusCode, new
            {
                message = result.Message
            });
        }

        return Ok(result.Data);
    }
}
