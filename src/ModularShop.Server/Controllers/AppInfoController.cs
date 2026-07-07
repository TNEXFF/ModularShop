using Microsoft.AspNetCore.Mvc;
using ModularShop.Kernel.Infrastructure;
using ModularShop.Kernel.Api;

namespace ModularShop.Server.Controllers;

/// <summary>
/// Host-level diagnostics endpoint (<c>GET /api</c>) that lists the loaded modules. It is a real
/// controller (like every endpoint now) and returns the uniform <see cref="ApiResponse{T}"/> envelope.
/// </summary>
[ApiController]
[Route("api")]
public sealed class AppInfoController : ControllerBase
{
    private readonly IEnumerable<IModule> _modules;

    public AppInfoController(IEnumerable<IModule> modules) => _modules = modules;

    [HttpGet]
    public ActionResult<ApiResponse<AppInfo>> Get()
    {
        var info = new AppInfo(
            "ModularShop",
            "A Modular Monolith teaching example (ASP.NET Core + React, MSSQL).",
            _modules.Select(m => m.Name).ToArray());
        return Ok(ApiResponse<AppInfo>.Success(info));
    }
}

public sealed record AppInfo(string Application, string Description, IReadOnlyList<string> Modules);
