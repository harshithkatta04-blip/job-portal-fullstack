using JobPortal.Api.Controllers;
using JobPortal.Api.Data;
using JobPortal.Api.Dtos.Auth;
using JobPortal.Api.Models.Entities;
using JobPortal.Api.Models.Enums;
using JobPortal.Api.Services.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Api.Tests;

public class AuthControllerTests
{
    [Fact]
    public async Task Register_Candidate_CreatesUserAndReturnsToken()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);

        var request = new RegisterRequest
        {
            FullName = "Test Candidate",
            Email = "CANDIDATE@EXAMPLE.COM",
            Password = "Candidate@123",
            Role = UserRole.Candidate
        };

        var result = await controller.Register(request);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

        var response = Assert.IsType<AuthResponse>(objectResult.Value);
        Assert.Equal("candidate@example.com", response.Email);
        Assert.Equal("Candidate", response.Role);
        Assert.Equal("unit-test-token", response.Token);

        var savedUser = await dbContext.Users.SingleAsync();

        Assert.Equal("Test Candidate", savedUser.FullName);
        Assert.Equal("candidate@example.com", savedUser.Email);
        Assert.NotEqual(request.Password, savedUser.PasswordHash);
        Assert.Empty(dbContext.Companies);
    }

    [Fact]
    public async Task Register_Employer_CreatesUserAndCompany()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);

        var request = new RegisterRequest
        {
            FullName = "Test Employer",
            Email = "employer@example.com",
            Password = "Employer@123",
            Role = UserRole.Employer,
            CompanyName = "Test Technologies",
            CompanyLocation = "Hyderabad",
            CompanyDescription = "Software company",
            CompanyWebsite = "https://example.com"
        };

        var result = await controller.Register(request);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status201Created, objectResult.StatusCode);

        var response = Assert.IsType<AuthResponse>(objectResult.Value);
        Assert.Equal("Employer", response.Role);
        Assert.Equal("unit-test-token", response.Token);

        var savedUser = await dbContext.Users.SingleAsync();
        var savedCompany = await dbContext.Companies.SingleAsync();

        Assert.Equal(UserRole.Employer, savedUser.Role);
        Assert.Equal(savedUser.UserId, savedCompany.EmployerUserId);
        Assert.Equal("Test Technologies", savedCompany.Name);
        Assert.Equal("Hyderabad", savedCompany.Location);
        Assert.Equal("https://example.com", savedCompany.Website);
    }


    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        await using var dbContext = CreateDbContext();

        dbContext.Users.Add(new User
        {
            FullName = "Existing User",
            Email = "existing@example.com",
            PasswordHash = "existing-hash",
            Role = UserRole.Candidate
        });

        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext);

        var request = new RegisterRequest
        {
            FullName = "Another User",
            Email = "EXISTING@EXAMPLE.COM",
            Password = "Candidate@123",
            Role = UserRole.Candidate
        };

        var result = await controller.Register(request);

        Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(1, await dbContext.Users.CountAsync());
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        await using var dbContext = CreateDbContext();

        var passwordHasher = new PasswordHasher<User>();

        var user = new User
        {
            FullName = "Login Candidate",
            Email = "login@example.com",
            Role = UserRole.Candidate,
            IsActive = true
        };

        user.PasswordHash = passwordHasher.HashPassword(
            user,
            "Candidate@123");

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext);

        var request = new LoginRequest
        {
            Email = "LOGIN@EXAMPLE.COM",
            Password = "Candidate@123"
        };

        var result = await controller.Login(request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);

        Assert.Equal(user.UserId, response.UserId);
        Assert.Equal("login@example.com", response.Email);
        Assert.Equal("Candidate", response.Role);
        Assert.Equal("unit-test-token", response.Token);
    }
    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        await using var dbContext = CreateDbContext();

        var passwordHasher = new PasswordHasher<User>();

        var user = new User
        {
            FullName = "Login Candidate",
            Email = "login@example.com",
            Role = UserRole.Candidate,
            IsActive = true
        };

        user.PasswordHash = passwordHasher.HashPassword(
            user,
            "CorrectPassword@123");

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext);

        var request = new LoginRequest
        {
            Email = "login@example.com",
            Password = "WrongPassword@123"
        };

        var result = await controller.Login(request);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_InactiveUser_ReturnsUnauthorized()
    {
        await using var dbContext = CreateDbContext();

        var passwordHasher = new PasswordHasher<User>();

        var user = new User
        {
            FullName = "Inactive Candidate",
            Email = "inactive@example.com",
            Role = UserRole.Candidate,
            IsActive = false
        };

        user.PasswordHash = passwordHasher.HashPassword(
            user,
            "Candidate@123");

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext);

        var request = new LoginRequest
        {
            Email = "inactive@example.com",
            Password = "Candidate@123"
        };

        var result = await controller.Login(request);

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }
    [Fact]
    public async Task Register_EmployerWithoutCompanyDetails_ReturnsBadRequest()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);

        var request = new RegisterRequest
        {
            FullName = "Test Employer",
            Email = "employer@example.com",
            Password = "Employer@123",
            Role = UserRole.Employer
        };

        var result = await controller.Register(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(dbContext.Users);
        Assert.Empty(dbContext.Companies);
    }
    private static JobPortalDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<JobPortalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new JobPortalDbContext(options);
    }

    private static AuthController CreateController(
        JobPortalDbContext dbContext)
    {
        return new AuthController(
            dbContext,
            new PasswordHasher<User>(),
            new FakeJwtTokenService());
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public (string Token, DateTime ExpiresAt) CreateToken(User user)
        {
            return (
                "unit-test-token",
                new DateTime(
                    2030,
                    1,
                    1,
                    0,
                    0,
                    0,
                    DateTimeKind.Utc));
        }
    }
}