using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IWX_CloudZen.CloudServices.CloudWatchLogs.DTOs;
using IWX_CloudZen.CloudServices.CloudWatchLogs.Services;
using System.Security.Claims;

namespace IWX_CloudZen.CloudServices.CloudWatchLogs.Controllers
{
    [ApiController]
    [Route("api/cloud/services/cloudwatch-logs")]
    public class CloudWatchLogsController : ControllerBase
    {
        private readonly CloudWatchLogsService _service;

        public CloudWatchLogsController(CloudWatchLogsService service)
        {
            _service = service;
        }

        private string? CurrentUser => User.FindFirst(ClaimTypes.Email)?.Value;

        // ---- Log Groups ----

        [HttpGet("aws/log-groups/list")]
        [Authorize]
        public async Task<IActionResult> ListLogGroups([FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.ListLogGroups(user, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpGet("aws/log-groups/{logGroupId}")]
        [Authorize]
        public async Task<IActionResult> GetLogGroup([FromRoute] int logGroupId, [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.GetLogGroup(user, accountId, logGroupId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpPost("aws/log-groups/create")]
        [Authorize]
        public async Task<IActionResult> CreateLogGroup([FromQuery] int accountId, [FromBody] CreateLogGroupRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.CreateLogGroup(user, accountId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpPut("aws/log-groups/update/{logGroupId}")]
        [Authorize]
        public async Task<IActionResult> UpdateLogGroup([FromRoute] int logGroupId, [FromQuery] int accountId, [FromBody] UpdateLogGroupRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.UpdateLogGroup(user, accountId, logGroupId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpDelete("aws/log-groups/delete/{logGroupId}")]
        [Authorize]
        public async Task<IActionResult> DeleteLogGroup([FromRoute] int logGroupId, [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                await _service.DeleteLogGroup(user, accountId, logGroupId);
                return Ok(new { message = "Log group deleted successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpPost("aws/log-groups/sync")]
        [Authorize]
        public async Task<IActionResult> SyncLogGroups([FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.SyncLogGroups(user, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        // ---- Log Streams ----

        [HttpGet("aws/log-groups/{logGroupId}/streams/list")]
        [Authorize]
        public async Task<IActionResult> ListLogStreams([FromRoute] int logGroupId, [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.ListLogStreams(user, accountId, logGroupId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpPost("aws/log-groups/{logGroupId}/streams/create")]
        [Authorize]
        public async Task<IActionResult> CreateLogStream([FromRoute] int logGroupId, [FromQuery] int accountId, [FromBody] CreateLogStreamRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.CreateLogStream(user, accountId, logGroupId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpDelete("aws/log-groups/{logGroupId}/streams/delete/{logStreamId}")]
        [Authorize]
        public async Task<IActionResult> DeleteLogStream([FromRoute] int logGroupId, [FromRoute] int logStreamId, [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                await _service.DeleteLogStream(user, accountId, logGroupId, logStreamId);
                return Ok(new { message = "Log stream deleted successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpPost("aws/log-groups/{logGroupId}/streams/sync")]
        [Authorize]
        public async Task<IActionResult> SyncLogStreams([FromRoute] int logGroupId, [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.SyncLogStreams(user, accountId, logGroupId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        // ---- Log Events ----

        [HttpGet("aws/log-groups/{logGroupId}/events")]
        [Authorize]
        public async Task<IActionResult> GetLogEvents(
            [FromRoute] int logGroupId,
            [FromQuery] int accountId,
            [FromQuery] string logStreamName,
            [FromQuery] int limit = 100,
            [FromQuery] string? nextToken = null)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.GetLogEvents(user, accountId, logGroupId, logStreamName, limit, nextToken);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpPost("aws/log-groups/{logGroupId}/events/put")]
        [Authorize]
        public async Task<IActionResult> PutLogEvents(
            [FromRoute] int logGroupId,
            [FromQuery] int accountId,
            [FromBody] PutLogEventsRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                await _service.PutLogEvents(user, accountId, logGroupId, request);
                return Ok(new { message = "Log events published successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpPost("aws/log-groups/{logGroupId}/events/filter")]
        [Authorize]
        public async Task<IActionResult> FilterLogEvents(
            [FromRoute] int logGroupId,
            [FromQuery] int accountId,
            [FromBody] FilterLogEventsRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.FilterLogEvents(user, accountId, logGroupId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }
    }
}
