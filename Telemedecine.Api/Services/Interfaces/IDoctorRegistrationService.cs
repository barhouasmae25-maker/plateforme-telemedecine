using Telemedecine.Api.DTOs;

namespace Telemedecine.Api.Services.Interfaces;

public interface IDoctorRegistrationService
{
    Task<(bool Succeeded, int StatusCode, string Message, object? Data)>
        RegisterAsync(DoctorRegisterRequest request);
}