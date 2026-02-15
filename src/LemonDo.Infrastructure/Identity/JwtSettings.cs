namespace LemonDo.Infrastructure.Identity;

using Microsoft.Extensions.Options;

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

/// <summary>Validates <see cref="JwtSettings"/> at startup to fail fast on misconfiguration.</summary>
public sealed class JwtSettingsValidator : IValidateOptions<JwtSettings>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, JwtSettings options)
    {
        if (string.IsNullOrWhiteSpace(options.SecretKey))
            return ValidateOptionsResult.Fail("Jwt:SecretKey must be configured.");

        if (options.SecretKey.Length < 32)
            return ValidateOptionsResult.Fail("Jwt:SecretKey must be at least 32 characters.");

        return ValidateOptionsResult.Success;
    }
}
