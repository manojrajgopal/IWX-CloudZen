using IWX_CloudZen.Authentication.DTOs.Auth;
using IWX_CloudZen.Authentication.Models.Entities;
using IWX_CloudZen.Authentication.Interfaces;
using IWX_CloudZen.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace IWX_CloudZen.Authentication.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwt;

        public AuthService(AppDbContext context, JwtService jwt)
        {
            _context = context;
            _jwt = jwt;
        }

        public async Task<string> Register(RegisterRequest req)
        {
            if (req.Password != req.ConfirmPassword)
                throw new Exception("Passwords do not match");

            var exists = await _context.Users.AnyAsync(x => x.Email == req.Email);

            if (exists)
                throw new Exception("User already exists");

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

            return "User Created Successfully";
        }

        public async Task<object> Login(LoginRequest req)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == req.Email);

            if (user == null)
                throw new Exception("Invalid credentials");

            bool valid = BCrypt.Net.BCrypt.Verify( req.Password, user.PasswordHash );

            if (!valid)
                throw new Exception("Invalid credentials");

            var token = _jwt.GenerateToken(user);

            return new
            {
                token,
                email = user.Email,
                name = user.Name,
                role = user.Role,
                expires = "60 minutes"
            };
        }

        public object Me(ClaimsPrincipal user)
        {
            var Name = user.FindFirst(ClaimTypes.Name)?.Value;

            var Email = user.FindFirst(ClaimTypes.Email)?.Value;

            var Role = user.FindFirst(ClaimTypes.Role)?.Value;

            return new
            {
                Name,
                Email,
                Role,
                status = 200,
                Service = "Token Valid"
            };
        }
    }
}