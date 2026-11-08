namespace Telemedecine.Api.DTOs;

public record DoctorProfileResponse(
    int DoctorId,
    string UserId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string Specialty,
    string LicenseNumber,
    string? ClinicAddress,
    string? Description,
    decimal ConsultationFee,
    DateTime CreatedAt
);