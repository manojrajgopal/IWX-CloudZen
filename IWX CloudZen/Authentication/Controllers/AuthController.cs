using IWX_CloudZen.Authentication.DTOs.Auth;
using IWX_CloudZen.Authentication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IWX_CloudZen.Authentication.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth)
        {
            _auth = auth;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest req)
        {
            try
            {
                var result = await _auth.Register(req);

                return Created(result, 201);
            }
            catch (Exception ex)
            {
                return BadRequest( new { message = ex.Message } );
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest req)
        {
            try
            {
                var result = await _auth.Login(req);

                return Ok(result);
            }
            catch
            {
                return Unauthorized();
            }
        }

        [HttpGet("me")]
        [Authorize]

        public IActionResult Me()
        {
            var result = _auth.Me(User);

            return Ok(result);
        }
    }
}