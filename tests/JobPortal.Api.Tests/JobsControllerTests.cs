using System.Security.Claims;
using JobPortal.Api.Controllers;
using JobPortal.Api.Data;
using JobPortal.Api.Dtos.Jobs;
using JobPortal.Api.Models.Entities;
using JobPortal.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Api.Tests;

public class JobsControllerTests
{
    [Fact]
    public async Task Search_ReturnsOnlyAvailableJobs()
    {
        await using var dbContext = CreateDbContext();

        var activeCompany = CreateCompany(
            companyId: 1,
            employerUserId: 1,
            CompanyStatus.Active);

        var inactiveCompany = CreateCompany(
            companyId: 2,
            employerUserId: 2,
            CompanyStatus.Inactive);

        dbContext.Companies.AddRange(
            activeCompany,
            inactiveCompany);

        dbContext.Jobs.AddRange(
            CreateJob(
                jobId: 1,
                activeCompany,
                JobStatus.Open,
                DateTime.UtcNow.AddDays(10)),
            CreateJob(
                jobId: 2,
                activeCompany,
                JobStatus.Closed,
                DateTime.UtcNow.AddDays(10)),
            CreateJob(
                jobId: 3,
                inactiveCompany,
                JobStatus.Open,
                DateTime.UtcNow.AddDays(10)),
            CreateJob(
                jobId: 4,
                activeCompany,
                JobStatus.Open,
                DateTime.UtcNow.AddDays(-1)));

        await dbContext.SaveChangesAsync();

        var controller = new JobsController(dbContext);

        var result = await controller.Search(new JobSearchRequest());

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<JobListResponse>(okResult.Value);

        var returnedJob = Assert.Single(response.Items);

        Assert.Equal(1, returnedJob.JobId);
        Assert.Equal(1, response.TotalCount);
    }

