using JobPortal.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Api.Data;

public class JobPortalDbContext : DbContext
{
    public JobPortalDbContext(DbContextOptions<JobPortalDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<CandidateProfile> CandidateProfiles => Set<CandidateProfile>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Application> Applications => Set<Application>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUser(modelBuilder);
        ConfigureCandidateProfile(modelBuilder);
        ConfigureCompany(modelBuilder);
        ConfigureJob(modelBuilder);
        ConfigureApplication(modelBuilder);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.UserId);

            entity.Property(user => user.FullName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(user => user.Email)
                .HasMaxLength(255)
                .IsRequired();

            entity.HasIndex(user => user.Email)
                .IsUnique();

            entity.Property(user => user.PasswordHash)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(user => user.Role)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(user => user.IsActive)
                .HasDefaultValue(true);

            entity.Property(user => user.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }

    private static void ConfigureCandidateProfile(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CandidateProfile>(entity =>
        {
            entity.ToTable(
                "CandidateProfiles",
                table => table.HasCheckConstraint(
                    "CK_CandidateProfiles_ExperienceYears",
                    "\"ExperienceYears\" >= 0"));

            entity.HasKey(profile => profile.CandidateProfileId);

            entity.HasIndex(profile => profile.UserId)
                .IsUnique();

            entity.Property(profile => profile.PhoneNumber)
                .HasMaxLength(20);

            entity.Property(profile => profile.Location)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(profile => profile.Skills)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(profile => profile.Education)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(profile => profile.ResumeUrl)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(profile => profile.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(profile => profile.User)
                .WithOne()
                .HasForeignKey<CandidateProfile>(profile => profile.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCompany(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(company => company.CompanyId);

            entity.HasIndex(company => company.EmployerUserId)
                .IsUnique();

            entity.Property(company => company.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(company => company.Website)
                .HasMaxLength(500);

            entity.Property(company => company.Location)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(company => company.Description)
                .HasMaxLength(2000)
                .IsRequired();

            entity.Property(company => company.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(company => company.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(company => company.EmployerUser)
                .WithOne()
                .HasForeignKey<Company>(company => company.EmployerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureJob(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Job>(entity =>
        {
            entity.ToTable(
                "Jobs",
                table => table.HasCheckConstraint(
                    "CK_Jobs_ExperienceRequiredYears",
                    "\"ExperienceRequiredYears\" >= 0"));

            entity.HasKey(job => job.JobId);

            entity.Property(job => job.Title)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(job => job.Description)
                .HasMaxLength(4000)
                .IsRequired();

            entity.Property(job => job.Location)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(job => job.JobType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(job => job.SalaryRange)
                .HasMaxLength(50);

            entity.Property(job => job.SkillsRequired)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(job => job.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(job => job.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(job => job.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(job => job.Company)
                .WithMany()
                .HasForeignKey(job => job.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureApplication(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Application>(entity =>
        {
            entity.HasKey(application => application.ApplicationId);

            entity.HasIndex(application =>
                    new { application.JobId, application.CandidateUserId })
                .IsUnique();

            entity.Property(application => application.ResumeUrl)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(application => application.CoverLetter)
                .HasMaxLength(2000);

            entity.Property(application => application.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(application => application.AppliedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(application => application.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(application => application.Job)
                .WithMany()
                .HasForeignKey(application => application.JobId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(application => application.CandidateUser)
                .WithMany()
                .HasForeignKey(application => application.CandidateUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}