namespace PIPDC.Application.Auth;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(string userId, string email, string fullName, IEnumerable<string> roles);
    string CreateRefreshToken();
}
