using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ModularShop.SharedKernel.Domain;
using ModularShop.SharedKernel.Identity;

namespace ModularShop.SharedKernel.Web;

public static class WebExtensions
{
    /// <summary>Registers shared web cross-cutting services (current-user accessor).</summary>
    public static IServiceCollection AddSharedWeb(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        return services;
    }

    /// <summary>
    /// Maps a domain <see cref="Result{T}"/> to an HTTP response carrying an <see cref="ApiResponse{T}"/>.
    /// This is where the module-neutral result becomes an HTTP status code, so endpoints stay tiny.
    /// </summary>
    public static IResult ToHttpResult<T>(this Result<T> result) => result.Status switch
    {
        ResultStatus.Ok => Results.Ok(ApiResponse<T>.Success(result.Value!)),
        ResultStatus.NotFound => Results.NotFound(ApiResponse<T>.Fail("Not found.", result.Errors)),
        ResultStatus.Invalid => Results.BadRequest(ApiResponse<T>.Fail("Validation failed.", result.Errors)),
        _ => Results.Json(ApiResponse<T>.Fail("Unexpected error.", result.Errors), statusCode: 500),
    };
}
