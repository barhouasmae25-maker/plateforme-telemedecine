
using System.ComponentModel.DataAnnotations;

namespace Telemedecine.Api.Models;

public class Appointment
{
    public int Id { get; set; }

    [Required]
    public int PatientId { get; set; }

    public Patient Patient { get; set; } = null!;

    [Required]
    public int DoctorId { get; set; }

    public Doctor Doctor { get; set; } = null!;

    [Required]
    public DateTime StartAt { get; set; }

    [Required]
    public DateTime EndAt { get; set; }

    [MaxLength(1000)]
    public string? Reason { get; set; }

    public AppointmentStatus Status { get; set; }
        = AppointmentStatus.Pending;

    public DateTime CreatedAt { get; set; }
        = DateTime.UtcNow;
}
