using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using JobPortal.Api.Data;
using JobPortal.Api.Dtos.Applications;
using JobPortal.Api.Models.Entities;
using JobPortal.Api.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Api.Controllers;

[ApiController]
[Route("api/applications")]
public class ApplicationsController : ControllerBase
{
    private readonly JobPortalDbContext _dbContext;

    public ApplicationsController(JobPortalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Authorize(Roles = nameof(UserRole.Candidate))]
    [HttpPost("~/api/jobs/{jobId:int}/apply")]
    public async Task<ActionResult<ApplicationResponse>> Apply(
        int jobId,
        ApplyJobRequest request)
    {
        var candidateUserId = GetUserId();

        if (candidateUserId is null)
        {
            return Unauthorized(new { message = "Invalid user token." });
        }

        var candidate = await _dbContext.Users
            .SingleOrDefaultAsync(user =>
                user.UserId == candidateUserId.Value &&
                user.Role == UserRole.Candidate &&
                user.IsActive);

        if (candidate is null)
        {
            return Unauthorized(new
            {
                message = "Candidate account is unavailable."
            });
        }

        var profile = await _dbContext.CandidateProfiles
            .Include(profile => profile.CandidateProfileSkills)
            .SingleOrDefaultAsync(profile =>
                profile.UserId == candidateUserId.Value);

        if (profile is null ||
            string.IsNullOrWhiteSpace(profile.Location) ||
            string.IsNullOrWhiteSpace(profile.Education) ||
            string.IsNullOrWhiteSpace(profile.ResumeUrl) ||
            profile.CandidateProfileSkills.Count == 0)
        {
            return BadRequest(new
            {
                message =
                    "Complete your Candidate profile before applying."
            });
        }

        var job = await _dbContext.Jobs
            .Include(job => job.Company)
            .SingleOrDefaultAsync(job => job.JobId == jobId);

        if (job is null)
        {
            return NotFound(new { message = "Job was not found." });
        }

        if (job.Status != JobStatus.Open ||
            job.ApplicationDeadline <= DateTime.UtcNow ||
            job.Company.Status != CompanyStatus.Active)
        {
            return BadRequest(new
            {
                message = "This Job is not accepting applications."
            });
        }

        var alreadyApplied = await _dbContext.Applications
            .AnyAsync(application =>
                application.JobId == jobId &&
                application.CandidateUserId ==
                    candidateUserId.Value);

        if (alreadyApplied)
        {
            return Conflict(new
            {
                message = "You have already applied for this Job."
            });
        }

        var now = DateTime.UtcNow;

        var application = new Application
        {
            JobId = job.JobId,
            Job = job,
            CandidateUserId = candidate.UserId,
            CandidateUser = candidate,
            ResumeUrl = profile.ResumeUrl,
            CoverLetter = string.IsNullOrWhiteSpace(
                request.CoverLetter)
                ? null
                : request.CoverLetter.Trim(),
            Status = ApplicationStatus.Applied,
            AppliedAt = now,
            UpdatedAt = now
        };

        _dbContext.Applications.Add(application);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetMine),
            null,
            ToResponse(application));
    }

    [Authorize(Roles = nameof(UserRole.Candidate))]
    [HttpGet("me")]
    public async Task<ActionResult<List<ApplicationResponse>>> GetMine()
    {
        var candidateUserId = GetUserId();

        if (candidateUserId is null)
        {
            return Unauthorized(new { message = "Invalid user token." });
        }

        if (!await IsActiveCandidate(candidateUserId.Value))
        {
            return Unauthorized(new
            {
                message = "Candidate account is unavailable."
            });
        }

        var applications = await _dbContext.Applications
            .AsNoTracking()
            .Include(application => application.Job)
            .ThenInclude(job => job.Company)
            .Include(application => application.CandidateUser)
            .Where(application =>
                application.CandidateUserId ==
                    candidateUserId.Value)
            .OrderByDescending(application =>
                application.AppliedAt)
            .ToListAsync();

        return Ok(applications
            .Select(ToResponse)
            .ToList());
    }

    [Authorize(Roles = nameof(UserRole.Employer))]
    [HttpGet("~/api/jobs/{jobId:int}/applications")]
    public async Task<ActionResult<List<ApplicationResponse>>>
        GetForJob(int jobId)
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

        var ownsJob = await _dbContext.Jobs
            .AnyAsync(job =>
                job.JobId == jobId &&
                job.Company.EmployerUserId ==
                    employerUserId.Value);

        if (!ownsJob)
        {
            return NotFound(new { message = "Job was not found." });
        }

        var applications = await _dbContext.Applications
            .AsNoTracking()
            .Include(application => application.Job)
            .ThenInclude(job => job.Company)
            .Include(application => application.CandidateUser)
            .Where(application => application.JobId == jobId)
            .OrderByDescending(application =>
                application.AppliedAt)
            .ToListAsync();

        return Ok(applications
            .Select(ToResponse)
            .ToList());
    }

    [Authorize(Roles = nameof(UserRole.Employer))]
    [HttpPatch("{applicationId:int}/status")]
    public async Task<ActionResult<ApplicationResponse>> UpdateStatus(
        int applicationId,
        UpdateApplicationStatusRequest request)
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

        if (!Enum.IsDefined(request.Status) ||
            request.Status == ApplicationStatus.Withdrawn)
        {
            return BadRequest(new
            {
                message = "Invalid Employer application status."
            });
        }

        var application = await _dbContext.Applications
            .Include(application => application.Job)
            .ThenInclude(job => job.Company)
            .Include(application => application.CandidateUser)
            .SingleOrDefaultAsync(application =>
                application.ApplicationId == applicationId &&
                application.Job.Company.EmployerUserId ==
                    employerUserId.Value);

        if (application is null)
        {
            return NotFound(new
            {
                message = "Application was not found."
            });
        }

        if (application.Status == ApplicationStatus.Withdrawn)
        {
            return BadRequest(new
            {
                message =
                    "A withdrawn application cannot be updated."
            });
        }

        application.Status = request.Status;
        application.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(ToResponse(application));
    }

    [Authorize(Roles = nameof(UserRole.Candidate))]
    [HttpPatch("{applicationId:int}/withdraw")]
    public async Task<ActionResult<ApplicationResponse>> Withdraw(
        int applicationId)
    {
        var candidateUserId = GetUserId();

        if (candidateUserId is null)
        {
            return Unauthorized(new { message = "Invalid user token." });
        }

        if (!await IsActiveCandidate(candidateUserId.Value))
        {
            return Unauthorized(new
            {
                message = "Candidate account is unavailable."
            });
        }

        var application = await _dbContext.Applications
            .Include(application => application.Job)
            .ThenInclude(job => job.Company)
            .Include(application => application.CandidateUser)
            .SingleOrDefaultAsync(application =>
                application.ApplicationId == applicationId &&
                application.CandidateUserId ==
                    candidateUserId.Value);

        if (application is null)
        {
            return NotFound(new
            {
                message = "Application was not found."
            });
        }

        if (application.Status != ApplicationStatus.Applied &&
            application.Status != ApplicationStatus.UnderReview)
        {
            return BadRequest(new
            {
                message =
                    "This application can no longer be withdrawn."
            });
        }

        application.Status = ApplicationStatus.Withdrawn;
        application.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(ToResponse(application));
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

    private Task<bool> IsActiveCandidate(int userId)
    {
        return _dbContext.Users.AnyAsync(user =>
            user.UserId == userId &&
            user.Role == UserRole.Candidate &&
            user.IsActive);
    }

    private Task<bool> IsActiveEmployer(int userId)
    {
        return _dbContext.Users.AnyAsync(user =>
            user.UserId == userId &&
            user.Role == UserRole.Employer &&
            user.IsActive);
    }

    private static ApplicationResponse ToResponse(
        Application application)
    {
        return new ApplicationResponse
        {
            ApplicationId = application.ApplicationId,
            JobId = application.JobId,
            JobTitle = application.Job.Title,
            JobStatus = application.Job.Status,
            CompanyId = application.Job.CompanyId,
            CompanyName = application.Job.Company.Name,
            CandidateUserId = application.CandidateUserId,
            CandidateName = application.CandidateUser.FullName,
            CandidateEmail = application.CandidateUser.Email,
            ResumeUrl = application.ResumeUrl,
            CoverLetter = application.CoverLetter,
            Status = application.Status,
            AppliedAt = application.AppliedAt,
            UpdatedAt = application.UpdatedAt
        };
    }
}