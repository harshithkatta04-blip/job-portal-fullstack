using JobPortal.Api.Models.Entities;

namespace JobPortal.Api.Services.Auth;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(User user);
}