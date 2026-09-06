using JobPortal.Api.Data;
using JobPortal.Api.Dtos.Skills;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Api.Controllers;

[ApiController]
[Route("api/skills")]
public class SkillsController : ControllerBase
{
    private readonly JobPortalDbContext _dbContext;

    public SkillsController(JobPortalDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<SkillResponse>>> GetAll()
    {
        var skills = await _dbContext.Skills
            .AsNoTracking()
            .OrderBy(skill => skill.Name)
            .Select(skill => new SkillResponse
            {
                SkillId = skill.SkillId,
                Name = skill.Name
            })
            .ToListAsync();

        return Ok(skills);
    }
}