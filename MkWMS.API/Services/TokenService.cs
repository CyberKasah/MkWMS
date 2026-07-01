using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MkWMS.Data.Context;
using MkWMS.Data.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MkWMS.API.Services;

public class TokenService : ITokenService
{
    private readonly MkWMSDbContext _context;
    private readonly IConfiguration _config;

    public TokenService(MkWMSDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public string GenerateAccessToken(User user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim("warehouseId", user.WarehouseId?.ToString() ?? "")
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var jwtKey = _config["Jwt:Key"] ?? throw new InvalidOperationException(
            "Jwt:Key не настроен. Задайте секрет через переменную окружения или dotnet user-secrets, не храните его в appsettings.json.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expireMinutes = Convert.ToDouble(_config["Jwt:ExpireMinutes"] ?? "30");

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateOpaqueToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private int RefreshTokenDays => Convert.ToInt32(_config["Jwt:RefreshTokenDays"] ?? "14");

    public async Task<(string AccessToken, string RefreshToken)> IssueTokenPairAsync(User user)
    {
        var roles = await _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

        var accessToken = GenerateAccessToken(user, roles);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = GenerateOpaqueToken(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays)
        };
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return (accessToken, refreshToken.Token);
    }

    public async Task<(bool Success, string? AccessToken, string? RefreshToken, string? Error)> RotateAsync(string refreshToken)
    {
        var existing = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (existing == null)
            return (false, null, null, "Сессия не найдена, требуется повторный вход");

        if (!existing.IsActive)
            return (false, null, null, "Сессия истекла или была отозвана, требуется повторный вход");

        if (!existing.User.IsActive)
            return (false, null, null, "Учётная запись деактивирована");

        var (accessToken, newRefreshToken) = await IssueTokenPairAsync(existing.User);

        existing.RevokedAt = DateTime.UtcNow;
        existing.ReplacedByToken = newRefreshToken;
        await _context.SaveChangesAsync();

        return (true, accessToken, newRefreshToken, null);
    }

    public async Task RevokeAsync(string refreshToken)
    {
        var existing = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
        if (existing != null && existing.RevokedAt == null)
        {
            existing.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RevokeAllForUserAsync(int userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var t in tokens)
            t.RevokedAt = DateTime.UtcNow;

        if (tokens.Count > 0)
            await _context.SaveChangesAsync();
    }
}
