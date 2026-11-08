namespace Telemedecine.Api.DTOs;

public record PatientAppointmentResponse(
    int AppointmentId,
    int DoctorId,
    string DoctorFirstName,
    string DoctorLastName,
    string Specialty,
    DateTime StartAt,
    DateTime EndAt,
    string? Reason,
    string Status,
    DateTime CreatedAt
);