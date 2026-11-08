
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Telemedecine.Api.Models;

namespace Telemedecine.Api.Data;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<AppUser>>();

        var configuration = scope.ServiceProvider
            .GetRequiredService<IConfiguration>();

        // 1. Créer les rôles s'ils n'existent pas
        string[] roles =
        [
            "PATIENT",
            "MEDECIN",
            "PHARMACIEN",
            "ADMIN"
        ];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(
                    new IdentityRole(role));

                if (!result.Succeeded)
                {
                    var errors = string.Join(
                        ", ",
                        result.Errors.Select(e => e.Description));

                    throw new InvalidOperationException(
                        $"Impossible de créer le rôle {role}: {errors}");
                }
            }
        }

        // 2. Récupérer les informations de l'administrateur
        var adminEmail = configuration["Admin:Email"];
        var adminPassword = configuration["Admin:Password"];
        var firstName = configuration["Admin:FirstName"] ?? "System";
        var lastName = configuration["Admin:LastName"] ?? "Administrator";

        // Ne rien faire si les informations ne sont pas configurées
        if (string.IsNullOrWhiteSpace(adminEmail) ||
            string.IsNullOrWhiteSpace(adminPassword))
        {
            return;
        }

        // 3. Chercher le compte administrateur
        var admin = await userManager.FindByEmailAsync(adminEmail);

        // 4. Créer un nouveau compte ADMIN s'il n'existe pas
        if (admin is null)
        {
            admin = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true,
                IsActive = true,
                IsProfessionalApproved = true
            };

            var createResult = await userManager.CreateAsync(
                admin,
                adminPassword);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    createResult.Errors.Select(e => e.Description));

                throw new InvalidOperationException(
                    $"Impossible de créer le compte ADMIN: {errors}");
            }
        }

        // 5. Attribuer le rôle ADMIN
        if (!await userManager.IsInRoleAsync(admin, "ADMIN"))
        {
            var roleResult = await userManager.AddToRoleAsync(
                admin,
                "ADMIN");

            if (!roleResult.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    roleResult.Errors.Select(e => e.Description));

                throw new InvalidOperationException(
                    $"Impossible d'attribuer le rôle ADMIN: {errors}");
            }
        }
    }
}
