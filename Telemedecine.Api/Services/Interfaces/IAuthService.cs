
using Telemedecine.Api.DTOs;

namespace Telemedecine.Api.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request);

    Task<AuthResult> LoginAsync(LoginRequest request);

    Task<AuthResult> GetCurrentUserAsync(string userId);
}

public record AuthResult(
    bool Succeeded,
    int StatusCode,
    string Message,
    object? Data = null);
