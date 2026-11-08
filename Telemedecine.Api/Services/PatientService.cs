
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Telemedecine.Api.Data;
using Telemedecine.Api.DTOs;
using Telemedecine.Api.Models;
using Telemedecine.Api.Services.Interfaces;

namespace Telemedecine.Api.Services;

public class PatientService : IPatientService
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public PatientService(
        AppDbContext db,
        UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<PatientProfileResponse?> GetProfileAsync(
        string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return null;

        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        // إنشاء ملف المريض إذا لم يكن موجوداً بعد.
        if (patient == null)
        {
            patient = new Patient
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber
            };

            _db.Patients.Add(patient);
            await _db.SaveChangesAsync();
        }

        return MapToResponse(patient);
    }

    public async Task<PatientProfileResponse?> UpdateProfileAsync(
        string userId,
        UpdatePatientProfileRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return null;

        var patient = await _db.Patients
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (patient == null)
        {
            patient = new Patient
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty
            };

            _db.Patients.Add(patient);
        }

        patient.FirstName = request.FirstName.Trim();
        patient.LastName = request.LastName.Trim();
        patient.PhoneNumber = request.PhoneNumber?.Trim();
        patient.DateOfBirth = request.DateOfBirth;

        // مزامنة الاسم والهاتف مع حساب Identity.
        user.FirstName = patient.FirstName;
        user.LastName = patient.LastName;
        user.PhoneNumber = patient.PhoneNumber;

        var identityResult = await _userManager.UpdateAsync(user);

        if (!identityResult.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join(
                    "; ",
                    identityResult.Errors.Select(e => e.Description)));
        }

        patient.Email = user.Email ?? string.Empty;

        await _db.SaveChangesAsync();

        return MapToResponse(patient);
    }

    private static PatientProfileResponse MapToResponse(
        Patient patient)
    {
        return new PatientProfileResponse
        {
            Id = patient.Id,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            Email = patient.Email,
            PhoneNumber = patient.PhoneNumber,
            DateOfBirth = patient.DateOfBirth
        };
    }
}
