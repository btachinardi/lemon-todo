namespace LemonDo.Domain.Identity.ValueObjects;

using LemonDo.Domain.Common;

/// <summary>
/// User display name value object. Must be 2-100 characters after trimming.
/// </summary>
public sealed class DisplayName : ValueObject<string>, IReconstructable<DisplayName, string>
{
    /// <summary>Minimum allowed length.</summary>
    public const int MinLength = 2;

    /// <summary>Maximum allowed length.</summary>
    public const int MaxLength = 100;

    private DisplayName(string value) : base(value) { }

    /// <summary>Creates a <see cref="DisplayName"/> after validating length constraints.</summary>
    public static Result<DisplayName, DomainError> Create(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return Result<DisplayName, DomainError>.Failure(
                DomainError.Validation("displayName", "Display name is required."));

        var trimmed = displayName.Trim();

        if (trimmed.Length < MinLength)
            return Result<DisplayName, DomainError>.Failure(
                DomainError.Validation("displayName", $"Display name must be at least {MinLength} characters."));

        if (trimmed.Length > MaxLength)
            return Result<DisplayName, DomainError>.Failure(
                DomainError.Validation("displayName", $"Display name must not exceed {MaxLength} characters."));

        return Result<DisplayName, DomainError>.Success(new DisplayName(trimmed));
    }

    /// <summary>Reconstructs a <see cref="DisplayName"/> from a persistence value.</summary>
    public static DisplayName Reconstruct(string value) => new(value);
}
