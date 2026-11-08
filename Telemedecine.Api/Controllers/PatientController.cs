
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Telemedecine.Api.DTOs;
using Telemedecine.Api.Services.Interfaces;

namespace Telemedecine.Api.Controllers;

[ApiController]
[Route("api/patient")]
[Authorize(Roles = "PATIENT")]
public class PatientController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var profile =
            await _patientService.GetProfileAsync(userId);

        if (profile == null)
            return NotFound(new
            {
                message = "Compte utilisateur introuvable."
            });

        return Ok(profile);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdatePatientProfileRequest request)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        var profile =
            await _patientService.UpdateProfileAsync(
                userId, request);

        if (profile == null)
            return NotFound(new
            {
                message = "Compte utilisateur introuvable."
            });

        return Ok(new
        {
            message = "Profil mis à jour avec succès.",
            profile
        });
    }
}
