using System.ComponentModel.DataAnnotations;

namespace JobPortal.Api.Dtos.Companies;

public class UpdateCompanyRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Url]
    [MaxLength(500)]
    public string? Website { get; set; }

    [Required]
    [MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;
}