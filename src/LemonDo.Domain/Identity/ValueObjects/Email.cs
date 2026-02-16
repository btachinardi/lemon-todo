namespace LemonDo.Domain.Identity.ValueObjects;

using System.Text.RegularExpressions;
using LemonDo.Domain.Common;

/// <summary>
/// Strongly-typed email address with format validation.
/// Maximum length is 254 characters per RFC 5321.
/// </summary>
public sealed partial class Email : ValueObject<string>, IReconstructable<Email, string>, ISensitivePii
{
    /// <summary>Maximum allowed length for an email address (RFC 5321).</summary>
    public const int MaxLength = 254;

    private Email(string value) : base(value) { }

    /// <inheritdoc />
    public string Redacted
    {
        get
        {
            var atIndex = Value.IndexOf('@');
            if (atIndex <= 1) return "***@***";
            return $"{Value[0]}***@{Value[(atIndex + 1)..]}";
        }
    }

    /// <summary>Creates an <see cref="Email"/> after validating format and length.</summary>
    public static Result<Email, DomainError> Create(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Result<Email, DomainError>.Failure(
                DomainError.Validation("email", "Email is required."));

        var trimmed = email.Trim();

        if (trimmed.Length > MaxLength)
            return Result<Email, DomainError>.Failure(
                DomainError.Validation("email", $"Email must not exceed {MaxLength} characters."));

        if (!EmailRegex().IsMatch(trimmed))
            return Result<Email, DomainError>.Failure(
                DomainError.Validation("email", "Email format is invalid."));

        return Result<Email, DomainError>.Success(new Email(trimmed.ToLowerInvariant()));
    }

    /// <summary>Reconstructs an <see cref="Email"/> from a persistence value.</summary>
    public static Email Reconstruct(string value) => new(value);

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
}
