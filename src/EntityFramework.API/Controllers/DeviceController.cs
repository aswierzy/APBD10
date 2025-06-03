using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EntityFramework.DAL;
using EntityFramework.DTO;

namespace EntityFramework.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DeviceController : ControllerBase
{
    private readonly DeviceContext _context;

    public DeviceController(DeviceContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<object>>> GetDevices()
    {
        var devices = await _context.Device
            .Select(d => new
            {
                d.Id,
                d.Name
            }).ToListAsync();

        return Ok(devices);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> GetDevice(int id)
    {
        var device = await _context.Device
            .Include(d => d.DeviceType)
            .Include(d => d.DeviceEmployees)
                .ThenInclude(de => de.Employee)
                    .ThenInclude(e => e.Person)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (device == null)
            return NotFound();

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

        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PostDevice(DeviceDTO dto)
    {
        var deviceType = await _context.DeviceType
            .FirstOrDefaultAsync(dt => dt.Name == dto.DeviceTypeName);

        if (deviceType == null)
            return BadRequest($"Device type '{dto.DeviceTypeName}' not found.");

        var device = new Device
        {
            Name = dto.Name,
            IsEnabled = dto.IsEnabled,
            AdditionalProperties = JsonSerializer.Serialize(dto.AdditionalProperties),
            DeviceTypeId = deviceType.Id
        };

        _context.Device.Add(device);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDevice), new { id = device.Id }, new { device.Id });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PutDevice(int id, DeviceDTO dto)
    {
        var device = await _context.Device.FirstOrDefaultAsync(d => d.Id == id);

        if (device == null)
            return NotFound();

        var deviceType = await _context.DeviceType
            .FirstOrDefaultAsync(dt => dt.Name == dto.DeviceTypeName);

        if (deviceType == null)
            return BadRequest($"Device type '{dto.DeviceTypeName}' not found.");

        device.Name = dto.Name;
        device.IsEnabled = dto.IsEnabled;
        device.AdditionalProperties = JsonSerializer.Serialize(dto.AdditionalProperties);
        device.DeviceTypeId = deviceType.Id;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDevice(int id)
    {
        var device = await _context.Device.FindAsync(id);
        if (device == null)
            return NotFound();

        _context.Device.Remove(device);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}