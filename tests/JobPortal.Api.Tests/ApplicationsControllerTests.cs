using System.Security.Claims;
using JobPortal.Api.Controllers;
using JobPortal.Api.Data;
using JobPortal.Api.Dtos.Applications;
using JobPortal.Api.Models.Entities;
using JobPortal.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Api.Tests;

public class ApplicationsControllerTests
{
    [Fact]
    public async Task Apply_CompletedProfile_CreatesApplication()
    {
        await using var dbContext = CreateDbContext();
        await SeedBaseData(dbContext);

        var controller = CreateController(
            dbContext,
            userId: 1,
            UserRole.Candidate);

        var request = new ApplyJobRequest
        {
            CoverLetter = " I am interested in this role. "
        };

        var result = await controller.Apply(1, request);

        var createdResult = Assert.IsType<ObjectResult>(
             result.Result);

        Assert.Equal(
            StatusCodes.Status201Created,
            createdResult.StatusCode);

        var response = Assert.IsType<ApplicationResponse>(
            createdResult.Value);

        Assert.Equal(ApplicationStatus.Applied, response.Status);
        Assert.Equal("https://example.com/resume.pdf", response.ResumeUrl);
        Assert.Equal(
            "I am interested in this role.",
            response.CoverLetter);

        var savedApplication =
            await dbContext.Applications.SingleAsync();

        Assert.Equal(1, savedApplication.JobId);
        Assert.Equal(1, savedApplication.CandidateUserId);
        Assert.Equal(
            ApplicationStatus.Applied,
            savedApplication.Status);
    }

    [Fact]
    public async Task Apply_IncompleteProfile_ReturnsBadRequest()
    {
        await using var dbContext = CreateDbContext();
        await SeedBaseData(dbContext);

        var profile =
            await dbContext.CandidateProfiles.SingleAsync();

        dbContext.CandidateProfiles.Remove(profile);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(
            dbContext,
            userId: 1,
            UserRole.Candidate);

        var result = await controller.Apply(
            1,
            new ApplyJobRequest());

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(dbContext.Applications);
    }

    [Fact]
    public async Task Apply_DuplicateApplication_ReturnsConflict()
    {
        await using var dbContext = CreateDbContext();
        await SeedBaseData(dbContext);
        await SeedApplication(dbContext);

        var controller = CreateController(
            dbContext,
            userId: 1,
            UserRole.Candidate);

        var result = await controller.Apply(
            1,
            new ApplyJobRequest());

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(1, await dbContext.Applications.CountAsync());
    }

    [Fact]
    public async Task Apply_AfterWithdrawal_ReturnsConflict()
    {
        await using var dbContext = CreateDbContext();
        await SeedBaseData(dbContext);
        await SeedApplication(dbContext);

        var existingApplication =
            await dbContext.Applications.SingleAsync();

        existingApplication.Status = ApplicationStatus.Withdrawn;
        await dbContext.SaveChangesAsync();

        var controller = CreateController(
            dbContext,
            userId: 1,
            UserRole.Candidate);

        var result = await controller.Apply(
            1,
            new ApplyJobRequest());

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(1, await dbContext.Applications.CountAsync());
    }

    [Fact]
    public async Task GetForJob_OwningEmployer_ReturnsApplicants()
    {
        await using var dbContext = CreateDbContext();
        await SeedBaseData(dbContext);
        await SeedApplication(dbContext);

        var controller = CreateController(
            dbContext,
            userId: 2,
            UserRole.Employer);

        var result = await controller.GetForJob(
    1,
    new ApplicationListRequest());
        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        var response = Assert.IsType<ApplicationListResponse>(
    okResult.Value);

        var application = Assert.Single(response.Items);

        Assert.Equal(1, response.Page);
        Assert.Equal(20, response.PageSize);
        Assert.Equal(1, response.TotalCount);
        Assert.Equal(1, response.TotalPages);

        Assert.Equal("Test Candidate", application.CandidateName);
        Assert.Equal(
            "candidate@example.com",
            application.CandidateEmail);
        Assert.Equal(1, application.JobId);
    }

