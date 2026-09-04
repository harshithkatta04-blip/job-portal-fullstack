using JobPortal.Api.Models.Enums;

namespace JobPortal.Api.Dtos.Jobs;

public class JobResponse
{
    public int JobId { get; set; }

    public int CompanyId { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string JobType { get; set; } = string.Empty;

    public int ExperienceRequiredYears { get; set; }

    public string? SalaryRange { get; set; }

    public DateTime ApplicationDeadline { get; set; }

    public JobStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<JobSkillResponse> Skills { get; set; } = [];
}