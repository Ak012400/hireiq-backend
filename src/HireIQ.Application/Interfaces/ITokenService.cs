using HireIQ.Domain.Entities;

namespace HireIQ.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
