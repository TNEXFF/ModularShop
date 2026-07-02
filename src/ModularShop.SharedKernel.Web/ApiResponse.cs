namespace ModularShop.SharedKernel.Web;

/// <summary>
/// Uniform JSON envelope returned by every endpoint. This mirrors the Platform's
/// <c>ApiResponse</c> (there built on <c>Ardalis.Result</c>); it is implemented here in a handful
/// of lines to stay dependency-free and completely transparent for teaching.
/// </summary>
public class ApiResponse
{
    public bool IsSuccess { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static ApiResponse Ok(string? message = null) => new() { IsSuccess = true, Message = message };

    public static ApiResponse Fail(string message, IReadOnlyList<string>? errors = null)
        => new() { IsSuccess = false, Message = message, Errors = errors ?? Array.Empty<string>() };
}

/// <summary>An <see cref="ApiResponse"/> that also carries a typed payload.</summary>
public sealed class ApiResponse<T> : ApiResponse
{
    public T? Data { get; init; }

    public static ApiResponse<T> Success(T data, string? message = null)
        => new() { IsSuccess = true, Data = data, Message = message };

    public static new ApiResponse<T> Fail(string message, IReadOnlyList<string>? errors = null)
        => new() { IsSuccess = false, Message = message, Errors = errors ?? Array.Empty<string>() };
}
