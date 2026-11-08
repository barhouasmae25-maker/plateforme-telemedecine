
using System.ComponentModel.DataAnnotations;

namespace Telemedecine.Api.DTOs;

public record CancelAppointmentRequest(
    [MaxLength(500)] string? Reason
);
