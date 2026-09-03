using JobPortal.Api.Models.Enums;

namespace JobPortal.Api.Dtos.Users;

public class UserResponse
{
    public int UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}