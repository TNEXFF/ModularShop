using Ardalis.Result;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ModularShop.Kernel.Web;

/// <summary>
/// Base class for every module's controllers. It translates the Ardalis <see cref="Result{T}"/>
/// returned by a use case into an HTTP response carrying the uniform <see cref="ApiResponse{T}"/>
/// envelope, mapping the result status to the right HTTP status code. Controllers stay thin: they
/// invoke a use case and pass its Result to <see cref="ToApiResponse{T}"/>.
/// <para>
/// It also carries <see cref="AuthorizeAttribute"/>, so <b>every</b> module endpoint requires an
/// authenticated user by default — authentication (a kernel concern) is enforced in one place. The
/// kernel's own <c>AuthController</c> opts specific actions out with <c>[AllowAnonymous]</c>.
/// </para>
/// </summary>
[ApiController]
[Authorize]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<ApiResponse<T>> ToApiResponse<T>(Result<T> result) => result.Status switch
    {
        ResultStatus.Ok => Ok(ApiResponse<T>.Success(result.Value)),
        ResultStatus.NotFound => NotFound(ApiResponse<T>.Fail("Resource not found.", ErrorMessages(result))),
        ResultStatus.Invalid => BadRequest(ApiResponse<T>.Fail("Validation failed.",
            result.ValidationErrors.Select(e => e.ErrorMessage).ToList())),
        ResultStatus.Unauthorized => StatusCode(StatusCodes.Status401Unauthorized,
            ApiResponse<T>.Fail("Unauthorized.", ErrorMessages(result))),
        ResultStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden,
            ApiResponse<T>.Fail("Forbidden.", ErrorMessages(result))),
        _ => StatusCode(StatusCodes.Status500InternalServerError,
            ApiResponse<T>.Fail("An unexpected error occurred.", ErrorMessages(result))),
    };

    private static IReadOnlyList<string> ErrorMessages<T>(Result<T> result) => result.Errors.ToList();
}
