
namespace Telemedecine.Api.DTOs;

public record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? PhoneNumber,
    string Role = "PATIENT"
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResponse(
    string Token,
    DateTime ExpiresAt,
    string Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string[] Roles
);

public record UserProfileResponse(
    string Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string[] Roles
);
