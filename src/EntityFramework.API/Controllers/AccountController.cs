using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using EntityFramework.DAL;
namespace EntityFramework.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly DeviceContext _context;
    private readonly IPasswordHasher<Account> _passwordHasher;

    public AccountController(DeviceContext context, IPasswordHasher<Account> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(Account account)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (await _context.Account.AnyAsync(a => a.Username == account.Username))
            return Conflict("Username already exists.");

        account.Password = _passwordHasher.HashPassword(account, account.Password);

        _context.Account.Add(account);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Account created successfully." });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllAccounts()
    {
        var accounts = await _context.Account
            .Select(a => new { a.Id, a.Username, a.Password })
            .ToListAsync();

        return Ok(accounts);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAccountById(int id)
    {
        var account = await _context.Account.FindAsync(id);
        if (account == null) return NotFound();

        return Ok(new { account.Username, account.Password });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> UpdateOwnAccount(int id, Account updatedAccount)
    {
        var account = await _context.Account.FindAsync(id);
        if (account == null) return NotFound();

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        account.Password = _passwordHasher.HashPassword(account, updatedAccount.Password);
        await _context.SaveChangesAsync();

        return Ok("Account updated successfully.");
    }
}
