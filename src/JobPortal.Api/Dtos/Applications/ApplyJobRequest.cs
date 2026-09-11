using System.ComponentModel.DataAnnotations;

namespace JobPortal.Api.Dtos.Applications;

public class ApplyJobRequest
{
    [MaxLength(2000)]
    public string? CoverLetter { get; set; }
}