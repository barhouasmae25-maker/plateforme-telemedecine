namespace Telemedecine.Api.DTOs;

public record AvailableDoctorResponse(
    int Id,
    string FirstName,
    string LastName,
    string Specialty,
    string? ClinicAddress,
    string? Description,
    decimal ConsultationFee
);