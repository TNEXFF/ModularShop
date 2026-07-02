namespace ModularShop.Kernel.Web;

/// <summary>
/// Uniform JSON envelope returned by every controller endpoint. Every response — success or
/// failure — is shaped as an <c>ApiResponse</c> so clients have a single contract to unwrap.
/// It is deliberately small and framework-free; the base controller (<see cref="ApiControllerBase"/>)
/// builds it from an Ardalis <c>Result</c>.
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

    public static ApiResponse<T> Success(T? data, string? message = null)
        => new() { IsSuccess = true, Data = data, Message = message };

    public static new ApiResponse<T> Fail(string message, IReadOnlyList<string>? errors = null)
        => new() { IsSuccess = false, Message = message, Errors = errors ?? Array.Empty<string>() };
}
