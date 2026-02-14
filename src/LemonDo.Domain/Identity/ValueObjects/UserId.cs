namespace LemonDo.Domain.Identity.ValueObjects;

using LemonDo.Domain.Common;

/// <summary>
/// Strongly-typed identifier for a User.
/// </summary>
public sealed class UserId : ValueObject
{
    /// <summary>
    /// Default user ID for single-user mode (CP1, no auth).
    /// </summary>
    public static readonly UserId Default = new(new Guid("00000000-0000-0000-0000-000000000001"));

    public Guid Value { get; }

    public UserId(Guid value)
    {
        Value = value;
    }

    public static UserId New() => new(Guid.NewGuid());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
