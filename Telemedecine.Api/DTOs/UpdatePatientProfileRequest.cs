
using System.ComponentModel.DataAnnotations;

namespace Telemedecine.Api.DTOs;

public class UpdatePatientProfileRequest
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    public DateOnly? DateOfBirth { get; set; }
}
