namespace LemonDo.Infrastructure.Identity;

/// <summary>Persisted refresh token for JWT authentication.</summary>
public sealed class RefreshToken
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>The user who owns this refresh token.</summary>
    public Guid UserId { get; set; }

    /// <summary>SHA-256 hash of the token value (never store plaintext).</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>When this token expires.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>When this token was revoked (null if still valid).</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>When this token was created.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Whether this token is still usable.</summary>
    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;
}
