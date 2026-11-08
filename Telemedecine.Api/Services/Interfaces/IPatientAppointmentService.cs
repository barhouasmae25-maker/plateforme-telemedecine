using Telemedecine.Api.DTOs;

namespace Telemedecine.Api.Services.Interfaces;

public interface IPatientAppointmentService
{
    Task<(bool Succeeded, int StatusCode, string Message,
        PatientAppointmentResponse? Data)> CreateAsync(
        string userId,
        CreateAppointmentRequest request
    );


    Task<(bool Succeeded, int StatusCode, string Message)>
    CancelAsync(string userId, int appointmentId, string? reason);

    Task<List<PatientAppointmentResponse>> GetMyAppointmentsAsync(
        string userId
    );


    
}