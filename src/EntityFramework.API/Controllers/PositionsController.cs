using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EntityFramework.DAL;

namespace EntityFramework.Controllers;

[Route("api/positions")]
[ApiController]
public class PositionsController : ControllerBase
{
    private readonly DeviceContext _context;
    private readonly ILogger<PositionsController> _logger;

    public PositionsController(DeviceContext context, ILogger<PositionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPositions()
    {
        _logger.LogInformation("Fetching all positions...");
        var positions = await _context.Position
            .Select(p => new { p.Id, p.Name })
            .ToListAsync();

        _logger.LogInformation("Returned {Count} positions.", positions.Count);
        return Ok(positions);
    }
}