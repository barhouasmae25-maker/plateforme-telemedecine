
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Telemedecine.Api.DTOs;
using Telemedecine.Api.Services.Interfaces;

namespace Telemedecine.Api.Controllers;

[ApiController]
[Route("api/patient/appointments")]
[Authorize(Roles = "PATIENT")]
public class PatientAppointmentController : ControllerBase
{
    private readonly IPatientAppointmentService _service;

    public PatientAppointmentController(
        IPatientAppointmentService service)
    {
        _service = service;
    }

    // POST: api/patient/appointments
    // Créer une demande de rendez-vous
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateAppointmentRequest request)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _service.CreateAsync(
            userId, request);

        if (!result.Succeeded)
        {
            return StatusCode(result.StatusCode, new
            {
                message = result.Message
            });
        }

        return StatusCode(201, new
        {
            message = result.Message,
            appointment = result.Data
        });
    }

    // GET: api/patient/appointments
    // Consulter ses rendez-vous
    [HttpGet]
    public async Task<IActionResult> GetMyAppointments()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var appointments =
            await _service.GetMyAppointmentsAsync(userId);

        return Ok(appointments);
    }

    // PUT: api/patient/appointments/{id}/cancel
    // Annuler son propre rendez-vous
    [HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(
        int id,
        [FromBody] CancelAppointmentRequest request)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var result = await _service.CancelAsync(
            userId,
            id,
            request.Reason);

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
