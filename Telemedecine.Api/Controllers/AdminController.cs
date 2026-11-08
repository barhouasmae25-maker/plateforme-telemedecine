
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Telemedecine.Api.Models;

namespace Telemedecine.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "ADMIN")]
public class AdminController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;

    public AdminController(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    // GET: api/admin/users
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderByDescending(user => user.CreatedAt)
            .ToListAsync();

        var result = new List<object>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            result.Add(new
            {
                id = user.Id,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                userName = user.UserName,
                phoneNumber = user.PhoneNumber,
                role = roles.FirstOrDefault() ?? string.Empty,
                roles,
                isActive = user.IsActive,
                isProfessionalApproved = user.IsProfessionalApproved,
                createdAt = user.CreatedAt
            });
        }

        return Ok(result);
    }

    // PUT: api/admin/users/{id}/status
    [HttpPut("users/{id}/status")]
    public async Task<IActionResult> UpdateUserStatus(
        string id,
        [FromBody] UpdateUserStatusRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user is null)
            return NotFound(new { message = "Utilisateur introuvable." });

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (user.Id == currentUserId && !request.IsActive)
        {
            return BadRequest(new
            {
                message = "Vous ne pouvez pas désactiver votre propre compte."
            });
        }

        user.IsActive = request.IsActive;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Impossible de modifier le statut du compte.",
                errors = result.Errors.Select(error => error.Description)
            });
        }

        return Ok(new
        {
            message = request.IsActive
                ? "Compte activé avec succès."
                : "Compte désactivé avec succès.",
            id = user.Id,
            isActive = user.IsActive
        });
    }

    // PUT: api/admin/users/{id}/approve-professional
    [HttpPut("users/{id}/approve-professional")]
    public async Task<IActionResult> ApproveProfessional(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user is null)
            return NotFound(new { message = "Utilisateur introuvable." });

        var roles = await _userManager.GetRolesAsync(user);

        var isProfessional =
            roles.Contains("MEDECIN") ||
            roles.Contains("PHARMACIEN");

        if (!isProfessional)
        {
            return BadRequest(new
            {
                message = "Ce compte n'est pas un compte professionnel."
            });
        }

        if (!user.IsActive)
        {
            return BadRequest(new
            {
                message = "Impossible d'approuver un compte désactivé."
            });
        }

        user.IsProfessionalApproved = true;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                message = "Impossible d'approuver ce compte.",
                errors = result.Errors.Select(error => error.Description)
            });
        }

        return Ok(new
        {
            message = "Compte professionnel approuvé avec succès.",
            id = user.Id,
            isProfessionalApproved = user.IsProfessionalApproved
        });
    }
}

public class UpdateUserStatusRequest
{
    public bool IsActive { get; set; }
}
