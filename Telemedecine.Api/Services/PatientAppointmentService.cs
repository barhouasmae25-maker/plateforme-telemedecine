using Microsoft.EntityFrameworkCore;
using Telemedecine.Api.Data;
using Telemedecine.Api.DTOs;
using Telemedecine.Api.Models;
using Telemedecine.Api.Services.Interfaces;

namespace Telemedecine.Api.Services;

public class PatientAppointmentService : IPatientAppointmentService
{
    private readonly AppDbContext _db;

    public PatientAppointmentService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(bool Succeeded, int StatusCode, string Message,
        PatientAppointmentResponse? Data)> CreateAsync(
        string userId,
        CreateAppointmentRequest request)
    {
        // 1. Vérifier le profil Patient
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient is null)
        {
            return (false, 404,
                "Profil patient introuvable.", null);
        }

        // 2. Vérifier les dates
        var startAt = request.StartAt;
        var endAt = request.EndAt;

        if (startAt.Kind != DateTimeKind.Utc ||
            endAt.Kind != DateTimeKind.Utc)
        {
            return (false, 400,
                "Les dates doivent être envoyées en UTC.", null);
        }

        if (startAt <= DateTime.UtcNow)
        {
            return (false, 400,
                "La date du rendez-vous doit être dans le futur.", null);
        }

        if (endAt <= startAt)
        {
            return (false, 400,
                "L'heure de fin doit être après l'heure de début.", null);
        }

        // 3. Vérifier le médecin
        var doctor = await _db.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == request.DoctorId);

        if (doctor is null ||
            !doctor.User.IsProfessionalApproved ||
            !doctor.User.IsActive)
        {
            return (false, 404,
                "Médecin introuvable ou non approuvé.", null);
        }

        // 4. Vérifier la disponibilité du médecin
        var doctorConflict = await _db.Appointments.AnyAsync(a =>
            a.DoctorId == doctor.Id &&
            (a.Status == AppointmentStatus.Pending ||
             a.Status == AppointmentStatus.Confirmed) &&
            startAt < a.EndAt &&
            endAt > a.StartAt
        );

        if (doctorConflict)
        {
            return (false, 409,
                "Le médecin a déjà un rendez-vous pendant cette période.",
                null);
        }

        // 5. Vérifier la disponibilité du patient
        var patientConflict = await _db.Appointments.AnyAsync(a =>
            a.PatientId == patient.Id &&
            (a.Status == AppointmentStatus.Pending ||
             a.Status == AppointmentStatus.Confirmed) &&
            startAt < a.EndAt &&
            endAt > a.StartAt
        );

        if (patientConflict)
        {
            return (false, 409,
                "Vous avez déjà un rendez-vous pendant cette période.",
                null);
        }

        // 6. Créer le rendez-vous
        var appointment = new Appointment
        {
            PatientId = patient.Id,
            DoctorId = doctor.Id,
            StartAt = startAt,
            EndAt = endAt,
            Reason = request.Reason?.Trim(),
            Status = AppointmentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.Appointments.Add(appointment);

        await _db.SaveChangesAsync();

        // 7. Retourner le rendez-vous créé
        var response = new PatientAppointmentResponse(
            appointment.Id,
            doctor.Id,
            doctor.User.FirstName,
            doctor.User.LastName,
            doctor.Specialty,
            appointment.StartAt,
            appointment.EndAt,
            appointment.Reason,
            appointment.Status.ToString(),
            appointment.CreatedAt
        );

        return (true, 201,
            "Votre demande de rendez-vous a été envoyée.",
            response);
    }

    public async Task<List<PatientAppointmentResponse>>
        GetMyAppointmentsAsync(string userId)
    {
        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient is null)
            return new List<PatientAppointmentResponse>();

        var appointments = await _db.Appointments
            .Include(a => a.Doctor)
            .ThenInclude(d => d.User)
            .Where(a => a.PatientId == patient.Id)
            .OrderByDescending(a => a.StartAt)
            .ToListAsync();

        return appointments.Select(a =>
            new PatientAppointmentResponse(
                a.Id,
                a.DoctorId,
                a.Doctor.User.FirstName,
                a.Doctor.User.LastName,
                a.Doctor.Specialty,
                a.StartAt,
                a.EndAt,
                a.Reason,
                a.Status.ToString(),
                a.CreatedAt
            )
        ).ToList();
    }


    public async Task<(bool Succeeded, int StatusCode, string Message)>
    CancelAsync(string userId, int appointmentId, string? reason)
{
    var patient = await _db.Patients
        .FirstOrDefaultAsync(p => p.UserId == userId);

    if (patient == null)
        return (false, 404, "Profil patient introuvable.");

    var appointment = await _db.Appointments
        .FirstOrDefaultAsync(a =>
            a.Id == appointmentId &&
            a.PatientId == patient.Id);

    if (appointment == null)
        return (false, 404, "Rendez-vous introuvable.");

    if (appointment.Status != AppointmentStatus.Pending &&
        appointment.Status != AppointmentStatus.Confirmed)
    {
        return (false, 400,
            "Ce rendez-vous ne peut plus être annulé.");
    }

    appointment.Status = AppointmentStatus.Cancelled;

    // Si tu n'as pas encore de propriété CancellationReason
    // dans Appointment, ne sauvegarde pas reason pour le moment.

    await _db.SaveChangesAsync();

    return (true, 200, "Rendez-vous annulé avec succès.");
}
}