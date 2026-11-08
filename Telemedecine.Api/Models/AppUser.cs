
using Microsoft.AspNetCore.Identity;

namespace Telemedecine.Api.Models;

public class AppUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool IsProfessionalApproved { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

