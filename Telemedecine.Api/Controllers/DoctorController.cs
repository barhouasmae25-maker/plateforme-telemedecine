using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Telemedecine.Api.DTOs;
using Telemedecine.Api.Services.Interfaces;

namespace Telemedecine.Api.Controllers;

[ApiController]
[Route("api/doctor")]
[Authorize(Roles = "MEDECIN")]
public class DoctorController : ControllerBase
{
    private readonly IDoctorService _doctorService;

    public DoctorController(IDoctorService doctorService)
    {
        _doctorService = doctorService;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var profile = await _doctorService.GetProfileAsync(userId);

        if (profile is null)
            return NotFound(new
            {
                message = "Profil médecin introuvable."
            });

        return Ok(profile);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateDoctorProfileRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var profile = await _doctorService.UpdateProfileAsync(
            userId,
            request
        );

        if (profile is null)
            return NotFound(new
            {
                message = "Profil médecin introuvable."
            });

        return Ok(new
        {
            message = "Profil médecin modifié avec succès.",
            profile
        });
    }
}