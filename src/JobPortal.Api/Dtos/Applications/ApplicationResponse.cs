using JobPortal.Api.Models.Enums;

namespace JobPortal.Api.Dtos.Applications;

public class ApplicationResponse
{
    public int ApplicationId { get; set; }

    public int JobId { get; set; }

    public string JobTitle { get; set; } = string.Empty;

    public JobStatus JobStatus { get; set; }

    public int CompanyId { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public int CandidateUserId { get; set; }

    public string CandidateName { get; set; } = string.Empty;

    public string CandidateEmail { get; set; } = string.Empty;

    public string ResumeUrl { get; set; } = string.Empty;

    public string? CoverLetter { get; set; }

    public ApplicationStatus Status { get; set; }

    public DateTime AppliedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}