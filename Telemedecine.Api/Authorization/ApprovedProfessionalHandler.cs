using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Telemedecine.Api.Models;

namespace Telemedecine.Api.Authorization;

public sealed class ApprovedProfessionalRequirement
    : IAuthorizationRequirement
{
}

public sealed class ApprovedProfessionalHandler
    : AuthorizationHandler<ApprovedProfessionalRequirement>
{
    private readonly UserManager<AppUser> _userManager;

    public ApprovedProfessionalHandler(
        UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ApprovedProfessionalRequirement requirement)
    {
        var userId =
            context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(userId))
            return;

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null || !user.IsActive)
            return;

        var roles = await _userManager.GetRolesAsync(user);

        var isProfessional =
            roles.Contains("MEDECIN") ||
            roles.Contains("PHARMACIEN");

        if (isProfessional && user.IsProfessionalApproved)
        {
            context.Succeed(requirement);
        }
    }
}