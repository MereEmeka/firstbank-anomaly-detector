using Microsoft.AspNetCore.Mvc;
using FirstBank.API.DTOs;
using FirstBank.Core.Models;
using FirstBank.DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FirstBank.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly FirstDBContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            FirstDBContext context,
            IConfiguration config,
            ILogger<AuthController> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            // Why BCrypt? It is intentionally slow. It stops hackers from using supercomputers 
            // to guess millions of passwords a second.
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password, 12);

            var user = new AppUser
            {
                Email = request.Email,
                PasswordHash = hashedPassword,
                Role = request.Role  // Default role is "Customer"
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";

            // BCrypt.Verify mathematically hashes the incoming
            // password and compares it to the database hash
            if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failure for Email: {Email} from IP {IPAddress} at {AttemptedAt}",
                    request.Email, ipAddress, DateTime.UtcNow);

                return Unauthorized(new { message = "Invalid email or password." });
            }
            _logger.LogInformation("Login Success for User: {UserId}, Email: {Email}, IP: {IPAddress}",
                user.UserId, user.Email, ipAddress);

            //Geenerating the JWT Token
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role) //This claim allows AuthZ to work later
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_config["Jwt:ExpiryMinutes"])),
                signingCredentials: credentials);

            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }
    }
}