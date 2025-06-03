using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EntityFramework.DAL;

namespace EntityFramework.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EmployeesController : ControllerBase
{
    private readonly DeviceContext _context;

    public EmployeesController(DeviceContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllEmployees()
    {
        var employees = await _context.Employee
            .Include(e => e.Person)
            .Select(e => new
            {
                e.Id,
                FullName = $"{e.Person.FirstName} {e.Person.LastName}"
            })
            .ToListAsync();

        return Ok(employees);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,User")]
    public async Task<IActionResult> GetEmployeeById(int id)
    {
        var employee = await _context.Employee
            .Include(e => e.Person)
            .Include(e => e.Position)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
            return NotFound();

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

        return Ok(result);
    }
}