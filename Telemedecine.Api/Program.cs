
using System.Security.Claims;
using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

using Telemedecine.Api.Authorization;
using Telemedecine.Api.Data;
using Telemedecine.Api.Models;
using Telemedecine.Api.Services;
using Telemedecine.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// 1. Controllers et OpenAPI
// =====================================================

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// =====================================================
// 2. Connexion PostgreSQL
// =====================================================

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "La chaîne de connexion PostgreSQL est introuvable.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// =====================================================
// 3. Configuration ASP.NET Core Identity
// =====================================================

builder.Services
    .AddIdentityCore<AppUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan =
            TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager();

// =====================================================
// 4. Configuration JWT
// =====================================================

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key manquant.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer manquant.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience manquant.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key doit contenir au moins 32 octets.");
}

// =====================================================
// 5. Authentification JWT
// =====================================================

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

        // Vérifier l'état du compte à chaque requête JWT.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;

                var userId =
                    principal?.FindFirstValue(
                        ClaimTypes.NameIdentifier)
                    ?? principal?.FindFirstValue("sub");

                if (string.IsNullOrWhiteSpace(userId))
                {
                    context.Fail(
                        "Identifiant utilisateur manquant.");
                    return;
                }

                var userManager = context.HttpContext
                    .RequestServices
                    .GetRequiredService<UserManager<AppUser>>();

                var user = await userManager.FindByIdAsync(userId);

                if (user is null || !user.IsActive)
                {
                    context.Fail(
                        "Compte désactivé ou introuvable.");
                    return;
                }

                var roles =
                    await userManager.GetRolesAsync(user);

                var isProfessional =
                    roles.Contains("MEDECIN") ||
                    roles.Contains("PHARMACIEN");

                if (isProfessional &&
                    !user.IsProfessionalApproved)
                {
                    context.Fail(
                        "Compte professionnel non approuvé.");
                }
            }
        };
    });

// =====================================================
// 6. Autorisation et policies
// =====================================================

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApprovedProfessional", policy =>
    {
        policy.RequireAuthenticatedUser();

        policy.AddRequirements(
            new ApprovedProfessionalRequirement());
    });
});

// =====================================================
// 7. Enregistrement des handlers d'autorisation
// =====================================================

builder.Services.AddScoped<
    IAuthorizationHandler,
    ApprovedProfessionalHandler>();

// =====================================================
// 8. Enregistrement des services métier
// =====================================================

// Authentification
builder.Services.AddScoped<IAuthService, AuthService>();

// Espace Patient
builder.Services.AddScoped<IPatientService, PatientService>();

// Inscription Médecin
builder.Services.AddScoped<
    IDoctorRegistrationService,
    DoctorRegistrationService>();

builder.Services.AddScoped<IDoctorService, DoctorService>();

builder.Services.AddScoped<
    IPatientAppointmentService,
    PatientAppointmentService>();

// Gestion administrative des médecins
builder.Services.AddScoped<
    IAdminDoctorService,
    AdminDoctorService>();

    

// =====================================================
// 9. CORS pour Angular
// =====================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// =====================================================
// 10. Construction de l'application
// =====================================================

var app = builder.Build();

// =====================================================
// 11. OpenAPI en développement
// =====================================================

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// =====================================================
// 12. Middlewares HTTP
// =====================================================

app.UseHttpsRedirection();

app.UseCors("AngularFrontend");

app.UseAuthentication();

app.UseAuthorization();

// =====================================================
// 13. Initialisation des rôles et Admin
// =====================================================

await IdentitySeeder.SeedRolesAsync(app.Services);

// =====================================================
// 14. Vérification PostgreSQL
// =====================================================

app.MapGet("/api/health/database", async (
    AppDbContext db) =>
{
    var connected = await db.Database.CanConnectAsync();

    return Results.Ok(new
    {
        database = "telemedecine_db",
        connected
    });
});

// =====================================================
// 15. Enregistrement des Controllers
// =====================================================

app.MapControllers();

// =====================================================
// 16. Démarrage
// =====================================================

app.Run();
