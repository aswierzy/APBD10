using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EntityFramework.DAL;

namespace EntityFramework.Controllers;

[Route("api/employees")]
[ApiController]
public class EmployeesController : ControllerBase
{
    private readonly DeviceContext _context;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(DeviceContext context, ILogger<EmployeesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllEmployees()
    {
        _logger.LogInformation("Admin requested list of all employees.");

        var employees = await _context.Employee
            .Include(e => e.Person)
            .Select(e => new
            {
                e.Id,
                FullName = $"{e.Person.FirstName} {e.Person.LastName}"
            })
            .ToListAsync();

        _logger.LogInformation("Returned {Count} employees.", employees.Count);
        return Ok(employees);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> GetEmployeeById(int id)
    {
        _logger.LogInformation("Fetching employee with ID: {Id}", id);

        var employee = await _context.Employee
            .Include(e => e.Person)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            _logger.LogWarning("Employee not found with ID: {Id}", id);
            return NotFound();
        }

        var result = new
        {
            Person = new
            {
                employee.Person.FirstName,
                employee.Person.MiddleName,
                employee.Person.LastName,
                employee.Person.Email,
                employee.Person.PhoneNumber,
                employee.Person.PassportNumber
            },
            employee.Salary,
            Position = new
            {
                employee.Position.Id,
                employee.Position.Name
            },
            employee.HireDate
        };

        _logger.LogInformation("Successfully returned employee data for ID: {Id}", id);
        return Ok(result);
    }
}