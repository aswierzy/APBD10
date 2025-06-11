using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EntityFramework.DAL;

namespace EntityFramework.Controllers;

[Route("api/roles")]
[ApiController]
public class RolesController : ControllerBase
{
    private readonly DeviceContext _context;
    private readonly ILogger<RolesController> _logger;

    public RolesController(DeviceContext context, ILogger<RolesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetRoles()
    {
        _logger.LogInformation("Fetching all roles...");
        var roles = await _context.Role
            .Select(r => new { r.Id, r.Name })
            .ToListAsync();

        _logger.LogInformation("Returned {Count} roles.", roles.Count);
        return Ok(roles);
    }
}