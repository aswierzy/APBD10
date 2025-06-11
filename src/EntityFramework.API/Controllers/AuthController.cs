using EntityFramework.DAL;
using EntityFramework.DTO;
using EntityFramework.Services.Tokens;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EntityFramework.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly DeviceContext _context;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthController> _logger;
        private readonly PasswordHasher<Account> _passwordHasher = new();

        public AuthController(DeviceContext context, ITokenService tokenService, ILogger<AuthController> logger)
        {
            _context = context;
            _tokenService = tokenService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult> Auth(LoginDTO login, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Authentication attempt for username: {Username}", login.Username);

            var foundUser = await _context.Account.Include(a => a.Role)
                .FirstOrDefaultAsync(a => a.Username == login.Username, cancellationToken);

            if (foundUser == null)
            {
                _logger.LogWarning("Authentication failed: User not found for username {Username}", login.Username);
                return Unauthorized();
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(foundUser, foundUser.Password, login.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Authentication failed: Invalid password for username {Username}", login.Username);
                return Unauthorized();
            }

            var token = new
            {
                AccessToken = _tokenService.GenerateToken(foundUser.Username, foundUser.Role.Name)
            };

            _logger.LogInformation("Authentication successful for username: {Username}", login.Username);
            return Ok(token);
        }
    }
}