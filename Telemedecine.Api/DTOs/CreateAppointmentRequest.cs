using System.ComponentModel.DataAnnotations;

namespace Telemedecine.Api.DTOs;

public record CreateAppointmentRequest(
    [Required]
    int DoctorId,

    [Required]
    DateTime StartAt,

    [Required]
    DateTime EndAt,

    [MaxLength(1000)]
    string? Reason
);