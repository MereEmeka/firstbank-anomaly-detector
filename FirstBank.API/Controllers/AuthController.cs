using FirstBank.API.DTOs;
using FirstBank.API.Features;
using FirstBank.API.Services;
using FirstBank.Core.Models;
using FirstBank.DataAccess.Data;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IEmailService _emailService;
        private readonly IMediator _mediator;

        public AuthController(
            FirstDBContext context,
            IConfiguration config,
            ILogger<AuthController> logger,
            IEmailService emailService,
            IMediator mediator)
        {
            _context = context;
            _config = config;
            _logger = logger;
            _emailService = emailService;
            _mediator = mediator;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto request)
        {
            // Why BCrypt? It is intentionally slow. It stops hackers from using supercomputers 
            // to guess millions of passwords a second.
            // Add this right before hashing the password:
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest(new { message = "This email is already registered." });
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password, 12);

            var user = new AppUser
            {
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = hashedPassword,
                Role = "Customer"  // Default role is "Customer"
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            string welcomeBody = $@"
                <h2>Welcome to FirstBank, {user.FirstName}!</h2>
                <p>Your account has been successfully created.</p>
                <p><strong>Email Registered:</strong> {user.Email}</p>
                <p><strong>Assigned Role:</strong> {user.Role}</p>
                <hr />
                <p>You can now log in to your account, check your balances, and test secure transfers.</p>
                <p>Thank you for choosing FirstBank.</p>";

            // Fire-and-forget background dispatch so the client isn't delayed
            _ = _emailService.SendEmailAsync(
                user.Email,
                "Welcome to FirstBank - Account Created Successfully",
                welcomeBody);

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

            //4. FIRE THE LOGIN NOTIFICATION EMAIL ---
            string emailBody = $@"
                <h2>FirstBank Security Alert</h2>
                <p>Hello {user.FirstName},</p>
                <p>A new login was just detected on your FirstBank account from IP: <strong>{ipAddress}</strong>.</p>
                <p><strong>Time:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
                <br/>
                <p>If this was you, you can safely ignore this email. If you did not log in, please secure your account immediately.</p>";

            // Fire-and-forget background dispatch so the client isn't delayed waiting for SMTP
            _ = _emailService.SendEmailAsync(user.Email, "Security Alert: New Login Detected", emailBody);

            //Generating the JWT Token
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
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["Jwt:ExpiryMinutes"])),
                signingCredentials: credentials);

            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            // Extract the UserId securely from the JWT token
            var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return Unauthorized();

            var command = new ChangePasswordCommand
            {
                UserId = Guid.Parse(userIdClaim),
                OldPassword = request.OldPassword,
                NewPassword = request.NewPassword
            };

            var result = await _mediator.Send(command);
            return StatusCode(result.StatusCode, result);
        }
    }
}