
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Telemedecine.Api.Models;

public class Doctor
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public AppUser User { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string Specialty { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LicenseNumber { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? ClinicAddress { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public decimal ConsultationFee { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
