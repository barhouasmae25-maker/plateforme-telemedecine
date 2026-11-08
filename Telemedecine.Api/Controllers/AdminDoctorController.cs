using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Telemedecine.Api.Services.Interfaces;

namespace Telemedecine.Api.Controllers;

[ApiController]
[Route("api/admin/doctors")]
[Authorize(Roles = "ADMIN")]
public class AdminDoctorController : ControllerBase
{
    private readonly IAdminDoctorService _adminDoctorService;

    public AdminDoctorController(
        IAdminDoctorService adminDoctorService)
    {
        _adminDoctorService = adminDoctorService;
    }

    // GET: api/admin/doctors/pending
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingDoctors()
    {
        var doctors = await _adminDoctorService
            .GetPendingDoctorsAsync();

        return Ok(doctors);
    }

    // PUT: api/admin/doctors/{id}/approve
    [HttpPut("{id:int}/approve")]
    public async Task<IActionResult> ApproveDoctor(int id)
    {
        var result = await _adminDoctorService
            .ApproveDoctorAsync(id);

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