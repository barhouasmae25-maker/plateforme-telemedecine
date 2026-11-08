using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Telemedecine.Api.Data;
using Telemedecine.Api.DTOs;
using Telemedecine.Api.Models;
using Telemedecine.Api.Services.Interfaces;

namespace Telemedecine.Api.Services;

public class DoctorRegistrationService : IDoctorRegistrationService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _db;

    public DoctorRegistrationService(
        UserManager<AppUser> userManager,
        AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<(bool Succeeded, int StatusCode, string Message, object? Data)>
        RegisterAsync(DoctorRegisterRequest request)
    {
        var email = request.Email.Trim();
        var licenseNumber = request.LicenseNumber.Trim();

        // 1. Vérifier si l'email existe déjà
        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            return (false, 409, "Cette adresse email est déjà utilisée.", null);
        }

        // 2. Vérifier le numéro de licence
        var licenseExists = await _db.Doctors
            .AnyAsync(d => d.LicenseNumber == licenseNumber);

        if (licenseExists)
        {
            return (false, 409, "Ce numéro de licence est déjà utilisé.", null);
        }

        // 3. Vérifier le rôle MEDECIN
        var roleExists = await _db.Roles
            .AnyAsync(r => r.Name == "MEDECIN");

        if (!roleExists)
        {
            return (false, 500, "Le rôle MEDECIN n'existe pas.", null);
        }

        // 4. Créer le compte utilisateur
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            IsActive = true,
            IsProfessionalApproved = false,
            EmailConfirmed = false
        };

        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        var createResult = await _userManager.CreateAsync(
            user,
            request.Password);

        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync();

            var errors = createResult.Errors
                .Select(e => e.Description)
                .ToArray();

            return (
                false,
                400,
                "Impossible de créer le compte médecin.",
                errors
            );
        }

        // 5. Attribuer le rôle MEDECIN
        var roleResult = await _userManager.AddToRoleAsync(
            user,
            "MEDECIN");

        if (!roleResult.Succeeded)
        {
            await transaction.RollbackAsync();

            return (
                false,
                500,
                "Impossible d'attribuer le rôle MEDECIN.",
                roleResult.Errors.Select(e => e.Description).ToArray()
            );
        }

        // 6. Créer le profil professionnel
        var doctor = new Doctor
        {
            UserId = user.Id,
            Specialty = request.Specialty.Trim(),
            LicenseNumber = licenseNumber,
            ClinicAddress = request.ClinicAddress?.Trim(),
            Description = request.Description?.Trim(),
            ConsultationFee = request.ConsultationFee,
            CreatedAt = DateTime.UtcNow
        };

        _db.Doctors.Add(doctor);

        try
        {
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();

            return (
                false,
                409,
                "Impossible d'enregistrer le profil. " +
                "Vérifiez le numéro de licence.",
                null
            );
        }

        // 7. Retourner les informations sans mot de passe
        var response = new DoctorRegistrationResponse(
            doctor.Id,
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email!,
            doctor.Specialty,
            doctor.LicenseNumber,
            user.IsProfessionalApproved,
            "Votre demande d'inscription médecin a été envoyée. " +
            "Elle sera examinée par l'administration."
        );

        return (
            true,
            201,
            "Demande d'inscription créée avec succès.",
            response
        );
    }
}