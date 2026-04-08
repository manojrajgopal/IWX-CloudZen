using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IWX_CloudZen.CloudServices.EC2.DTOs;
using IWX_CloudZen.CloudServices.EC2.Services;
using System.Security.Claims;

namespace IWX_CloudZen.CloudServices.EC2.Controllers
{
    [ApiController]
    [Route("api/cloud/services/ec2")]
    public class Ec2Controller : ControllerBase
    {
        private readonly Ec2Service _service;

        public Ec2Controller(Ec2Service service)
        {
            _service = service;
        }

        private string? CurrentUser => User.FindFirst(ClaimTypes.Email)?.Value;

        // ---- List / Get ----

        [HttpGet("aws/list")]
        [Authorize]
        public async Task<IActionResult> ListInstances([FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.ListInstances(user, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpGet("aws/get/{instanceDbId}")]
        [Authorize]
        public async Task<IActionResult> GetInstance([FromRoute] int instanceDbId, [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.GetInstance(user, accountId, instanceDbId);
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

        // ---- Launch ----

        [HttpPost("aws/launch")]
        [Authorize]
        public async Task<IActionResult> LaunchInstances([FromQuery] int accountId, [FromBody] LaunchEc2InstanceRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.LaunchInstances(user, accountId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        // ---- Update ----

        [HttpPut("aws/update/{instanceDbId}")]
        [Authorize]
        public async Task<IActionResult> UpdateInstance([FromRoute] int instanceDbId, [FromQuery] int accountId, [FromBody] UpdateEc2InstanceRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.UpdateInstance(user, accountId, instanceDbId, request);
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

        // ---- Start / Stop / Reboot ----

        [HttpPost("aws/start/{instanceDbId}")]
        [Authorize]
        public async Task<IActionResult> StartInstance([FromRoute] int instanceDbId, [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                await _service.StartInstance(user, accountId, instanceDbId);
                return Ok(new { message = "EC2 instance start initiated." });
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

        [HttpPost("aws/stop/{instanceDbId}")]
        [Authorize]
        public async Task<IActionResult> StopInstance([FromRoute] int instanceDbId, [FromQuery] int accountId, [FromQuery] bool force = false)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                await _service.StopInstance(user, accountId, instanceDbId, force);
                return Ok(new { message = "EC2 instance stop initiated." });
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

        [HttpPost("aws/reboot/{instanceDbId}")]
        [Authorize]
        public async Task<IActionResult> RebootInstance([FromRoute] int instanceDbId, [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                await _service.RebootInstance(user, accountId, instanceDbId);
                return Ok(new { message = "EC2 instance reboot initiated." });
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

        // ---- Terminate ----

        [HttpDelete("aws/terminate/{instanceDbId}")]
        [Authorize]
        public async Task<IActionResult> TerminateInstance([FromRoute] int instanceDbId, [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                await _service.TerminateInstance(user, accountId, instanceDbId);
                return Ok(new { message = "EC2 instance terminated successfully." });
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

        // ---- Sync ----

        [HttpPost("aws/sync")]
        [Authorize]
        public async Task<IActionResult> SyncInstances([FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.SyncInstances(user, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }
    }
}
