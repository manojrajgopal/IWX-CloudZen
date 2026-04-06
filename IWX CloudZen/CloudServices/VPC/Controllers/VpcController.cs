using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IWX_CloudZen.CloudServices.VPC.DTOs;
using IWX_CloudZen.CloudServices.VPC.Services;
using System.Security.Claims;

namespace IWX_CloudZen.CloudServices.VPC.Controllers
{
    [ApiController]
    [Route("api/cloud/services/vpc")]
    public class VpcController : ControllerBase
    {
        private readonly VpcService _service;

        public VpcController(VpcService service)
        {
            _service = service;
        }

        private string? CurrentUser => User.FindFirst(ClaimTypes.Email)?.Value;

        [HttpGet("aws/list")]
        [Authorize]
        public async Task<IActionResult> ListVpcs([FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.ListVpcs(user, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpPost("aws/create")]
        [Authorize]
        public async Task<IActionResult> CreateVpc([FromQuery] int accountId, [FromBody] CreateVpcRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.CreateVpc(user, accountId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpPut("aws/update/{vpcId}")]
        [Authorize]
        public async Task<IActionResult> UpdateVpc([FromRoute] int vpcId, [FromQuery] int accountId, [FromBody] UpdateVpcRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.UpdateVpc(user, accountId, vpcId, request);
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

        [HttpDelete("aws/delete/{vpcId}")]
        [Authorize]
        public async Task<IActionResult> DeleteVpc([FromRoute] int vpcId, [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                await _service.DeleteVpc(user, accountId, vpcId);
                return Ok(new { message = "VPC deleted successfully." });
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

        [HttpPost("aws/sync")]
        [Authorize]
        public async Task<IActionResult> SyncVpcs([FromQuery] int accountId)
        {
            //try
            //{
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.SyncVpcs(user, accountId);
                return Ok(result);
            //}
            //catch (Exception ex)
            //{
            //    return BadRequest("Failed: " + ex.Message);
            //}
        }
    }
}
