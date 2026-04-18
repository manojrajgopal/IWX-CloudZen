using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IWX_CloudZen.CloudServices.InternetGateway.DTOs;
using IWX_CloudZen.CloudServices.InternetGateway.Services;
using System.Security.Claims;

namespace IWX_CloudZen.CloudServices.InternetGateway.Controllers
{
    [ApiController]
    [Route("api/cloud/services/internet-gateway")]
    public class InternetGatewayController : ControllerBase
    {
        private readonly InternetGatewayService _service;

        public InternetGatewayController(InternetGatewayService service)
        {
            _service = service;
        }

        private string? CurrentUser => User.FindFirst(ClaimTypes.Email)?.Value;

        // ---- List all internet gateways ----

        [HttpGet("aws/list")]
        [Authorize]
        public async Task<IActionResult> ListInternetGateways([FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.ListInternetGateways(user, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed: " + ex.Message });
            }
        }

        // ---- Get single internet gateway by DB ID ----

        [HttpGet("aws/{id}")]
        [Authorize]
        public async Task<IActionResult> GetInternetGateway([FromRoute] int id, [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.GetInternetGateway(user, accountId, id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed: " + ex.Message });
            }
        }

        // ---- Get internet gateway for a specific VPC ----

        [HttpGet("aws/vpc/{vpcId}")]
        [Authorize]
        public async Task<IActionResult> GetInternetGatewayForVpc([FromRoute] string vpcId, [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.GetInternetGatewayForVpc(user, accountId, vpcId);
                if (result is null)
                    return Ok(new { hasInternetGateway = false, internetGateway = (object?)null });

                return Ok(new { hasInternetGateway = true, internetGateway = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed: " + ex.Message });
            }
        }

        // ---- Create internet gateway ----

        [HttpPost("aws/create")]
        [Authorize]
        public async Task<IActionResult> CreateInternetGateway([FromQuery] int accountId, [FromBody] CreateInternetGatewayRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.CreateInternetGateway(user, accountId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed: " + ex.Message });
            }
        }

        // ---- Update internet gateway (rename) ----

        [HttpPut("aws/update/{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateInternetGateway([FromRoute] int id, [FromQuery] int accountId, [FromBody] UpdateInternetGatewayRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.UpdateInternetGateway(user, accountId, id, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed: " + ex.Message });
            }
        }

        // ---- Delete internet gateway ----

        [HttpDelete("aws/delete/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteInternetGateway([FromRoute] int id, [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                await _service.DeleteInternetGateway(user, accountId, id);
                return Ok(new { message = "Internet Gateway deleted successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed: " + ex.Message });
            }
        }

        // ---- Attach internet gateway to VPC ----

        [HttpPost("aws/attach/{id}")]
        [Authorize]
        public async Task<IActionResult> AttachToVpc([FromRoute] int id, [FromQuery] int accountId, [FromBody] AttachInternetGatewayRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.AttachToVpc(user, accountId, id, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed: " + ex.Message });
            }
        }

        // ---- Detach internet gateway from VPC ----

        [HttpPost("aws/detach/{id}")]
        [Authorize]
        public async Task<IActionResult> DetachFromVpc([FromRoute] int id, [FromQuery] int accountId, [FromBody] DetachInternetGatewayRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.DetachFromVpc(user, accountId, id, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed: " + ex.Message });
            }
        }

        // ---- Sync internet gateways from cloud ----

        [HttpPost("aws/sync")]
        [Authorize]
        public async Task<IActionResult> SyncInternetGateways([FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.SyncInternetGateways(user, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed: " + ex.Message });
            }
        }
    }
}
