using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ModularShop.Kernel.Web;

/// <summary>
/// Catches unhandled exceptions and returns a consistent <see cref="ApiResponse"/> with HTTP 500.
/// Cross-cutting concerns like this belong in the kernel so every module benefits uniformly.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception processing {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(
                ApiResponse.Fail("An unexpected error occurred.", new[] { ex.Message }));
        }
    }
}
