using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EntityFramework.DAL;
using EntityFramework.DTO;

namespace EntityFramework.Controllers;

[Route("api/devices")]
[ApiController]
public class DeviceController : ControllerBase
{
    private readonly DeviceContext _context;
    private readonly ILogger<DeviceController> _logger;

    public DeviceController(DeviceContext context, ILogger<DeviceController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<object>>> GetDevices()
    {
        _logger.LogInformation("Fetching all devices (ID and Name only).");

        var devices = await _context.Device
            .Select(d => new { d.Id, d.Name })
            .ToListAsync();

        return Ok(devices);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> GetDevice(int id)
    {
        _logger.LogInformation("Fetching device details for ID: {Id}", id);

        var device = await _context.Device
            .Include(d => d.DeviceType)
            .Include(d => d.DeviceEmployees)
                .ThenInclude(de => de.Employee)
                    .ThenInclude(e => e.Person)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (device == null)
        {
            _logger.LogWarning("Device not found for ID: {Id}", id);
            return NotFound();
        }

        var currentEmployee = device.DeviceEmployees
            .OrderByDescending(de => de.IssueDate)
            .FirstOrDefault(de => de.ReturnDate == null);

        var response = new
        {
            device.Name,
            DeviceTypeName = device.DeviceType?.Name,
            device.IsEnabled,
            AdditionalProperties = JsonSerializer.Deserialize<object>(device.AdditionalProperties),
            CurrentEmployee = currentEmployee == null ? null : new
            {
                Id = currentEmployee.Employee.Id,
                Name = $"{currentEmployee.Employee.Person.FirstName} {currentEmployee.Employee.Person.LastName}"
            }
        };

        var username = User.Identity?.Name;

        var account = await _context.Account
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Username == username);

        if (User.IsInRole("User") && currentEmployee?.EmployeeId != account?.Employee.Id)
        {
            _logger.LogWarning("Unauthorized access attempt by user: {Username} for device ID: {Id}", username, id);
            return Forbid();
        }

        _logger.LogInformation("Successfully returned device ID: {Id}", id);
        return Ok(response);
    }

    [HttpGet("types")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDeviceTypes()
    {
        _logger.LogInformation("Fetching all device types...");

        var types = await _context.DeviceType
            .Select(t => new { t.Id, t.Name })
            .ToListAsync();

        _logger.LogInformation("Returned {Count} device types.", types.Count);
        return Ok(types);
    }
    
    
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PostDevice(DeviceDTO dto)
    {
        _logger.LogInformation("Attempting to create new device: {Name}", dto.Name);

        var deviceType = await _context.DeviceType
            .FirstOrDefaultAsync(dt => dt.Name == dto.DeviceTypeName);

        if (deviceType == null)
        {
            _logger.LogWarning("Device type not found: {Type}", dto.DeviceTypeName);
            return BadRequest($"Device type '{dto.DeviceTypeName}' not found.");
        }

        var device = new Device
        {
            Name = dto.Name,
            IsEnabled = dto.IsEnabled,
            AdditionalProperties = JsonSerializer.Serialize(dto.AdditionalProperties),
            DeviceTypeId = deviceType.Id
        };

        _context.Device.Add(device);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Device created successfully with ID: {Id}", device.Id);
        return CreatedAtAction(nameof(GetDevice), new { id = device.Id }, new { device.Id });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PutDevice(int id, DeviceDTO dto)
    {
        _logger.LogInformation("Attempting to update device ID: {Id}", id);

        var device = await _context.Device.FirstOrDefaultAsync(d => d.Id == id);
        if (device == null)
        {
            _logger.LogWarning("Device not found for update: {Id}", id);
            return NotFound();
        }

        var deviceType = await _context.DeviceType
            .FirstOrDefaultAsync(dt => dt.Name == dto.DeviceTypeName);

        if (deviceType == null)
        {
            _logger.LogWarning("Device type not found for update: {Type}", dto.DeviceTypeName);
            return BadRequest($"Device type '{dto.DeviceTypeName}' not found.");
        }

        device.Name = dto.Name;
        device.IsEnabled = dto.IsEnabled;
        device.AdditionalProperties = JsonSerializer.Serialize(dto.AdditionalProperties);
        device.DeviceTypeId = deviceType.Id;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Device ID: {Id} updated successfully.", id);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDevice(int id)
    {
        _logger.LogInformation("Attempting to delete device ID: {Id}", id);

        var device = await _context.Device.FindAsync(id);
        if (device == null)
        {
            _logger.LogWarning("Device not found for deletion: {Id}", id);
            return NotFound();
        }

        _context.Device.Remove(device);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Device ID: {Id} deleted successfully.", id);
        return NoContent();
    }
}