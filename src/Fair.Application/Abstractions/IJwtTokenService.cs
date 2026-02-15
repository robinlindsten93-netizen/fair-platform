namespace Fair.Application.Abstractions;

public interface IJwtTokenService
{
    string CreateToken(Guid userId, string role);
}
