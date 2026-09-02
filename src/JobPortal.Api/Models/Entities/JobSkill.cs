namespace JobPortal.Api.Models.Entities;

public class JobSkill
{
    public int JobId { get; set; }

    public int SkillId { get; set; }

    public Job Job { get; set; } = null!;

    public Skill Skill { get; set; } = null!;
}