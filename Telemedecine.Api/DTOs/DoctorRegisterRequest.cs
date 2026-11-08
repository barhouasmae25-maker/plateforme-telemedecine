using System.ComponentModel.DataAnnotations;

namespace Telemedecine.Api.DTOs;

public record DoctorRegisterRequest(
    [Required, MaxLength(100)]
    string FirstName,

    [Required, MaxLength(100)]
    string LastName,

    [Required, EmailAddress]
    string Email,

    [Required, MinLength(8), MaxLength(128)]
    string Password,

    string? PhoneNumber,

    [Required, MaxLength(100)]
    string Specialty,

    [Required, MaxLength(100)]
    string LicenseNumber,

    [MaxLength(250)]
    string? ClinicAddress,

    [MaxLength(1000)]
    string? Description,

    [Range(0, 999999)]
    decimal ConsultationFee
);