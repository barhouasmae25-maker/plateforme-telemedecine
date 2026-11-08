
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Telemedecine.Api.Models;

namespace Telemedecine.Api.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Un compte utilisateur ne peut avoir qu'un seul profil médecin.
        builder.Entity<Doctor>()
            .HasIndex(d => d.UserId)
            .IsUnique();

        builder.Entity<Doctor>()
            .HasIndex(d => d.LicenseNumber)
            .IsUnique();

        // Relation entre médecin et compte utilisateur.
        builder.Entity<Doctor>()
            .HasOne(d => d.User)
            .WithOne()
            .HasForeignKey<Doctor>(d => d.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relation entre patient et compte utilisateur.
        builder.Entity<Patient>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relation entre rendez-vous et patient.
        builder.Entity<Appointment>()
            .HasOne(a => a.Patient)
            .WithMany()
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relation entre rendez-vous et médecin.
        builder.Entity<Appointment>()
            .HasOne(a => a.Doctor)
            .WithMany()
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Doctor>()
            .Property(d => d.ConsultationFee)
            .HasPrecision(10, 2);

        builder.Entity<Appointment>()
            .HasIndex(a => new { a.DoctorId, a.StartAt });

        builder.Entity<Appointment>()
            .Property(a => a.Status)
            .HasConversion<string>();
    }
}
