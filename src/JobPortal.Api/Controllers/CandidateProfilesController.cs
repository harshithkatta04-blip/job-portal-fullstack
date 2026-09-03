using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using JobPortal.Api.Data;
using JobPortal.Api.Dtos.CandidateProfiles;
using JobPortal.Api.Models.Entities;
using JobPortal.Api.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Candidate))]
[Route("api/candidate-profile")]
public class CandidateProfilesController : ControllerBase
{
    private readonly JobPortalDbContext _dbContext;

    public CandidateProfilesController(JobPortalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("me")]
    public async Task<ActionResult<CandidateProfileResponse>> GetMe()
    {
        var userId = GetUserId();

        if (userId is null)
        {
            return Unauthorized(new { message = "Invalid user token." });
        }

        if (!await IsActiveCandidate(userId.Value))
        {
            return Unauthorized(new { message = "Candidate account is unavailable." });
        }

        var profile = await _dbContext.CandidateProfiles
            .AsNoTracking()
            .Include(profile => profile.CandidateProfileSkills)
            .ThenInclude(profileSkill => profileSkill.Skill)
            .SingleOrDefaultAsync(profile => profile.UserId == userId.Value);

        if (profile is null)
        {
            return NotFound(new { message = "Candidate profile was not found." });
        }

        return Ok(ToResponse(profile));
    }

    [HttpPut("me")]
    public async Task<ActionResult<CandidateProfileResponse>> UpsertMe(
        UpsertCandidateProfileRequest request)
    {
        var userId = GetUserId();

        if (userId is null)
        {
            return Unauthorized(new { message = "Invalid user token." });
        }

        if (!await IsActiveCandidate(userId.Value))
        {
            return Unauthorized(new { message = "Candidate account is unavailable." });
        }

        if (request.SkillIds is null || request.SkillIds.Count == 0)
        {
            return BadRequest(new { message = "At least one skill is required." });
        }

        var skillIds = request.SkillIds
            .Distinct()
            .ToList();

        if (skillIds.Any(skillId => skillId <= 0))
        {
            return BadRequest(new { message = "Skill IDs must be positive." });
        }

        var skills = await _dbContext.Skills
            .Where(skill => skillIds.Contains(skill.SkillId))
            .ToListAsync();

        if (skills.Count != skillIds.Count)
        {
            var existingSkillIds = skills
                .Select(skill => skill.SkillId);

            var missingSkillIds = skillIds
                .Except(existingSkillIds)
                .ToList();

            return BadRequest(new
            {
                message = "One or more skills do not exist.",
                missingSkillIds
            });
        }

        var profile = await _dbContext.CandidateProfiles
            .Include(profile => profile.CandidateProfileSkills)
            .ThenInclude(profileSkill => profileSkill.Skill)
            .SingleOrDefaultAsync(profile => profile.UserId == userId.Value);

        if (profile is null)
        {
            profile = new CandidateProfile
            {
                UserId = userId.Value
            };

            _dbContext.CandidateProfiles.Add(profile);
        }

        profile.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? null
            : request.PhoneNumber.Trim();

        profile.Location = request.Location.Trim();
        profile.Education = request.Education.Trim();
        profile.ExperienceYears = request.ExperienceYears;
        profile.ResumeUrl = request.ResumeUrl.Trim();
        profile.UpdatedAt = DateTime.UtcNow;

        var obsoleteMappings = profile.CandidateProfileSkills
            .Where(mapping => !skillIds.Contains(mapping.SkillId))
            .ToList();

        foreach (var mapping in obsoleteMappings)
        {
            profile.CandidateProfileSkills.Remove(mapping);
        }

        var existingIds = profile.CandidateProfileSkills
            .Select(mapping => mapping.SkillId)
            .ToHashSet();

        foreach (var skill in skills.Where(
                     skill => !existingIds.Contains(skill.SkillId)))
        {
            profile.CandidateProfileSkills.Add(new CandidateProfileSkill
            {
                Skill = skill
            });
        }

        await _dbContext.SaveChangesAsync();

        return Ok(ToResponse(profile));
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

    private static CandidateProfileResponse ToResponse(
        CandidateProfile profile)
    {
        return new CandidateProfileResponse
        {
            CandidateProfileId = profile.CandidateProfileId,
            UserId = profile.UserId,
            PhoneNumber = profile.PhoneNumber,
            Location = profile.Location,
            Education = profile.Education,
            ExperienceYears = profile.ExperienceYears,
            ResumeUrl = profile.ResumeUrl,
            UpdatedAt = profile.UpdatedAt,
            Skills = profile.CandidateProfileSkills
                .OrderBy(mapping => mapping.Skill.Name)
                .Select(mapping => new SkillResponse
                {
                    SkillId = mapping.SkillId,
                    Name = mapping.Skill.Name
                })
                .ToList()
        };
    }
}