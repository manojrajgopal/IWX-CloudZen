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
        // CONNECT MANUAL (no DB record required)
        // ==============================

        /// <summary>
        /// Connect to any SSH host by providing IP, OS user, and PEM key directly.
        /// Does not require the instance to be in the database.
        /// </summary>
        [HttpPost("aws/connect-manual")]
        [Authorize]
        public async Task<IActionResult> ConnectManual(
            [FromQuery] int accountId,
            [FromBody] ManualConnectionRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.ConnectManual(user, accountId, request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Manual connection failed: " + ex.Message });
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
        // TAB COMPLETE
        // ==============================

        /// <summary>
        /// Returns file/folder name completions for the given partial word.
        /// Used by the terminal's Tab key handler.
        /// </summary>
        [HttpGet("aws/tab-complete")]
        [Authorize]
        public async Task<IActionResult> TabComplete(
            [FromQuery] int accountId,
            [FromQuery] string sessionId,
            [FromQuery] string? partial)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var completions = await _service.TabComplete(user, accountId, sessionId, partial ?? "");
                return Ok(new { completions });
            }
            catch
            {
                // Return empty list on any error — tab complete is best-effort
                return Ok(new { completions = Array.Empty<string>() });
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

        // ==============================
        // FILE BROWSER — LIST DIRECTORY
        // ==============================

        [HttpGet("aws/files/list")]
        [Authorize]
        public async Task<IActionResult> ListDirectory(
            [FromQuery] int accountId,
            [FromQuery] string sessionId,
            [FromQuery] string path = "/")
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();
                var result = await _service.ListDirectory(user, accountId, sessionId, path);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { message = "Failed: " + ex.Message }); }
        }

        // ==============================
        // FILE BROWSER — READ FILE
        // ==============================

        [HttpGet("aws/files/read")]
        [Authorize]
        public async Task<IActionResult> ReadFile(
            [FromQuery] int accountId,
            [FromQuery] string sessionId,
            [FromQuery] string path)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();
                var result = await _service.ReadFile(user, accountId, sessionId, path);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { message = "Failed: " + ex.Message }); }
        }

        // ==============================
        // FILE BROWSER — WRITE FILE
        // ==============================

        [HttpPost("aws/files/write")]
        [Authorize]
        public async Task<IActionResult> WriteFile(
            [FromQuery] int accountId,
            [FromBody] FileWriteRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();
                var result = await _service.WriteFile(user, accountId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { message = "Failed: " + ex.Message }); }
        }

        // ==============================
        // FILE BROWSER — DELETE
        // ==============================

        [HttpDelete("aws/files/delete")]
        [Authorize]
        public async Task<IActionResult> DeleteFile(
            [FromQuery] int accountId,
            [FromBody] FileDeleteRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();
                var result = await _service.DeleteFileOrDirectory(user, accountId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { message = "Failed: " + ex.Message }); }
        }

        // ==============================
        // FILE BROWSER — MAKE DIRECTORY
        // ==============================

        [HttpPost("aws/files/mkdir")]
        [Authorize]
        public async Task<IActionResult> MakeDirectory(
            [FromQuery] int accountId,
            [FromBody] FileMkdirRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();
                var result = await _service.MakeDirectory(user, accountId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { message = "Failed: " + ex.Message }); }
        }

        // ==============================
        // FILE BROWSER — RENAME / MOVE
        // ==============================

        [HttpPost("aws/files/rename")]
        [Authorize]
        public async Task<IActionResult> RenameOrMove(
            [FromQuery] int accountId,
            [FromBody] FileRenameRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();
                var result = await _service.RenameOrMove(user, accountId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { message = "Failed: " + ex.Message }); }
        }

        // ==============================
        // FILE BROWSER — COPY
        // ==============================

        [HttpPost("aws/files/copy")]
        [Authorize]
        public async Task<IActionResult> CopyFile(
            [FromQuery] int accountId,
            [FromBody] FileCopyRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();
                var result = await _service.CopyFile(user, accountId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { message = "Failed: " + ex.Message }); }
        }

        // ==============================
        // FILE BROWSER — DOWNLOAD
        // ==============================

        [HttpGet("aws/files/download")]
        [Authorize]
        public async Task<IActionResult> DownloadFile(
            [FromQuery] int accountId,
            [FromQuery] string sessionId,
            [FromQuery] string path)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();
                var result = await _service.DownloadFile(user, accountId, sessionId, path);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { message = "Failed: " + ex.Message }); }
        }

        // ==============================
        // FILE BROWSER — SEARCH
        // ==============================

        [HttpPost("aws/files/search")]
        [Authorize]
        public async Task<IActionResult> SearchFiles(
            [FromQuery] int accountId,
            [FromBody] FileSearchRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();
                var result = await _service.SearchFiles(user, accountId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { message = "Failed: " + ex.Message }); }
        }

        // ==============================
        // SYSTEM INFO
        // ==============================

        [HttpGet("aws/system-info")]
        [Authorize]
        public async Task<IActionResult> GetSystemInfo(
            [FromQuery] int accountId,
            [FromQuery] string sessionId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();
                var result = await _service.GetSystemInfo(user, accountId, sessionId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { message = "Failed: " + ex.Message }); }
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
