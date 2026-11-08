namespace Telemedecine.Api.DTOs;

public record DoctorRegistrationResponse(
    int DoctorId,
    string UserId,
    string FirstName,
    string LastName,
    string Email,
    string Specialty,
    string LicenseNumber,
    bool IsProfessionalApproved,
    string Message
);