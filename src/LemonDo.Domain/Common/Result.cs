namespace LemonDo.Domain.Common;

/// <summary>
/// Discriminated union representing either a success with a value or a failure with an error.
/// Access <see cref="Value"/> only when <see cref="IsSuccess"/> is true; access <see cref="Error"/>
/// only when <see cref="IsFailure"/> is true. Violating this throws <see cref="InvalidOperationException"/>.
/// </summary>
/// <typeparam name="TValue">The type of the success value.</typeparam>
/// <typeparam name="TError">The type of the failure error (typically <see cref="DomainError"/>).</typeparam>
/// <example>
/// <code>
/// Result&lt;TaskTitle, DomainError&gt; result = TaskTitle.Create("Buy groceries");
/// if (result.IsSuccess)
///     Console.WriteLine(result.Value);  // "Buy groceries"
/// else
///     Console.WriteLine(result.Error.Message);
/// </code>
/// </example>
public sealed class Result<TValue, TError>
{
    private readonly TValue? _value;
    private readonly TError? _error;

    /// <summary>Indicates whether the result represents a success.</summary>
    public bool IsSuccess { get; }

    /// <summary>Indicates whether the result represents a failure.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// The success value. Only valid when <see cref="IsSuccess"/> is <c>true</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Accessed on a failed result.</exception>
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed Result.");

    /// <summary>
    /// The failure error. Only valid when <see cref="IsFailure"/> is <c>true</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Accessed on a successful result.</exception>
    public TError Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access Error on a successful Result.");

    private Result(TValue value)
    {
        IsSuccess = true;
        _value = value;
        _error = default;
    }

    private Result(TError error, bool _)
    {
        IsSuccess = false;
        _value = default;
        _error = error;
    }

    /// <summary>Creates a successful result wrapping <paramref name="value"/>.</summary>
    public static Result<TValue, TError> Success(TValue value) => new(value);

    /// <summary>Creates a failed result wrapping <paramref name="error"/>.</summary>
    public static Result<TValue, TError> Failure(TError error) => new(error, false);

    /// <summary>
    /// Transforms the success value using <paramref name="map"/>, preserving failures unchanged.
    /// </summary>
    public Result<TOut, TError> Map<TOut>(Func<TValue, TOut> map) =>
        IsSuccess
            ? Result<TOut, TError>.Success(map(_value!))
            : Result<TOut, TError>.Failure(_error!);
}

/// <summary>
/// Unit result representing success or failure without a value.
/// Used for operations that either succeed (no return) or fail with an error.
/// </summary>
/// <typeparam name="TError">The type of the failure error (typically <see cref="DomainError"/>).</typeparam>
public sealed class Result<TError>
{
    private readonly TError? _error;

    /// <summary>Indicates whether the result represents a success.</summary>
    public bool IsSuccess { get; }

    /// <summary>Indicates whether the result represents a failure.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// The failure error. Only valid when <see cref="IsFailure"/> is <c>true</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Accessed on a successful result.</exception>
    public TError Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access Error on a successful Result.");

    private Result()
    {
        IsSuccess = true;
        _error = default;
    }

    private Result(TError error)
    {
        IsSuccess = false;
        _error = error;
    }

    /// <summary>Creates a successful unit result.</summary>
    public static Result<TError> Success() => new();

    /// <summary>Creates a failed unit result wrapping <paramref name="error"/>.</summary>
    public static Result<TError> Failure(TError error) => new(error);
}
