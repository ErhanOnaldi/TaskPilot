namespace TaskPilot.Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public bool IsRevoked => RevokedAtUtc is not null;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsActive => !IsRevoked && !IsExpired;

    public bool IsExpiredAt(DateTime utcNow)
    {
        return utcNow >= ExpiresAtUtc;
    }

    public bool IsActiveAt(DateTime utcNow)
    {
        return !IsRevoked && !IsExpiredAt(utcNow);
    }

    public User? User { get; set; }
}
