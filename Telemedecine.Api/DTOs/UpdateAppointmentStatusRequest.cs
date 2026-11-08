using System.ComponentModel.DataAnnotations;

namespace Telemedecine.Api.DTOs;

public record UpdateAppointmentStatusRequest(
    [Required]
    string Status
);