
using Telemedecine.Api.DTOs;

namespace Telemedecine.Api.Services.Interfaces;

public interface IPatientService
{
    Task<PatientProfileResponse?> GetProfileAsync(string userId);

    Task<PatientProfileResponse?> UpdateProfileAsync(
        string userId,
        UpdatePatientProfileRequest request);
}
