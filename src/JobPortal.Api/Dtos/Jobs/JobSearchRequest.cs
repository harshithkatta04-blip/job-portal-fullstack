using System.ComponentModel.DataAnnotations;
using JobPortal.Api.Models.Enums;
namespace JobPortal.Api.Dtos.Jobs;

public class JobSearchRequest
{
    [MaxLength(150)]
    public string? Title { get; set; }

    [MaxLength(150)]
    public string? Location { get; set; }

    public JobType? JobType { get; set; }

    [Range(0, 100)]
    public int? ExperienceYears { get; set; }

    [Range(1, 10000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}