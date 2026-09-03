using System.Security.Claims;
using JobPortal.Api.Controllers;
using JobPortal.Api.Data;
using JobPortal.Api.Dtos.CandidateProfiles;
using JobPortal.Api.Models.Entities;
using JobPortal.Api.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Api.Tests;

public class CandidateProfilesControllerTests
{
    [Fact]
    public async Task UpsertMe_NewProfile_CreatesProfileWithSkills()
    {
        await using var dbContext = CreateDbContext();

        dbContext.Users.Add(CreateCandidate());
        dbContext.Skills.AddRange(
            new Skill { SkillId = 1, Name = "C#" },
            new Skill { SkillId = 2, Name = "React" });

        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext, userId: 1);

        var request = new UpsertCandidateProfileRequest
        {
            PhoneNumber = "+919876543210",
            Location = " Hyderabad ",
            Education = " B.Tech Computer Science ",
            ExperienceYears = 1,
            ResumeUrl = " https://example.com/resume.pdf ",
            SkillIds = [1, 2, 2]
        };

        var result = await controller.UpsertMe(request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CandidateProfileResponse>(
            okResult.Value);

        Assert.Equal(1, response.UserId);
        Assert.Equal("Hyderabad", response.Location);
        Assert.Equal("B.Tech Computer Science", response.Education);
        Assert.Equal(2, response.Skills.Count);

        var savedProfile = await dbContext.CandidateProfiles
            .Include(profile => profile.CandidateProfileSkills)
            .SingleAsync();

        Assert.Equal(1, savedProfile.UserId);
        Assert.Equal(2, savedProfile.CandidateProfileSkills.Count);
    }

    [Fact]
    public async Task UpsertMe_ExistingProfile_UpdatesProfileAndSkills()
    {
        await using var dbContext = CreateDbContext();

        var candidate = CreateCandidate();

        var csharp = new Skill
        {
            SkillId = 1,
            Name = "C#"
        };

        var react = new Skill
        {
            SkillId = 2,
            Name = "React"
        };

        var postgres = new Skill
        {
            SkillId = 3,
            Name = "PostgreSQL"
        };

        var profile = new CandidateProfile
        {
            CandidateProfileId = 1,
            UserId = candidate.UserId,
            Location = "Old Location",
            Education = "Old Education",
            ExperienceYears = 0,
            ResumeUrl = "https://example.com/old.pdf",
            CandidateProfileSkills =
            [
                new CandidateProfileSkill
                {
                    Skill = csharp
                },
                new CandidateProfileSkill
                {
                    Skill = react
                }
            ]
        };

        dbContext.Users.Add(candidate);
        dbContext.Skills.Add(postgres);
        dbContext.CandidateProfiles.Add(profile);

        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext, userId: 1);

        var request = new UpsertCandidateProfileRequest
        {
            PhoneNumber = "+919999999999",
            Location = "Bengaluru",
            Education = "B.Tech",
            ExperienceYears = 2,
            ResumeUrl = "https://example.com/new.pdf",
            SkillIds = [1, 3]
        };

        var result = await controller.UpsertMe(request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CandidateProfileResponse>(
            okResult.Value);

        Assert.Equal("Bengaluru", response.Location);
        Assert.Equal(2, response.ExperienceYears);
        Assert.Equal([1, 3], response.Skills
            .Select(skill => skill.SkillId)
            .OrderBy(skillId => skillId));

        var savedSkillIds = await dbContext.CandidateProfileSkills
            .Where(mapping => mapping.CandidateProfileId == 1)
            .Select(mapping => mapping.SkillId)
            .OrderBy(skillId => skillId)
            .ToListAsync();

        Assert.Equal([1, 3], savedSkillIds);
    }

    [Fact]
    public async Task UpsertMe_InvalidSkill_ReturnsBadRequestWithoutProfile()
    {
        await using var dbContext = CreateDbContext();

        dbContext.Users.Add(CreateCandidate());
        dbContext.Skills.Add(new Skill
        {
            SkillId = 1,
            Name = "C#"
        });

        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext, userId: 1);

        var request = new UpsertCandidateProfileRequest
        {
            Location = "Hyderabad",
            Education = "B.Tech",
            ExperienceYears = 1,
            ResumeUrl = "https://example.com/resume.pdf",
            SkillIds = [1, 9999]
        };

        var result = await controller.UpsertMe(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Empty(dbContext.CandidateProfiles);
        Assert.Empty(dbContext.CandidateProfileSkills);
    }

    private static User CreateCandidate()
    {
        return new User
        {
            UserId = 1,
            FullName = "Test Candidate",
            Email = "candidate@example.com",
            PasswordHash = "test-hash",
            Role = UserRole.Candidate,
            IsActive = true
        };
    }

    private static JobPortalDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<JobPortalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new JobPortalDbContext(options);
    }

    private static CandidateProfilesController CreateController(
        JobPortalDbContext dbContext,
        int userId)
    {
        var controller = new CandidateProfilesController(dbContext);

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
                            UserRole.Candidate.ToString())
                    ],
                    "UnitTest"))
            }
        };

        return controller;
    }
}