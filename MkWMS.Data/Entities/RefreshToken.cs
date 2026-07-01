using System;

namespace MkWMS.Data.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }




    public string? ReplacedByToken { get; set; }

    public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
}
