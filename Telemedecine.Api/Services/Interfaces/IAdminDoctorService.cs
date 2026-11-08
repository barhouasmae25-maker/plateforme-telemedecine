using Telemedecine.Api.DTOs;

namespace Telemedecine.Api.Services.Interfaces;

public interface IAdminDoctorService
{
    Task<List<PendingDoctorResponse>> GetPendingDoctorsAsync();

    Task<(bool Succeeded, int StatusCode, string Message)>
        ApproveDoctorAsync(int doctorId);
}