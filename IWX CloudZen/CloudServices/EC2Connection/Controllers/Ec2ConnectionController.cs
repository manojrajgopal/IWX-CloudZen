using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IWX_CloudZen.CloudServices.EC2Connection.DTOs;
using IWX_CloudZen.CloudServices.EC2Connection.Services;
using System.Security.Claims;

namespace IWX_CloudZen.CloudServices.EC2Connection.Controllers
{
    [ApiController]
    [Route("api/cloud/services/ec2-connection")]
    public class Ec2ConnectionController : ControllerBase
    {
        private readonly Ec2ConnectionService _service;

        public Ec2ConnectionController(Ec2ConnectionService service)
        {
            _service = service;
        }

        private string? CurrentUser => User.FindFirst(ClaimTypes.Email)?.Value;

        // ==============================
        // START CONNECTION
        // ==============================

        /// <summary>
        /// Start an SSH connection to an EC2 instance.
        /// Resolves IP, key pair, and OS user from the database automatically.
        /// </summary>
        [HttpPost("aws/connect")]
        [Authorize]
        public async Task<IActionResult> Connect(
            [FromQuery] int accountId,
            [FromBody] StartConnectionRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.Connect(user, accountId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Connection failed: " + ex.Message });
            }
        }

        // ==============================
        // EXECUTE COMMAND
        // ==============================

        /// <summary>
        /// Execute a shell command on an active SSH session.
        /// Returns stdout, stderr, and exit code.
        /// </summary>
        [HttpPost("aws/execute")]
        [Authorize]
        public async Task<IActionResult> Execute(
            [FromQuery] int accountId,
            [FromBody] ExecuteCommandRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.Execute(user, accountId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Execution failed: " + ex.Message });
            }
        }

        // ==============================
        // DISCONNECT
        // ==============================

        /// <summary>
        /// Disconnect an active SSH session.
        /// </summary>
        [HttpPost("aws/disconnect")]
        [Authorize]
        public async Task<IActionResult> Disconnect(
            [FromQuery] int accountId,
            [FromBody] DisconnectRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.Disconnect(user, accountId, request.SessionId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Disconnect failed: " + ex.Message });
            }
        }

        // ==============================
        // GET SESSION STATUS / LOGS
        // ==============================

        /// <summary>
        /// Get status and command history for an active session.
        /// </summary>
        [HttpGet("aws/session/{sessionId}")]
        [Authorize]
        public async Task<IActionResult> GetSessionStatus(
            [FromRoute] string sessionId,
            [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.GetSessionStatus(user, accountId, sessionId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed: " + ex.Message });
            }
        }

        // ==============================
        // LIST ACTIVE SESSIONS
        // ==============================

        /// <summary>
        /// List all active SSH sessions for the current user and account.
        /// </summary>
        [HttpGet("aws/sessions")]
        [Authorize]
        public async Task<IActionResult> ListActiveSessions([FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.ListActiveSessions(user, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed: " + ex.Message });
            }
        }
    }
}
