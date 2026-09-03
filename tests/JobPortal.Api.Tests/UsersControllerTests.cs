using System.Security.Claims;
using JobPortal.Api.Controllers;
using JobPortal.Api.Data;
using JobPortal.Api.Dtos.Users;
using JobPortal.Api.Models.Entities;
using JobPortal.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Api.Tests;

public class UsersControllerTests
{
    [Fact]
    public async Task GetMe_ValidUser_ReturnsSafeUserDetails()
    {
        await using var dbContext = CreateDbContext();

        var user = new User
        {
            FullName = "Test Candidate",
            Email = "candidate@example.com",
            PasswordHash = "secret-hash",
            Role = UserRole.Candidate,
            IsActive = true
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var controller = new UsersController(dbContext);
        SetUser(controller, user.UserId, UserRole.Candidate);

        var result = await controller.GetMe();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<UserResponse>(okResult.Value);

        Assert.Equal(user.UserId, response.UserId);
        Assert.Equal("Test Candidate", response.FullName);
        Assert.Equal("candidate@example.com", response.Email);
        Assert.Equal(UserRole.Candidate, response.Role);
        Assert.True(response.IsActive);
    }

    private static JobPortalDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<JobPortalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new JobPortalDbContext(options);
    }

    private static void SetUser(
        ControllerBase controller,
        int userId,
        UserRole role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role.ToString())
        };

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(claims, "TestAuthentication"))
            }
        };
    }
}