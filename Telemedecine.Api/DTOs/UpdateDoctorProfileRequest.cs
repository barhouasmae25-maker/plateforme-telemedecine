using System.ComponentModel.DataAnnotations;

namespace Telemedecine.Api.DTOs;

public record UpdateDoctorProfileRequest(
    [Required, MaxLength(100)]
    string Specialty,

    [MaxLength(250)]
    string? ClinicAddress,

    [MaxLength(1000)]
    string? Description,

    [Range(typeof(decimal), "0", "999999")]
    decimal ConsultationFee
);