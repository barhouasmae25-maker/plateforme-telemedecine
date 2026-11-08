
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Telemedecine.Api.DTOs;
using Telemedecine.Api.Models;
using Telemedecine.Api.Services.Interfaces;

namespace Telemedecine.Api.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IConfiguration _configuration;

    private static readonly string[] AllowedRoles =
    [
        "PATIENT",
        "MEDECIN",
        "PHARMACIEN"
    ];

    public AuthService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    public async Task<AuthResult> RegisterAsync(
        RegisterRequest request)
    {
        var firstName = request.FirstName?.Trim();
        var lastName = request.LastName?.Trim();
        var email = request.Email?.Trim();
        var password = request.Password;
        var role = request.Role?.Trim().ToUpperInvariant();

        // 1. Vérification des champs obligatoires
        if (string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return new(
                false,
                400,
                "Tous les champs obligatoires doivent être remplis."
            );
        }

        // 2. Vérification du format email
        if (!System.Net.Mail.MailAddress.TryCreate(
                email, out var address) ||
            !string.Equals(
                address.Address,
                email,
                StringComparison.OrdinalIgnoreCase))
        {
            return new(false, 400, "L'adresse email est invalide.");
        }

        // 3. Vérification du mot de passe
        if (password.Length > 128)
        {
            return new(
                false,
                400,
                "Le mot de passe ne doit pas dépasser 128 caractères."
            );
        }

        // 4. Seuls ces rôles peuvent être demandés publiquement.
        // Le rôle ADMIN ne peut jamais être choisi à l'inscription.
        if (string.IsNullOrWhiteSpace(role) ||
            !AllowedRoles.Contains(role))
        {
            return new(
                false,
                400,
                "Le rôle demandé est invalide."
            );
        }

        // 5. Création du compte
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = request.PhoneNumber?.Trim(),
            IsActive = true,
            IsProfessionalApproved = false
        };

        var createResult = await _userManager.CreateAsync(
            user,
            password
        );

        if (!createResult.Succeeded)
        {
            return new(
                false,
                400,
                "Impossible de créer le compte.",
                createResult.Errors
                    .Select(error => error.Description)
                    .ToArray()
            );
        }

        // 6. Attribution du rôle demandé
        var roleResult = await _userManager.AddToRoleAsync(
            user,
            role!
        );

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            return new(
                false,
                500,
                "Impossible d'attribuer le rôle demandé.",
                roleResult.Errors
                    .Select(error => error.Description)
                    .ToArray()
            );
        }

        // 7. Message adapté au type de compte
        var message = role switch
        {
            "PATIENT" =>
                "Compte patient créé avec succès.",

            "MEDECIN" =>
                "Demande de compte médecin créée. " +
                "Votre compte doit être approuvé par l'administration.",

            "PHARMACIEN" =>
                "Demande de compte pharmacien créée. " +
                "Votre compte doit être approuvé par l'administration.",

            _ => "Compte créé avec succès."
        };

        var profile = new UserProfileResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email!,
            user.PhoneNumber,
            [role!]
        );

        return new(true, 201, message, profile);
    }

    public async Task<AuthResult> LoginAsync(
        LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return new(
                false,
                400,
                "Email et mot de passe obligatoires."
            );
        }

        var email = request.Email.Trim();

        var user = await _userManager.FindByEmailAsync(email);

        if (user is null || !user.IsActive)
        {
            return new(
                false,
                401,
                "Email ou mot de passe incorrect."
            );
        }

        var signInResult = await _signInManager
            .CheckPasswordSignInAsync(
                user,
                request.Password,
                lockoutOnFailure: true
            );

        if (!signInResult.Succeeded)
        {
            return new(
                false,
                401,
                "Email ou mot de passe incorrect."
            );
        }

        var roles = await _userManager.GetRolesAsync(user);

        var isProfessional =
            roles.Contains("MEDECIN") ||
            roles.Contains("PHARMACIEN");

        // Un professionnel non approuvé ne reçoit pas de JWT.
        // Il devra attendre la validation administrative.
        if (isProfessional && !user.IsProfessionalApproved)
        {
            return new(
                false,
                403,
                "Votre compte professionnel est en attente " +
                "de validation par l'administration."
            );
        }

        var expiresAt = DateTime.UtcNow.AddHours(1);

        var token = GenerateJwtToken(
            user,
            roles,
            expiresAt
        );

        var response = new AuthResponse(
            token,
            expiresAt,
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email!,
            user.PhoneNumber,
            roles.ToArray()
        );

        return new(
            true,
            200,
            "Connexion réussie.",
            response
        );
    }

    public async Task<AuthResult> GetCurrentUserAsync(
        string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user is null || !user.IsActive)
        {
            return new(
                false,
                401,
                "Utilisateur introuvable ou compte désactivé."
            );
        }

        var roles = await _userManager.GetRolesAsync(user);

        var profile = new UserProfileResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email!,
            user.PhoneNumber,
            roles.ToArray()
        );

        return new(
            true,
            200,
            "Profil récupéré.",
            profile
        );
    }

    private string GenerateJwtToken(
        AppUser user,
        IList<string> roles,
        DateTime expiresAt)
    {
        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "Jwt:Key manquant."
            );

        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "Jwt:Issuer manquant."
            );

        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "Jwt:Audience manquant."
            );

        if (Encoding.UTF8.GetByteCount(key) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Key doit contenir au moins 32 octets."
            );
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email!),
            new(ClaimTypes.Email, user.Email!)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key)
        );

        var credentials = new SigningCredentials(
            signingKey,
            SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}
