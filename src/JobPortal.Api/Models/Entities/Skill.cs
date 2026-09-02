namespace JobPortal.Api.Models.Entities;

public class Skill
{
    public int SkillId { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<CandidateProfileSkill> CandidateProfileSkills { get; set; }
        = new List<CandidateProfileSkill>();
    public ICollection<JobSkill> JobSkills { get; set; }
    = new List<JobSkill>();
}