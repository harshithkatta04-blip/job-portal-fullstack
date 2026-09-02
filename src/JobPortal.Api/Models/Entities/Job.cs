using JobPortal.Api.Models.Enums;

namespace JobPortal.Api.Models.Entities;

public class Job
{
    public int JobId { get; set; }

    public int CompanyId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string JobType { get; set; } = string.Empty;

    public int ExperienceRequiredYears { get; set; }

    public string? SalaryRange { get; set; }

    public DateTime ApplicationDeadline { get; set; }

    public JobStatus Status { get; set; } = JobStatus.Open;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<JobSkill> JobSkills { get; set; }
         = new List<JobSkill>();

    public Company Company { get; set; } = null!;
}