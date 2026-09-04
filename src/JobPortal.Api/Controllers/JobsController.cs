using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using JobPortal.Api.Data;
using JobPortal.Api.Dtos.Jobs;
using JobPortal.Api.Models.Entities;
using JobPortal.Api.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Api.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobsController : ControllerBase
{
    private readonly JobPortalDbContext _dbContext;

    public JobsController(JobPortalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<JobListResponse>> Search(
        [FromQuery] JobSearchRequest request)
    {
        var now = DateTime.UtcNow;

        var query = _dbContext.Jobs
            .AsNoTracking()
            .Include(job => job.Company)
            .Include(job => job.JobSkills)
            .ThenInclude(jobSkill => jobSkill.Skill)
            .Where(job =>
                job.Status == JobStatus.Open &&
                job.ApplicationDeadline >= now &&
                job.Company.Status == CompanyStatus.Active);

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            var title = request.Title.Trim().ToLower();

            query = query.Where(job =>
                job.Title.ToLower().Contains(title));
        }

        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            var location = request.Location.Trim().ToLower();

            query = query.Where(job =>
                job.Location.ToLower().Contains(location));
        }

        if (!string.IsNullOrWhiteSpace(request.JobType))
        {
            var jobType = request.JobType.Trim().ToLower();

            query = query.Where(job =>
                job.JobType.ToLower() == jobType);
        }

        if (request.ExperienceYears.HasValue)
        {
            query = query.Where(job =>
                job.ExperienceRequiredYears <=
                request.ExperienceYears.Value);
        }

        var totalCount = await query.CountAsync();

        var jobs = await query
            .OrderByDescending(job => job.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return Ok(new JobListResponse
        {
            Items = jobs.Select(ToResponse).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    totalCount / (double)request.PageSize)
        });
    }

    [AllowAnonymous]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<JobResponse>> GetById(int id)
    {
        var now = DateTime.UtcNow;

        var job = await _dbContext.Jobs
            .AsNoTracking()
            .Include(job => job.Company)
            .Include(job => job.JobSkills)
            .ThenInclude(jobSkill => jobSkill.Skill)
            .SingleOrDefaultAsync(job =>
                job.JobId == id &&
                job.Status == JobStatus.Open &&
                job.ApplicationDeadline >= now &&
                job.Company.Status == CompanyStatus.Active);

        if (job is null)
        {
            return NotFound(new
            {
                message = "Job was not found or is unavailable."
            });
        }

        return Ok(ToResponse(job));
    }

    [Authorize(Roles = nameof(UserRole.Employer))]
    [HttpGet("mine")]
    public async Task<ActionResult<List<JobResponse>>> GetMine()
    {
        var employerUserId = GetUserId();

        if (employerUserId is null)
        {
            return Unauthorized(new { message = "Invalid user token." });
        }

        if (!await IsActiveEmployer(employerUserId.Value))
        {
            return Unauthorized(new
            {
                message = "Employer account is unavailable."
            });
        }

        var company = await _dbContext.Companies
            .AsNoTracking()
            .SingleOrDefaultAsync(company =>
                company.EmployerUserId == employerUserId.Value);

        if (company is null)
        {
            return NotFound(new { message = "Company was not found." });
        }

        var jobs = await _dbContext.Jobs
            .AsNoTracking()
            .Include(job => job.Company)
            .Include(job => job.JobSkills)
            .ThenInclude(jobSkill => jobSkill.Skill)
            .Where(job => job.CompanyId == company.CompanyId)
            .OrderByDescending(job => job.CreatedAt)
            .ToListAsync();

        return Ok(jobs.Select(ToResponse).ToList());
    }

    [Authorize(Roles = nameof(UserRole.Employer))]
    [HttpPost]
    public async Task<ActionResult<JobResponse>> Create(
        UpsertJobRequest request)
    {
        var employerUserId = GetUserId();

        if (employerUserId is null)
        {
            return Unauthorized(new { message = "Invalid user token." });
        }

        if (!await IsActiveEmployer(employerUserId.Value))
        {
            return Unauthorized(new
            {
                message = "Employer account is unavailable."
            });
        }

        var company = await _dbContext.Companies
            .SingleOrDefaultAsync(company =>
                company.EmployerUserId == employerUserId.Value);

        if (company is null)
        {
            return NotFound(new { message = "Company was not found." });
        }

        if (company.Status != CompanyStatus.Active)
        {
            return BadRequest(new
            {
                message = "An inactive company cannot create jobs."
            });
        }

        var validationResult = await ValidateRequest(request);

        if (validationResult is not null)
        {
            return validationResult;
        }

        var skillIds = request.SkillIds.Distinct().ToList();

        var skills = await _dbContext.Skills
            .Where(skill => skillIds.Contains(skill.SkillId))
            .ToListAsync();

        var now = DateTime.UtcNow;

        var job = new Job
        {
            CompanyId = company.CompanyId,
            Company = company,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Location = request.Location.Trim(),
            JobType = request.JobType.Trim(),
            ExperienceRequiredYears =
                request.ExperienceRequiredYears,
            SalaryRange = string.IsNullOrWhiteSpace(
                request.SalaryRange)
                ? null
                : request.SalaryRange.Trim(),
            ApplicationDeadline = request.ApplicationDeadline,
            Status = JobStatus.Open,
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var skill in skills)
        {
            job.JobSkills.Add(new JobSkill
            {
                Skill = skill
            });
        }

        _dbContext.Jobs.Add(job);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = job.JobId },
            ToResponse(job));
    }

    [Authorize(Roles = nameof(UserRole.Employer))]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<JobResponse>> Update(
        int id,
        UpsertJobRequest request)
    {
        var employerUserId = GetUserId();

        if (employerUserId is null)
        {
            return Unauthorized(new { message = "Invalid user token." });
        }

        if (!await IsActiveEmployer(employerUserId.Value))
        {
            return Unauthorized(new
            {
                message = "Employer account is unavailable."
            });
        }

        var company = await _dbContext.Companies
            .SingleOrDefaultAsync(company =>
                company.EmployerUserId == employerUserId.Value);

        if (company is null)
        {
            return NotFound(new { message = "Company was not found." });
        }

        if (company.Status != CompanyStatus.Active)
        {
            return BadRequest(new
            {
                message = "An inactive company cannot update jobs."
            });
        }

        var job = await _dbContext.Jobs
            .Include(job => job.Company)
            .Include(job => job.JobSkills)
            .ThenInclude(jobSkill => jobSkill.Skill)
            .SingleOrDefaultAsync(job =>
                job.JobId == id &&
                job.CompanyId == company.CompanyId);

        if (job is null)
        {
            return NotFound(new { message = "Job was not found." });
        }

        var validationResult = await ValidateRequest(request);

        if (validationResult is not null)
        {
            return validationResult;
        }

        var skillIds = request.SkillIds.Distinct().ToList();

        var skills = await _dbContext.Skills
            .Where(skill => skillIds.Contains(skill.SkillId))
            .ToListAsync();

        job.Title = request.Title.Trim();
        job.Description = request.Description.Trim();
        job.Location = request.Location.Trim();
        job.JobType = request.JobType.Trim();
        job.ExperienceRequiredYears =
            request.ExperienceRequiredYears;
        job.SalaryRange = string.IsNullOrWhiteSpace(
            request.SalaryRange)
            ? null
            : request.SalaryRange.Trim();
        job.ApplicationDeadline = request.ApplicationDeadline;
        job.UpdatedAt = DateTime.UtcNow;

        var obsoleteMappings = job.JobSkills
            .Where(mapping => !skillIds.Contains(mapping.SkillId))
            .ToList();

        foreach (var mapping in obsoleteMappings)
        {
            job.JobSkills.Remove(mapping);
        }

        var existingSkillIds = job.JobSkills
            .Select(mapping => mapping.SkillId)
            .ToHashSet();

        foreach (var skill in skills.Where(
                     skill => !existingSkillIds.Contains(skill.SkillId)))
        {
            job.JobSkills.Add(new JobSkill
            {
                Skill = skill
            });
        }

        await _dbContext.SaveChangesAsync();

        return Ok(ToResponse(job));
    }

    [Authorize(Roles = nameof(UserRole.Employer))]
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<JobResponse>> UpdateStatus(
        int id,
        UpdateJobStatusRequest request)
    {
        var employerUserId = GetUserId();

        if (employerUserId is null)
        {
            return Unauthorized(new { message = "Invalid user token." });
        }

        if (!await IsActiveEmployer(employerUserId.Value))
        {
            return Unauthorized(new
            {
                message = "Employer account is unavailable."
            });
        }

        if (!Enum.IsDefined(request.Status))
        {
            return BadRequest(new { message = "Invalid job status." });
        }

        var company = await _dbContext.Companies
            .SingleOrDefaultAsync(company =>
                company.EmployerUserId == employerUserId.Value);

        if (company is null)
        {
            return NotFound(new { message = "Company was not found." });
        }

        var job = await _dbContext.Jobs
            .Include(job => job.Company)
            .Include(job => job.JobSkills)
            .ThenInclude(jobSkill => jobSkill.Skill)
            .SingleOrDefaultAsync(job =>
                job.JobId == id &&
                job.CompanyId == company.CompanyId);

        if (job is null)
        {
            return NotFound(new { message = "Job was not found." });
        }

        if (request.Status == JobStatus.Open)
        {
            if (company.Status != CompanyStatus.Active)
            {
                return BadRequest(new
                {
                    message =
                        "A job cannot be opened for an inactive company."
                });
            }

            if (job.ApplicationDeadline <= DateTime.UtcNow)
            {
                return BadRequest(new
                {
                    message =
                        "A job with an expired deadline cannot be opened."
                });
            }
        }

        job.Status = request.Status;
        job.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(ToResponse(job));
    }

    private async Task<ActionResult<JobResponse>?> ValidateRequest(
        UpsertJobRequest request)
    {
        if (request.ApplicationDeadline <= DateTime.UtcNow)
        {
            return BadRequest(new
            {
                message = "Application deadline must be in the future."
            });
        }

        if (request.SkillIds is null || request.SkillIds.Count == 0)
        {
            return BadRequest(new
            {
                message = "At least one skill is required."
            });
        }

        var skillIds = request.SkillIds.Distinct().ToList();

        if (skillIds.Any(skillId => skillId <= 0))
        {
            return BadRequest(new
            {
                message = "Skill IDs must be positive."
            });
        }

        var existingSkillIds = await _dbContext.Skills
            .Where(skill => skillIds.Contains(skill.SkillId))
            .Select(skill => skill.SkillId)
            .ToListAsync();

        if (existingSkillIds.Count != skillIds.Count)
        {
            var missingSkillIds = skillIds
                .Except(existingSkillIds)
                .ToList();

            return BadRequest(new
            {
                message = "One or more skills do not exist.",
                missingSkillIds
            });
        }

        return null;
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

    private static JobResponse ToResponse(Job job)
    {
        return new JobResponse
        {
            JobId = job.JobId,
            CompanyId = job.CompanyId,
            CompanyName = job.Company.Name,
            Title = job.Title,
            Description = job.Description,
            Location = job.Location,
            JobType = job.JobType,
            ExperienceRequiredYears =
                job.ExperienceRequiredYears,
            SalaryRange = job.SalaryRange,
            ApplicationDeadline = job.ApplicationDeadline,
            Status = job.Status,
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt,
            Skills = job.JobSkills
                .OrderBy(mapping => mapping.Skill.Name)
                .Select(mapping => new JobSkillResponse
                {
                    SkillId = mapping.SkillId,
                    Name = mapping.Skill.Name
                })
                .ToList()
        };
    }
}