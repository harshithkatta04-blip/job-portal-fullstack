namespace JobPortal.Api.Models.Entities;

public class CandidateProfile
{
    public int CandidateProfileId { get; set; }

    public int UserId { get; set; }

    public string? PhoneNumber { get; set; }

    public string Location { get; set; } = string.Empty;

    public string Skills { get; set; } = string.Empty;

    public string Education { get; set; } = string.Empty;

    public int ExperienceYears { get; set; }

    public string ResumeUrl { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}