using IWX_CloudZen.Data;
using IWX_CloudZen.Services;
using Microsoft.AspNetCore.Mvc;
using IWX_CloudZen.DTOs.Auth;
using IWX_CloudZen.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace IWX_CloudZen.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwt;

        public AuthController(AppDbContext context, JwtService jwt)
        {
            _context = context;
            _jwt = jwt;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest req)
        {
            if (req.Password != req.ConfirmPassword)
                return BadRequest(new { message = "Passwords do not match" });

            var exists = await _context.Users.AnyAsync(x => x.Email == req.Email);

            if (exists)
                return Conflict(new { message = "User already exists" });

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);

            var user = new User
            {
                Name = req.Name,
                Email = req.Email,
                PhoneNumber = req.PhoneNumber,
                PasswordHash = passwordHash,
                Role = "User",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Created("User Created Successfully", 201);
        }

        [HttpPost("login")]
        public IActionResult Login(LoginRequest req)
        {
            var user = _context.Users.FirstOrDefault(x => x.Email == req.Email);

            if (user == null)
                return Unauthorized();

            bool valid = BCrypt.Net.BCrypt.Verify(
                req.Password,
                user.PasswordHash
            );

            if (!valid)
                return Unauthorized();

            var token = _jwt.GenerateToken(user);

            return Ok(new
            {
                token,
                email = user.Email,
                name = user.Name,
                role = user.Role,
                expires = "60 minutes"
            });
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            var Name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var Email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var Role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            
            return Ok(new
            {
                Name,
                Email,
                Role,
                status = 200,
                Service = "Token Valid"
            });
        }
    }
}
