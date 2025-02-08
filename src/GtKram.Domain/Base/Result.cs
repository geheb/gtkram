namespace GtKram.Domain.Base;

public readonly struct Result
{
    private readonly Error[]? _errors;

    public bool IsFailed => _errors?.Length > 0;
    public bool IsSuccess => !IsFailed;
    public Error[] Errors => _errors ?? [];

    private Result(Error[]? errors = default) => _errors = errors;

    public static Result Ok() => new();

    public static Result<T> Ok<T>(T value) => new(value);

    public static Result Fail(Error error) => new([error]);

    public static Result Fail(Error[] errors) => new(errors);

    public static Result Fail(string code, string error) => new([new(code, error)]);

    public static Result Fail(IEnumerable<(string code, string error)> errors) => new([.. errors.Select(f => new Error(f.code, f.error))]);

    public static Result<T> Fail<T>(Error error) => new(default!, [error]);
}