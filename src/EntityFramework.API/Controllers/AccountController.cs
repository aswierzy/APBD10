using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using EntityFramework.DAL;

namespace EntityFramework.Controllers;

[Route("api/accounts")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly DeviceContext _context;
    private readonly IPasswordHasher<Account> _passwordHasher;
    private readonly ILogger<AccountController> _logger;

    public AccountController(DeviceContext context, IPasswordHasher<Account> passwordHasher, ILogger<AccountController> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(Account account)
    {
        _logger.LogInformation("Register attempt for username: {Username}", account.Username);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state for account registration: {@ModelState}", ModelState);
            return BadRequest(ModelState);
        }

        if (await _context.Account.AnyAsync(a => a.Username == account.Username))
        {
            _logger.LogWarning("Username already exists: {Username}", account.Username);
            return Conflict("Username already exists.");
        }

        account.Password = _passwordHasher.HashPassword(account, account.Password);

        _context.Account.Add(account);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Account created successfully for username: {Username}", account.Username);
        return Ok(new { Message = "Account created successfully." });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllAccounts()
    {
        _logger.LogInformation("Admin requested all accounts");
        var accounts = await _context.Account
            .Select(a => new { a.Id, a.Username, a.Password })
            .ToListAsync();

        return Ok(accounts);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAccountById(int id)
    {
        _logger.LogInformation("Fetching account by ID: {Id}", id);
        var account = await _context.Account.FindAsync(id);
        if (account == null)
        {
            _logger.LogWarning("Account not found for ID: {Id}", id);
            return NotFound();
        }

        return Ok(new { account.Username, account.Password });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> UpdateOwnAccount(int id, Account updatedAccount)
    {
        _logger.LogInformation("User updating account ID: {Id}", id);
        var account = await _context.Account.FindAsync(id);
        if (account == null)
        {
            _logger.LogWarning("Attempt to update non-existent account ID: {Id}", id);
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid model state during account update for ID: {Id}", id);
            return BadRequest(ModelState);
        }

        account.Password = _passwordHasher.HashPassword(account, updatedAccount.Password);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Account ID: {Id} updated successfully", id);
        return Ok("Account updated successfully.");
    }
}
