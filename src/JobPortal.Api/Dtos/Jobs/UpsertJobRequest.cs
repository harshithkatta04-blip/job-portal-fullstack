using System.ComponentModel.DataAnnotations;

namespace JobPortal.Api.Dtos.Jobs;

public class UpsertJobRequest
{
    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string JobType { get; set; } = string.Empty;

    [Range(0, 100)]
    public int ExperienceRequiredYears { get; set; }

    [MaxLength(50)]
    public string? SalaryRange { get; set; }

    public DateTime ApplicationDeadline { get; set; }

    [Required]
    [MinLength(1)]
    public List<int> SkillIds { get; set; } = [];
}