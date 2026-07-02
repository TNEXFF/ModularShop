namespace ModularShop.SharedKernel.Domain;

/// <summary>Outcome of an operation, avoiding exceptions for expected control flow.</summary>
public enum ResultStatus
{
    Ok,
    NotFound,
    Invalid,
    Error
}

/// <summary>
/// A lightweight result type (the pattern the Platform gets from Ardalis.Result, implemented
/// here in a few transparent lines). Application services return these; the web layer maps them
/// to HTTP status codes + an <c>ApiResponse</c> envelope.
/// </summary>
public class Result
{
    public ResultStatus Status { get; }
    public bool IsSuccess => Status == ResultStatus.Ok;
    public IReadOnlyList<string> Errors { get; }

    protected Result(ResultStatus status, IReadOnlyList<string>? errors = null)
    {
        Status = status;
        Errors = errors ?? Array.Empty<string>();
    }

    public static Result Success() => new(ResultStatus.Ok);
    public static Result NotFound(params string[] errors) => new(ResultStatus.NotFound, errors);
    public static Result Invalid(params string[] errors) => new(ResultStatus.Invalid, errors);
    public static Result Error(params string[] errors) => new(ResultStatus.Error, errors);
}

/// <summary>A <see cref="Result"/> that also carries a value on success.</summary>
public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(ResultStatus.Ok) => Value = value;
    private Result(ResultStatus status, IReadOnlyList<string> errors) : base(status, errors) { }

    public static Result<T> Success(T value) => new(value);
    public static new Result<T> NotFound(params string[] errors) => new(ResultStatus.NotFound, errors);
    public static new Result<T> Invalid(params string[] errors) => new(ResultStatus.Invalid, errors);
    public static new Result<T> Error(params string[] errors) => new(ResultStatus.Error, errors);
}
