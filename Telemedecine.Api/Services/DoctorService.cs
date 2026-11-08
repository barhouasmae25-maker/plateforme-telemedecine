using Microsoft.EntityFrameworkCore;
using Telemedecine.Api.Data;
using Telemedecine.Api.DTOs;
using Telemedecine.Api.Services.Interfaces;
using Telemedecine.Api.Models;

namespace Telemedecine.Api.Services;

public class DoctorService : IDoctorService
{
    private readonly AppDbContext _db;

    public DoctorService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DoctorProfileResponse?> GetProfileAsync(string userId)
    {
        var doctor = await _db.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor is null)
            return null;

        return MapToResponse(doctor);
    }

    public async Task<DoctorProfileResponse?> UpdateProfileAsync(
        string userId,
        UpdateDoctorProfileRequest request)
    {
        var doctor = await _db.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor is null)
            return null;

        doctor.Specialty = request.Specialty.Trim();
        doctor.ClinicAddress = request.ClinicAddress?.Trim();
        doctor.Description = request.Description?.Trim();
        doctor.ConsultationFee = request.ConsultationFee;

        await _db.SaveChangesAsync();

        return MapToResponse(doctor);
    }

    private static DoctorProfileResponse MapToResponse(
        Models.Doctor doctor)
    {
        return new DoctorProfileResponse(
            doctor.Id,
            doctor.UserId,
            doctor.User.FirstName,
            doctor.User.LastName,
            doctor.User.Email ?? string.Empty,
            doctor.User.PhoneNumber,
            doctor.Specialty,
            doctor.LicenseNumber,
            doctor.ClinicAddress,
            doctor.Description,
            doctor.ConsultationFee,
            doctor.CreatedAt
        );
    }

public async Task<List<AvailableDoctorResponse>> GetAvailableDoctorsAsync()
{
    var doctors = await _db.Doctors
        .Include(d => d.User)
        .Where(d =>
            d.User.IsActive &&
            d.User.IsProfessionalApproved)
        .OrderBy(d => d.User.FirstName)
        .Select(d => new AvailableDoctorResponse(
            d.Id,
            d.User.FirstName,
            d.User.LastName,
            d.Specialty,
            d.ClinicAddress,
            d.Description,
            d.ConsultationFee
        ))
        .ToListAsync();

    return doctors;
}








public async Task<List<DoctorAppointmentResponse>> GetAppointmentsAsync(
    string userId)
{
    var doctor = await _db.Doctors
        .FirstOrDefaultAsync(d => d.UserId == userId);

    if (doctor is null)
        return new List<DoctorAppointmentResponse>();

    var appointments = await _db.Appointments
        .Include(a => a.Patient)
        .ThenInclude(p => p.User)
        .Where(a => a.DoctorId == doctor.Id)
        .OrderBy(a => a.StartAt)
        .ToListAsync();

    return appointments.Select(a => new DoctorAppointmentResponse(
        a.Id,
        a.PatientId,
        a.Patient.FirstName,
        a.Patient.LastName,
        a.Patient.Email,
        a.Patient.PhoneNumber,
        a.StartAt,
        a.EndAt,
        a.Reason,
        a.Status.ToString(),
        a.CreatedAt
    )).ToList();
}

public async Task<(bool Succeeded, int StatusCode, string Message)>
    UpdateAppointmentStatusAsync(
        string userId,
        int appointmentId,
        AppointmentStatus status)
{
    var doctor = await _db.Doctors
        .FirstOrDefaultAsync(d => d.UserId == userId);

    if (doctor is null)
        return (false, 404, "Profil médecin introuvable.");

    var appointment = await _db.Appointments
        .FirstOrDefaultAsync(a =>
            a.Id == appointmentId &&
            a.DoctorId == doctor.Id);

    if (appointment is null)
        return (false, 404, "Rendez-vous introuvable.");

    if (appointment.Status != AppointmentStatus.Pending)
        return (false, 409,
            "Ce rendez-vous a déjà été traité.");

    if (status != AppointmentStatus.Confirmed &&
        status != AppointmentStatus.Rejected)
        return (false, 400,
            "Statut non autorisé.");

    appointment.Status = status;

    await _db.SaveChangesAsync();

    return (true, 200,
        "Statut du rendez-vous modifié avec succès.");
}









}