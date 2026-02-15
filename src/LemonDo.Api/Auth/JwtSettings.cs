namespace LemonDo.Api.Auth;

/// <summary>JWT configuration bound from appsettings.</summary>
public sealed class JwtSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Token issuer (iss claim).</summary>
    public string Issuer { get; set; } = "LemonDo";

    /// <summary>Token audience (aud claim).</summary>
    public string Audience { get; set; } = "LemonDo";

    /// <summary>HMAC-SHA256 secret key (min 32 chars).</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Access token lifetime in minutes.</summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    /// <summary>Refresh token lifetime in days.</summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
