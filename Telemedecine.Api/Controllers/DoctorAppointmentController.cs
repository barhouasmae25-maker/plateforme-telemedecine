using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Telemedecine.Api.DTOs;
using Telemedecine.Api.Models;
using Telemedecine.Api.Services.Interfaces;

namespace Telemedecine.Api.Controllers;

[ApiController]
[Route("api/doctor/appointments")]
[Authorize(Roles = "MEDECIN")]
public class DoctorAppointmentController : ControllerBase
{
    private readonly IDoctorService _doctorService;

    public DoctorAppointmentController(IDoctorService doctorService)
    {
        _doctorService = doctorService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAppointments()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var appointments =
            await _doctorService.GetAppointmentsAsync(userId);

        return Ok(appointments);
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateAppointmentStatusRequest request)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier
        );

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (!Enum.TryParse<AppointmentStatus>(
                request.Status,
                true,
                out var status))
        {
            return BadRequest(new
            {
                message = "Statut invalide."
            });
        }

        var result =
            await _doctorService.UpdateAppointmentStatusAsync(
                userId,
                id,
                status
            );

        if (!result.Succeeded)
        {
            return StatusCode(result.StatusCode, new
            {
                message = result.Message
            });
        }

        return Ok(new
        {
            message = result.Message
        });
    }
}