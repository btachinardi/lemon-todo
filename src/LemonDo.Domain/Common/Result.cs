namespace LemonDo.Domain.Common;

/// <summary>
/// Discriminated union representing either a success with a value or a failure with an error.
/// </summary>
public sealed class Result<TValue, TError>
{
    private readonly TValue? _value;
    private readonly TError? _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed Result.");

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

    public static Result<TValue, TError> Success(TValue value) => new(value);
    public static Result<TValue, TError> Failure(TError error) => new(error, false);

    public Result<TOut, TError> Map<TOut>(Func<TValue, TOut> map) =>
        IsSuccess
            ? Result<TOut, TError>.Success(map(_value!))
            : Result<TOut, TError>.Failure(_error!);
}

/// <summary>
/// Result without a value (unit result). Represents success or failure with an error.
/// </summary>
public sealed class Result<TError>
{
    private readonly TError? _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

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

    public static Result<TError> Success() => new();
    public static Result<TError> Failure(TError error) => new(error);
}
