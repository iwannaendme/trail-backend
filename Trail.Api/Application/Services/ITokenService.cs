using Trail.Api.Domain.Entities;

namespace Trail.Api.Application.Services;

public interface ITokenService
{
    string GenerateToken(User user);
}
