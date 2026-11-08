using Telemedecine.Api.DTOs;
using Telemedecine.Api.Models;

namespace Telemedecine.Api.Services.Interfaces;

public interface IDoctorService
{
    Task<DoctorProfileResponse?> GetProfileAsync(string userId);

    Task<DoctorProfileResponse?> UpdateProfileAsync(
        string userId,
        UpdateDoctorProfileRequest request
    );

    Task<List<DoctorAppointmentResponse>> GetAppointmentsAsync(
        string userId
    );

    Task<List<AvailableDoctorResponse>> GetAvailableDoctorsAsync();

    Task<(bool Succeeded, int StatusCode, string Message)>
        UpdateAppointmentStatusAsync(
            string userId,
            int appointmentId,
            AppointmentStatus status
        );
}