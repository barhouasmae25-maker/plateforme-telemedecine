using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Telemedecine.Api.Data;
using Telemedecine.Api.DTOs;
using Telemedecine.Api.Models;
using Telemedecine.Api.Services.Interfaces;

namespace Telemedecine.Api.Services;

public class AdminDoctorService : IAdminDoctorService
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public AdminDoctorService(
        AppDbContext db,
        UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // Afficher les demandes des médecins en attente
    public async Task<List<PendingDoctorResponse>>
        GetPendingDoctorsAsync()
    {
        var doctors = await _db.Doctors
            .Include(d => d.User)
            .Where(d => !d.User.IsProfessionalApproved)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        var result = new List<PendingDoctorResponse>();

        foreach (var doctor in doctors)
        {
            if (!await _userManager.IsInRoleAsync(
                    doctor.User, "MEDECIN"))
            {
                continue;
            }

            result.Add(new PendingDoctorResponse(
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
            ));
        }

        return result;
    }

    // Approuver la demande d'un médecin
    public async Task<(bool Succeeded, int StatusCode, string Message)>
        ApproveDoctorAsync(int doctorId)
    {
        var doctor = await _db.Doctors
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == doctorId);

        if (doctor is null)
        {
            return (
                false,
                404,
                "Demande médecin introuvable."
            );
        }

        var isDoctor = await _userManager.IsInRoleAsync(
            doctor.User, "MEDECIN");

        if (!isDoctor)
        {
            return (
                false,
                400,
                "Cet utilisateur n'a pas le rôle MEDECIN."
            );
        }

        if (doctor.User.IsProfessionalApproved)
        {
            return (
                false,
                409,
                "Ce compte médecin est déjà approuvé."
            );
        }

        doctor.User.IsProfessionalApproved = true;

        await _db.SaveChangesAsync();

        return (
            true,
            200,
            "Le compte médecin a été approuvé avec succès."
        );
    }
}