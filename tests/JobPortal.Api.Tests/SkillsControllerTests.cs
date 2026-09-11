using JobPortal.Api.Controllers;
using JobPortal.Api.Data;
using JobPortal.Api.Dtos.Skills;
using JobPortal.Api.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Api.Tests;

public class SkillsControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsSkillsAlphabetically()
    {
        await using var dbContext = CreateDbContext();

        dbContext.Skills.AddRange(
            new Skill
            {
                SkillId = 1,
                Name = "React"
            },
            new Skill
            {
                SkillId = 2,
                Name = "C#"
            },
            new Skill
            {
                SkillId = 3,
                Name = "PostgreSQL"
            });

        await dbContext.SaveChangesAsync();

        var controller = new SkillsController(dbContext);

        var result = await controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);

        var response = Assert.IsType<List<SkillResponse>>(
            okResult.Value);

        Assert.Equal(3, response.Count);
        Assert.Equal(
            ["C#", "PostgreSQL", "React"],
            response.Select(skill => skill.Name));
    }

    private static JobPortalDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<JobPortalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new JobPortalDbContext(options);
    }
}