    [Fact]
    public async Task GetForJob_OtherEmployer_ReturnsNotFound()
    {
        await using var dbContext = CreateDbContext();
        await SeedBaseData(dbContext);
        await SeedApplication(dbContext);

        dbContext.Users.Add(new User
        {
            UserId = 3,
            FullName = "Other Employer",
            Email = "other.employer@example.com",
            PasswordHash = "test-hash",
            Role = UserRole.Employer,
            IsActive = true
        });

        await dbContext.SaveChangesAsync();

        var controller = CreateController(
            dbContext,
            userId: 3,
            UserRole.Employer);

        var result = await controller.GetForJob(
    1,
    new ApplicationListRequest());
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateStatus_OwningEmployer_UpdatesApplication()
    {
        await using var dbContext = CreateDbContext();
        await SeedBaseData(dbContext);
        await SeedApplication(dbContext);

        var controller = CreateController(
            dbContext,
            userId: 2,
            UserRole.Employer);

        var request = new UpdateApplicationStatusRequest
        {
            Status = ApplicationStatus.UnderReview
        };

        var result = await controller.UpdateStatus(1, request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        var response = Assert.IsType<ApplicationResponse>(
            okResult.Value);

        Assert.Equal(
            ApplicationStatus.UnderReview,
            response.Status);

        var savedApplication =
            await dbContext.Applications.SingleAsync();

        Assert.Equal(
            ApplicationStatus.UnderReview,
            savedApplication.Status);
    }

    [Fact]
    public async Task Withdraw_OwningCandidate_WithdrawsApplication()
    {
        await using var dbContext = CreateDbContext();
        await SeedBaseData(dbContext);
        await SeedApplication(dbContext);

        var controller = CreateController(
            dbContext,
            userId: 1,
            UserRole.Candidate);

        var result = await controller.Withdraw(1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        var response = Assert.IsType<ApplicationResponse>(
            okResult.Value);

        Assert.Equal(
            ApplicationStatus.Withdrawn,
            response.Status);

        var savedApplication =
            await dbContext.Applications.SingleAsync();

        Assert.Equal(
            ApplicationStatus.Withdrawn,
            savedApplication.Status);
    }

    [Fact]
    public async Task GetMine_WithPagination_ReturnsRequestedPage()
    {
        await using var dbContext = CreateDbContext();
        await SeedBaseData(dbContext);
        await SeedApplication(dbContext);

        var firstApplication =
            await dbContext.Applications.SingleAsync();

        firstApplication.AppliedAt =
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

        dbContext.Jobs.Add(new Job
        {
            JobId = 2,
            CompanyId = 1,
            Title = "Senior .NET Developer",
            Description = "Develop backend services.",
            Location = "Bengaluru",
            JobType = JobType.FullTime,
            ExperienceRequiredYears = 3,
            SalaryRange = "8-10 LPA",
            ApplicationDeadline = DateTime.UtcNow.AddDays(30),
            Status = JobStatus.Open
        });

        dbContext.Applications.Add(new Application
        {
            ApplicationId = 2,
            JobId = 2,
            CandidateUserId = 1,
            ResumeUrl = "https://example.com/resume.pdf",
            CoverLetter = "Second application",
            Status = ApplicationStatus.Applied,
            AppliedAt =
                new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc)
        });

        await dbContext.SaveChangesAsync();

        var controller = CreateController(
            dbContext,
            userId: 1,
            UserRole.Candidate);

        var request = new ApplicationListRequest
        {
            Page = 2,
            PageSize = 1
        };

        var result = await controller.GetMine(request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApplicationListResponse>(
            okResult.Value);

        var application = Assert.Single(response.Items);

        Assert.Equal(1, application.JobId);
        Assert.Equal(2, response.Page);
        Assert.Equal(1, response.PageSize);
        Assert.Equal(2, response.TotalCount);
        Assert.Equal(2, response.TotalPages);
    }

    [Fact]
    public async Task GetForJob_WithPagination_ReturnsRequestedPage()
    {
        await using var dbContext = CreateDbContext();
        await SeedBaseData(dbContext);
        await SeedApplication(dbContext);

        var firstApplication =
            await dbContext.Applications.SingleAsync();

        firstApplication.AppliedAt =
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

        dbContext.Users.Add(new User
        {
            UserId = 3,
            FullName = "Second Candidate",
            Email = "second.candidate@example.com",
            PasswordHash = "test-hash",
            Role = UserRole.Candidate,
            IsActive = true
        });

        dbContext.Applications.Add(new Application
        {
            ApplicationId = 2,
            JobId = 1,
            CandidateUserId = 3,
            ResumeUrl = "https://example.com/second-resume.pdf",
            CoverLetter = "Second candidate application",
            Status = ApplicationStatus.Applied,
            AppliedAt =
                new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc)
        });

        await dbContext.SaveChangesAsync();

        var controller = CreateController(
            dbContext,
            userId: 2,
            UserRole.Employer);

        var request = new ApplicationListRequest
        {
            Page = 2,
            PageSize = 1
        };

        var result = await controller.GetForJob(1, request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApplicationListResponse>(
            okResult.Value);

        var application = Assert.Single(response.Items);

        Assert.Equal(1, application.ApplicationId);
        Assert.Equal("Test Candidate", application.CandidateName);
        Assert.Equal(2, response.Page);
        Assert.Equal(1, response.PageSize);
        Assert.Equal(2, response.TotalCount);
        Assert.Equal(2, response.TotalPages);
    }

    private static async Task SeedBaseData(
           JobPortalDbContext dbContext)
    {
        var candidate = new User
        {
            UserId = 1,
            FullName = "Test Candidate",
            Email = "candidate@example.com",
            PasswordHash = "test-hash",
            Role = UserRole.Candidate,
            IsActive = true
        };

        var employer = new User
        {
            UserId = 2,
            FullName = "Test Employer",
            Email = "employer@example.com",
            PasswordHash = "test-hash",
            Role = UserRole.Employer,
            IsActive = true
        };

        var skill = new Skill
        {
            SkillId = 1,
            Name = "C#"
        };

        var profile = new CandidateProfile
        {
            CandidateProfileId = 1,
            UserId = candidate.UserId,
            User = candidate,
            Location = "Hyderabad",
            Education = "B.Tech",
            ExperienceYears = 1,
            ResumeUrl = "https://example.com/resume.pdf",
            CandidateProfileSkills =
            [
                new CandidateProfileSkill
                {
                    Skill = skill
                }
            ]
        };

        var company = new Company
        {
            CompanyId = 1,
            EmployerUserId = employer.UserId,
            EmployerUser = employer,
            Name = "Test Technologies",
            Location = "Hyderabad",
            Description = "Test company",
            Status = CompanyStatus.Active
        };

        var job = new Job
        {
            JobId = 1,
            CompanyId = company.CompanyId,
            Company = company,
            Title = "Junior .NET Developer",
            Description = "Develop APIs.",
            Location = "Hyderabad",
            JobType = JobType.FullTime,
            ExperienceRequiredYears = 1,
            SalaryRange = "3-5 LPA",
            ApplicationDeadline = DateTime.UtcNow.AddDays(30),
            Status = JobStatus.Open
        };

        dbContext.Users.AddRange(candidate, employer);
        dbContext.Skills.Add(skill);
        dbContext.CandidateProfiles.Add(profile);
        dbContext.Companies.Add(company);
        dbContext.Jobs.Add(job);

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedApplication(
        JobPortalDbContext dbContext)
    {
        dbContext.Applications.Add(new Application
        {
            ApplicationId = 1,
            JobId = 1,
            CandidateUserId = 1,
            ResumeUrl = "https://example.com/resume.pdf",
            CoverLetter = "Test cover letter",
            Status = ApplicationStatus.Applied
        });

        await dbContext.SaveChangesAsync();
    }

    private static JobPortalDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<JobPortalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new JobPortalDbContext(options);
    }

    private static ApplicationsController CreateController(
        JobPortalDbContext dbContext,
        int userId,
        UserRole role)
    {
        var controller = new ApplicationsController(dbContext);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                    [
                        new Claim(
                            ClaimTypes.NameIdentifier,
                            userId.ToString()),
                        new Claim(
                            ClaimTypes.Role,
                            role.ToString())
                    ],
                    "UnitTest"))
            }
        };

        return controller;
    }
}