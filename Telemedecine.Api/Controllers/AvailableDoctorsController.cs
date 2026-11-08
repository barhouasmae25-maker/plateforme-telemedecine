using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Telemedecine.Api.Services.Interfaces;

namespace Telemedecine.Api.Controllers;

[ApiController]
[Route("api/doctors")]
[Authorize(Roles = "PATIENT")]
public class AvailableDoctorsController : ControllerBase
{
    private readonly IDoctorService _doctorService;

    public AvailableDoctorsController(IDoctorService doctorService)
    {
        _doctorService = doctorService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailableDoctors()
    {
        var doctors = await _doctorService.GetAvailableDoctorsAsync();

        return Ok(doctors);
    }
}