namespace GtKram.Domain.Base;

public readonly struct Result<T>
{
    private readonly Error[]? _errors;
    private readonly T _value;

    public bool IsFailed => _errors?.Length > 0;
    public bool IsSuccess => !IsFailed;
    public Error[] Errors => _errors ?? [];
    public T Value => IsSuccess ? _value : throw new InvalidOperationException("Value is not available");

    internal Result(T value, Error[]? errors = default)
    {
        _value = value;
        _errors = errors;
    }

    internal Result(Result result)
    {
        _value = default!;
        _errors = result.Errors;
    }

    internal Result(Result<T> result)
    {
        _value = result.Value;
        _errors = result.Errors;
    }

    public Result ToResult() => IsSuccess ? Result.Ok() : Result.Fail(_errors!);

    public static implicit operator Result<T>(T value) => value is Result<T> result ? result : new(value);
    public static implicit operator Result<T>(Result result) => new(result);
    public static implicit operator Result(Result<T> result) => result.ToResult();
}
