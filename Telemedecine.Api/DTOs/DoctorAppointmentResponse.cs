namespace Telemedecine.Api.DTOs;

public record DoctorAppointmentResponse(
    int AppointmentId,
    int PatientId,
    string PatientFirstName,
    string PatientLastName,
    string PatientEmail,
    string? PatientPhone,
    DateTime StartAt,
    DateTime EndAt,
    string? Reason,
    string Status,
    DateTime CreatedAt
);