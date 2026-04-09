using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IWX_CloudZen.CloudServices.EC2InstanceConnect.DTOs;
using IWX_CloudZen.CloudServices.EC2InstanceConnect.Services;
using System.Security.Claims;

namespace IWX_CloudZen.CloudServices.EC2InstanceConnect.Controllers
{
    [ApiController]
    [Route("api/cloud/services/ec2-instance-connect")]
    public class Ec2InstanceConnectController : ControllerBase
    {
        private readonly Ec2InstanceConnectService _service;

        public Ec2InstanceConnectController(Ec2InstanceConnectService service)
        {
            _service = service;
        }

        private string? CurrentUser => User.FindFirst(ClaimTypes.Email)?.Value;

        // ==============================
        // ENDPOINT CRUD
        // ==============================

        [HttpGet("aws/endpoints/list")]
        [Authorize]
        public async Task<IActionResult> ListEndpoints([FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.ListEndpoints(user, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpGet("aws/endpoints/get/{endpointDbId}")]
        [Authorize]
        public async Task<IActionResult> GetEndpoint([FromRoute] int endpointDbId, [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.GetEndpoint(user, accountId, endpointDbId);
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

        [HttpPost("aws/endpoints/create")]
        [Authorize]
        public async Task<IActionResult> CreateEndpoint(
            [FromQuery] int accountId,
            [FromBody] CreateEc2InstanceConnectEndpointRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.CreateEndpoint(user, accountId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpDelete("aws/endpoints/delete/{endpointDbId}")]
        [Authorize]
        public async Task<IActionResult> DeleteEndpoint([FromRoute] int endpointDbId, [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                await _service.DeleteEndpoint(user, accountId, endpointDbId);
                return Ok(new { message = "EC2 Instance Connect Endpoint deleted successfully." });
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

        // ==============================
        // SEND SSH PUBLIC KEY
        // ==============================

        [HttpPost("aws/send-ssh-public-key")]
        [Authorize]
        public async Task<IActionResult> SendSshPublicKey(
            [FromQuery] int accountId,
            [FromBody] SendSshPublicKeyRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.SendSshPublicKey(user, accountId, request);
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

        // ==============================
        // SEND SERIAL CONSOLE SSH PUBLIC KEY
        // ==============================

        [HttpPost("aws/send-serial-console-ssh-public-key")]
        [Authorize]
        public async Task<IActionResult> SendSerialConsoleSshPublicKey(
            [FromQuery] int accountId,
            [FromBody] SendSerialConsoleSshPublicKeyRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.SendSerialConsoleSshPublicKey(user, accountId, request);
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

        // ==============================
        // SESSION HISTORY
        // ==============================

        [HttpGet("aws/sessions/list")]
        [Authorize]
        public async Task<IActionResult> ListSessions([FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.ListSessions(user, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpGet("aws/sessions/list-by-instance/{instanceId}")]
        [Authorize]
        public async Task<IActionResult> ListSessionsByInstance(
            [FromRoute] string instanceId, [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.ListSessionsByInstance(user, accountId, instanceId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpDelete("aws/sessions/delete/{sessionDbId}")]
        [Authorize]
        public async Task<IActionResult> DeleteSession([FromRoute] int sessionDbId, [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                await _service.DeleteSession(user, accountId, sessionDbId);
                return Ok(new { message = "Session record deleted successfully." });
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

        [HttpDelete("aws/sessions/clear")]
        [Authorize]
        public async Task<IActionResult> ClearSessionHistory([FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                await _service.ClearSessionHistory(user, accountId);
                return Ok(new { message = "All session records cleared successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        // ==============================
        // SYNC ENDPOINTS
        // ==============================

        [HttpPost("aws/sync")]
        [Authorize]
        public async Task<IActionResult> SyncEndpoints([FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.SyncEndpoints(user, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }
    }
}
