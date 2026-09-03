using System.Security.Claims;
using JobPortal.Api.Controllers;
using JobPortal.Api.Data;
using JobPortal.Api.Dtos.Companies;
using JobPortal.Api.Models.Entities;
using JobPortal.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Api.Tests;

public class CompaniesControllerTests
{
    [Fact]
    public async Task GetMe_ExistingCompany_ReturnsCompany()
    {
        await using var dbContext = CreateDbContext();
        await SeedEmployerAndCompany(dbContext);

        var controller = CreateController(dbContext, userId: 1);

        var result = await controller.GetMe();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CompanyResponse>(okResult.Value);

        Assert.Equal(1, response.EmployerUserId);
        Assert.Equal("Test Technologies", response.Name);
        Assert.Equal("Hyderabad", response.Location);
        Assert.Equal(CompanyStatus.Active, response.Status);
    }

    [Fact]
    public async Task UpdateMe_ExistingCompany_UpdatesCompanyDetails()
    {
        await using var dbContext = CreateDbContext();
        await SeedEmployerAndCompany(dbContext);

        var controller = CreateController(dbContext, userId: 1);

        var request = new UpdateCompanyRequest
        {
            Name = " Updated Technologies ",
            Website = " https://updated.example.com ",
            Location = " Bengaluru ",
            Description = " Updated company description "
        };

        var result = await controller.UpdateMe(request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CompanyResponse>(okResult.Value);

        Assert.Equal("Updated Technologies", response.Name);
        Assert.Equal("https://updated.example.com", response.Website);
        Assert.Equal("Bengaluru", response.Location);
        Assert.Equal("Updated company description", response.Description);

        var savedCompany = await dbContext.Companies.SingleAsync();

        Assert.Equal("Updated Technologies", savedCompany.Name);
        Assert.Equal("Bengaluru", savedCompany.Location);
    }

    [Fact]
    public async Task UpdateStatus_ValidStatus_ChangesCompanyStatus()
    {
        await using var dbContext = CreateDbContext();
        await SeedEmployerAndCompany(dbContext);

        var controller = CreateController(dbContext, userId: 1);

        var request = new UpdateCompanyStatusRequest
        {
            Status = CompanyStatus.Inactive
        };

        var result = await controller.UpdateStatus(request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CompanyResponse>(okResult.Value);

        Assert.Equal(CompanyStatus.Inactive, response.Status);

        var savedCompany = await dbContext.Companies.SingleAsync();

        Assert.Equal(CompanyStatus.Inactive, savedCompany.Status);
    }

    private static async Task SeedEmployerAndCompany(
        JobPortalDbContext dbContext)
    {
        var employer = new User
        {
            UserId = 1,
            FullName = "Test Employer",
            Email = "employer@example.com",
            PasswordHash = "test-hash",
            Role = UserRole.Employer,
            IsActive = true
        };

        var company = new Company
        {
            CompanyId = 1,
            EmployerUserId = employer.UserId,
            Name = "Test Technologies",
            Website = "https://example.com",
            Location = "Hyderabad",
            Description = "Test company",
            Status = CompanyStatus.Active
        };

        dbContext.Users.Add(employer);
        dbContext.Companies.Add(company);

        await dbContext.SaveChangesAsync();
    }

    private static JobPortalDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<JobPortalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new JobPortalDbContext(options);
    }

    private static CompaniesController CreateController(
        JobPortalDbContext dbContext,
        int userId)
    {
        var controller = new CompaniesController(dbContext);

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