using System.ComponentModel.DataAnnotations;

namespace JobPortal.Api.Dtos.Jobs;

public class JobSearchRequest
{
    [MaxLength(150)]
    public string? Title { get; set; }

    [MaxLength(150)]
    public string? Location { get; set; }

    [MaxLength(50)]
    public string? JobType { get; set; }

    [Range(0, 100)]
    public int? ExperienceYears { get; set; }

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}