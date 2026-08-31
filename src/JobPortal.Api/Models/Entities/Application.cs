using JobPortal.Api.Models.Enums;

namespace JobPortal.Api.Models.Entities;

public class Application
{
    public int ApplicationId { get; set; }

    public int JobId { get; set; }

    public int CandidateUserId { get; set; }

    public string ResumeUrl { get; set; } = string.Empty;

    public string? CoverLetter { get; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Job Job { get; set; } = null!;

    public User CandidateUser { get; set; } = null!;
}