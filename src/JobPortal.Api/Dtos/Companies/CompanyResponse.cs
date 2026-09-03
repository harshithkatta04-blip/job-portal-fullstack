using JobPortal.Api.Models.Enums;

namespace JobPortal.Api.Dtos.Companies;

public class CompanyResponse
{
    public int CompanyId { get; set; }

    public int EmployerUserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Website { get; set; }

    public string Location { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public CompanyStatus Status { get; set; }

    public DateTime UpdatedAt { get; set; }
}