    [Fact]
    public async Task Create_ValidRequest_CreatesJobWithDistinctSkills()
    {
        await using var dbContext = CreateDbContext();
        await SeedEmployerCompanyAndSkills(dbContext);

        var controller = CreateEmployerController(
            dbContext,
            userId: 1);

        var request = CreateValidRequest();
        request.SkillIds = [1, 2, 2];

        var result = await controller.Create(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(
            result.Result);

        var response = Assert.IsType<JobResponse>(
            createdResult.Value);

        Assert.Equal("Junior .NET Developer", response.Title);
        Assert.Equal(JobStatus.Open, response.Status);
        Assert.Equal(2, response.Skills.Count);

        var savedJob = await dbContext.Jobs
            .Include(job => job.JobSkills)
            .SingleAsync();

        Assert.Equal(1, savedJob.CompanyId);
        Assert.Equal(2, savedJob.JobSkills.Count);
    }

    [Fact]
    public async Task Create_MissingSkill_ReturnsBadRequestWithoutJob()
    {
        await using var dbContext = CreateDbContext();
        await SeedEmployerCompanyAndSkills(dbContext);

        var controller = CreateEmployerController(
            dbContext,
            userId: 1);

        var request = CreateValidRequest();
        request.SkillIds = [1, 9999];

        var result = await controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(dbContext.Jobs);
        Assert.Empty(dbContext.JobSkills);
    }

    [Fact]
    public async Task Update_AnotherEmployersJob_ReturnsNotFound()
    {
        await using var dbContext = CreateDbContext();

        var firstEmployer = CreateEmployer(1);
        var secondEmployer = CreateEmployer(2);

        var firstCompany = CreateCompany(
            companyId: 1,
            employerUserId: 1,
            CompanyStatus.Active);

        var secondCompany = CreateCompany(
            companyId: 2,
            employerUserId: 2,
            CompanyStatus.Active);

        var skill = new Skill
        {
            SkillId = 1,
            Name = "C#"
        };

        var secondCompanyJob = CreateJob(
            jobId: 1,
            secondCompany,
            JobStatus.Open,
            DateTime.UtcNow.AddDays(10));

        dbContext.Users.AddRange(
            firstEmployer,
            secondEmployer);

        dbContext.Companies.AddRange(
            firstCompany,
            secondCompany);

        dbContext.Skills.Add(skill);
        dbContext.Jobs.Add(secondCompanyJob);

        await dbContext.SaveChangesAsync();

        var controller = CreateEmployerController(
            dbContext,
            userId: 1);

        var request = CreateValidRequest();
        request.SkillIds = [1];

        var result = await controller.Update(
            secondCompanyJob.JobId,
            request);

        Assert.IsType<NotFoundObjectResult>(result.Result);

        Assert.Equal(
            secondCompany.CompanyId,
            secondCompanyJob.CompanyId);
    }

    [Fact]
    public async Task UpdateStatus_OwnJob_ClosesJob()
    {
        await using var dbContext = CreateDbContext();
        await SeedEmployerCompanyAndSkills(dbContext);

        var company = await dbContext.Companies.SingleAsync();

        var job = CreateJob(
            jobId: 1,
            company,
            JobStatus.Open,
            DateTime.UtcNow.AddDays(10));

        dbContext.Jobs.Add(job);
        await dbContext.SaveChangesAsync();

        var controller = CreateEmployerController(
            dbContext,
            userId: 1);

        var request = new UpdateJobStatusRequest
        {
            Status = JobStatus.Closed
        };

        var result = await controller.UpdateStatus(
            job.JobId,
            request);

        var okResult = Assert.IsType<OkObjectResult>(
            result.Result);

        var response = Assert.IsType<JobResponse>(
            okResult.Value);

        Assert.Equal(JobStatus.Closed, response.Status);

        var savedJob = await dbContext.Jobs.SingleAsync();

        Assert.Equal(JobStatus.Closed, savedJob.Status);
    }

    private static UpsertJobRequest CreateValidRequest()
    {
        return new UpsertJobRequest
        {
            Title = "Junior .NET Developer",
            Description = "Develop ASP.NET Core APIs.",
            Location = "Hyderabad",
            JobType = "Full-time",
            ExperienceRequiredYears = 1,
            SalaryRange = "3-5 LPA",
            ApplicationDeadline = DateTime.UtcNow.AddDays(30),
            SkillIds = [1, 2]
        };
    }

    private static User CreateEmployer(int userId)
    {
        return new User
        {
            UserId = userId,
            FullName = $"Employer {userId}",
            Email = $"employer{userId}@example.com",
            PasswordHash = "test-hash",
            Role = UserRole.Employer,
            IsActive = true
        };
    }

    private static Company CreateCompany(
        int companyId,
        int employerUserId,
        CompanyStatus status)
    {
        return new Company
        {
            CompanyId = companyId,
            EmployerUserId = employerUserId,
            Name = $"Company {companyId}",
            Location = "Hyderabad",
            Description = "Test company",
            Status = status
        };
    }

    private static Job CreateJob(
        int jobId,
        Company company,
        JobStatus status,
        DateTime deadline)
    {
        return new Job
        {
            JobId = jobId,
            CompanyId = company.CompanyId,
            Company = company,
            Title = "Junior .NET Developer",
            Description = "Develop APIs.",
            Location = "Hyderabad",
            JobType = "Full-time",
            ExperienceRequiredYears = 1,
            SalaryRange = "3-5 LPA",
            ApplicationDeadline = deadline,
            Status = status
        };
    }

    private static async Task SeedEmployerCompanyAndSkills(
        JobPortalDbContext dbContext)
    {
        var employer = CreateEmployer(1);

        var company = CreateCompany(
            companyId: 1,
            employerUserId: employer.UserId,
            CompanyStatus.Active);

        dbContext.Users.Add(employer);
        dbContext.Companies.Add(company);

        dbContext.Skills.AddRange(
            new Skill
            {
                SkillId = 1,
                Name = "C#"
            },
            new Skill
            {
                SkillId = 2,
                Name = "React"
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

    private static JobsController CreateEmployerController(
        JobPortalDbContext dbContext,
        int userId)
    {
        var controller = new JobsController(dbContext);

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
                            UserRole.Employer.ToString())
                    ],
                    "UnitTest"))
            }
        };

        return controller;
    }
}