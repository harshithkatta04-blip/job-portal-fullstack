using System.ComponentModel.DataAnnotations;
using JobPortal.Api.Models.Enums;

namespace JobPortal.Api.Dtos.Auth;

public class RegisterRequest
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    [EnumDataType(typeof(UserRole))]
    public UserRole Role { get; set; }

    // Required only when Role is Employer
    [MaxLength(150)]
    public string? CompanyName { get; set; }

    [MaxLength(150)]
    public string? CompanyLocation { get; set; }

    [MaxLength(2000)]
    public string? CompanyDescription { get; set; }

    [Url]
    [MaxLength(500)]
    public string? CompanyWebsite { get; set; }
}