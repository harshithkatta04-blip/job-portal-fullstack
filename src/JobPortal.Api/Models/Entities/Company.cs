using JobPortal.Api.Models.Enums;

namespace JobPortal.Api.Models.Entities;

public class Company
{
    public int CompanyId { get; set; }

    public int EmployerUserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Website { get; set; }

    public string Location { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public CompanyStatus Status { get; set; } = CompanyStatus.Active;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User EmployerUser { get; set; } = null!;
}