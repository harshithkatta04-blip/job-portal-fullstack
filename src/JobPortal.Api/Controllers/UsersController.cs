using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using JobPortal.Api.Data;
using JobPortal.Api.Dtos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly JobPortalDbContext _dbContext;

    public UsersController(JobPortalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetMe()
    {
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token." });
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.UserId == userId);

        if (user is null || !user.IsActive)
        {
            return Unauthorized(new { message = "User account is unavailable." });
        }

        return Ok(new UserResponse
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        });
    }
}