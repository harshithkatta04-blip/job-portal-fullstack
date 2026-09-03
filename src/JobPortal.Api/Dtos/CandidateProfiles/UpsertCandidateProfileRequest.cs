using System.ComponentModel.DataAnnotations;

namespace JobPortal.Api.Dtos.CandidateProfiles;

public class UpsertCandidateProfileRequest
{
    [Phone]
    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [Required]
    [MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Education { get; set; } = string.Empty;

    [Range(0, 60)]
    public int ExperienceYears { get; set; }

    [Required]
    [Url]
    [MaxLength(1000)]
    public string ResumeUrl { get; set; } = string.Empty;

    [Required]
    public List<int> SkillIds { get; set; } = new();
}