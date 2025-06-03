using EntityFramework.DAL;
using EntityFramework.DTO;
using EntityFramework.Services.Tokens;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EntityFramework.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        
        private readonly DeviceContext _context;
        private readonly ITokenService _tokenService;
        private readonly PasswordHasher<Account> _passwordHasher = new();

        public AuthController(DeviceContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpPost]
        public async Task<ActionResult> Auth(LoginDTO login, CancellationToken cancellationToken)
        {
            var foundUser = await _context.Account.Include(a => a.Role)
                .FirstOrDefaultAsync(a => string.Equals(a.Username, login.Username),
                cancellationToken);
            if (foundUser == null)
            {
                return Unauthorized();
            }

            var verificationResult =
                _passwordHasher.VerifyHashedPassword(foundUser,foundUser.Password ,login.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return Unauthorized();
            }

            var token = new
            {
                AccessToken = _tokenService.GenerateToken(foundUser.Username, foundUser.Role.Name)
            };
            
            return Ok(token);
        }
    }
}
