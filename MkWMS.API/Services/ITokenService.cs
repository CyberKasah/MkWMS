using MkWMS.Data.Entities;

namespace MkWMS.API.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user, IList<string> roles);

    Task<(string AccessToken, string RefreshToken)> IssueTokenPairAsync(User user);

    Task<(bool Success, string? AccessToken, string? RefreshToken, string? Error)> RotateAsync(string refreshToken);

    Task RevokeAsync(string refreshToken);

    Task RevokeAllForUserAsync(int userId);
}
