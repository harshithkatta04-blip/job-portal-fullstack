using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using JobPortal.Api.Data;
using JobPortal.Api.Dtos.Companies;
using JobPortal.Api.Models.Entities;
using JobPortal.Api.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Employer))]
[Route("api/companies")]
public class CompaniesController : ControllerBase
{
    private readonly JobPortalDbContext _dbContext;

    public CompaniesController(JobPortalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("me")]
    public async Task<ActionResult<CompanyResponse>> GetMe()
    {
        var employerUserId = GetUserId();

        if (employerUserId is null)
        {
            return Unauthorized(new { message = "Invalid user token." });
        }

        if (!await IsActiveEmployer(employerUserId.Value))
        {
            return Unauthorized(new { message = "Employer account is unavailable." });
        }

        var company = await _dbContext.Companies
            .AsNoTracking()
            .SingleOrDefaultAsync(company =>
                company.EmployerUserId == employerUserId.Value);

        if (company is null)
        {
            return NotFound(new { message = "Company was not found." });
        }

        return Ok(ToResponse(company));
    }

    [HttpPut("me")]
    public async Task<ActionResult<CompanyResponse>> UpdateMe(
        UpdateCompanyRequest request)
    {
        var employerUserId = GetUserId();

        if (employerUserId is null)
        {
            return Unauthorized(new { message = "Invalid user token." });
        }

        if (!await IsActiveEmployer(employerUserId.Value))
        {
            return Unauthorized(new { message = "Employer account is unavailable." });
        }

        var company = await _dbContext.Companies
            .SingleOrDefaultAsync(company =>
                company.EmployerUserId == employerUserId.Value);

        if (company is null)
        {
            return NotFound(new { message = "Company was not found." });
        }

        company.Name = request.Name.Trim();
        company.Location = request.Location.Trim();
        company.Description = request.Description.Trim();
        company.Website = string.IsNullOrWhiteSpace(request.Website)
            ? null
            : request.Website.Trim();
        company.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(ToResponse(company));
    }

    [HttpPatch("me/status")]
    public async Task<ActionResult<CompanyResponse>> UpdateStatus(
        UpdateCompanyStatusRequest request)
    {
        var employerUserId = GetUserId();

        if (employerUserId is null)
        {
            return Unauthorized(new { message = "Invalid user token." });
        }

        if (!await IsActiveEmployer(employerUserId.Value))
        {
            return Unauthorized(new { message = "Employer account is unavailable." });
        }

        if (!Enum.IsDefined(request.Status))
        {
            return BadRequest(new { message = "Invalid company status." });
        }

        var company = await _dbContext.Companies
            .SingleOrDefaultAsync(company =>
                company.EmployerUserId == employerUserId.Value);

        if (company is null)
        {
            return NotFound(new { message = "Company was not found." });
        }

        company.Status = request.Status;
        company.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(ToResponse(company));
    }

    private int? GetUserId()
    {
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return int.TryParse(userIdValue, out var userId)
            ? userId
            : null;
    }

    private Task<bool> IsActiveEmployer(int userId)
    {
        return _dbContext.Users.AnyAsync(user =>
            user.UserId == userId &&
            user.Role == UserRole.Employer &&
            user.IsActive);
    }

    private static CompanyResponse ToResponse(Company company)
    {
        return new CompanyResponse
        {
            CompanyId = company.CompanyId,
            EmployerUserId = company.EmployerUserId,
            Name = company.Name,
            Website = company.Website,
            Location = company.Location,
            Description = company.Description,
            Status = company.Status,
            UpdatedAt = company.UpdatedAt
        };
    }
}