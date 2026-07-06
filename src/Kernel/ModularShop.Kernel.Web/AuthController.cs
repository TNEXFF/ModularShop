using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ModularShop.Kernel.Infrastructure.Identity;

namespace ModularShop.Kernel.Web;

/// <summary>
/// Authentication endpoints backed by ASP.NET Core Identity. They live in the <b>kernel</b> because
/// identity is a cross-cutting concern every module shares. Sign-in uses cookie authentication
/// (<see cref="SignInManager{TUser}"/>); every response is wrapped in the same <see cref="ApiResponse"/>
/// envelope as the rest of the API, so the SPA unwraps auth and business responses identically.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;

    public AuthController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn)
    {
        _users = users;
        _signIn = signIn;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthUser>>> Register([FromBody] RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Email : request.DisplayName,
        };

        var result = await _users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<AuthUser>.Fail(
                "Registration failed.", result.Errors.Select(e => e.Description).ToList()));

        await _signIn.SignInAsync(user, isPersistent: true);
        return Ok(ApiResponse<AuthUser>.Success(ToDto(user)));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthUser>>> Login([FromBody] LoginRequest request)
    {
        var user = await _users.FindByEmailAsync(request.Email);
        if (user is not null)
        {
            var result = await _signIn.PasswordSignInAsync(user, request.Password, isPersistent: true, lockoutOnFailure: false);
            if (result.Succeeded)
                return Ok(ApiResponse<AuthUser>.Success(ToDto(user, await _users.GetRolesAsync(user))));
        }

        return Unauthorized(ApiResponse<AuthUser>.Fail("Invalid email or password."));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> Logout()
    {
        await _signIn.SignOutAsync();
        return Ok(ApiResponse.Ok("Signed out."));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<AuthUser>>> Me()
    {
        var user = await _users.GetUserAsync(User);
        return user is null
            ? Unauthorized(ApiResponse<AuthUser>.Fail("Not signed in."))
            : Ok(ApiResponse<AuthUser>.Success(ToDto(user, await _users.GetRolesAsync(user))));
    }

    private static AuthUser ToDto(ApplicationUser user, IEnumerable<string>? roles = null) =>
        new(user.Id, user.Email!, user.DisplayName, roles?.ToArray() ?? []);
}

public sealed record RegisterRequest(string Email, string Password, string? DisplayName);

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthUser(Guid Id, string Email, string DisplayName, string[] Roles);
