using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Telemedecine.Api.DTOs;
using Telemedecine.Api.Services.Interfaces;

namespace Telemedecine.Api.Controllers;

[ApiController]
[Route("api/doctors")]
public class DoctorRegistrationController : ControllerBase
{
    private readonly IDoctorRegistrationService _doctorService;

    public DoctorRegistrationController(
        IDoctorRegistrationService doctorService)
    {
        _doctorService = doctorService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] DoctorRegisterRequest request)
    {
        var result = await _doctorService.RegisterAsync(request);

        if (!result.Succeeded)
        {
            return StatusCode(result.StatusCode, new
            {
                message = result.Message,
                errors = result.Data
            });
        }

        return StatusCode(result.StatusCode, new
        {
            message = result.Message,
            doctor = result.Data
        });
    }
}