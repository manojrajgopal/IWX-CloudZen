using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IWX_CloudZen.CloudServices.Subnet.DTOs;
using IWX_CloudZen.CloudServices.Subnet.Services;
using System.Security.Claims;

namespace IWX_CloudZen.CloudServices.Subnet.Controllers
{
    [ApiController]
    [Route("api/cloud/services/subnet")]
    public class SubnetController : ControllerBase
    {
        private readonly SubnetService _service;

        public SubnetController(SubnetService service)
        {
            _service = service;
        }

        private string? CurrentUser => User.FindFirst(ClaimTypes.Email)?.Value;

        // ================================================================
        // LIST
        // ================================================================

        /// <summary>
        /// List all subnets for the given account stored in DB.
        /// Optionally filter by ?vpcId= to scope to a single VPC.
        /// </summary>
        [HttpGet("aws/list")]
        [Authorize]
        public async Task<IActionResult> ListSubnets(
            [FromQuery] int accountId,
            [FromQuery] string? vpcId = null)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.ListSubnets(user, accountId, vpcId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================================================================
        // GET
        // ================================================================

        /// <summary>Get a single subnet by its DB record ID.</summary>
        [HttpGet("aws/{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetSubnet(
            [FromQuery] int accountId, int id)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.GetSubnet(user, accountId, id);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Subnet not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================================================================
        // CREATE
        // ================================================================

        /// <summary>Create a new subnet inside a VPC.</summary>
        [HttpPost("aws/create")]
        [Authorize]
        public async Task<IActionResult> CreateSubnet(
            [FromQuery] int accountId,
            [FromBody] CreateSubnetRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.CreateSubnet(user, accountId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================================================================
        // UPDATE
        // ================================================================

        /// <summary>Update a subnet's name, MapPublicIpOnLaunch, or AssignIpv6AddressOnCreation.</summary>
        [HttpPut("aws/update/{id:int}")]
        [Authorize]
        public async Task<IActionResult> UpdateSubnet(
            [FromQuery] int accountId, int id,
            [FromBody] UpdateSubnetRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.UpdateSubnet(user, accountId, id, request);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Subnet not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================================================================
        // DELETE
        // ================================================================

        /// <summary>Delete a subnet from the cloud and remove the DB record.</summary>
        [HttpDelete("aws/delete/{id:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteSubnet(
            [FromQuery] int accountId, int id)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                await _service.DeleteSubnet(user, accountId, id);
                return Ok(new { message = "Subnet deleted successfully." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Subnet not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================================================================
        // SYNC
        // ================================================================

        /// <summary>
        /// Sync all subnets from AWS into the DB for the given account.
        /// Optionally pass ?vpcId= to sync only subnets within a specific VPC.
        /// </summary>
        [HttpPost("aws/sync")]
        [Authorize]
        public async Task<IActionResult> SyncSubnets(
            [FromQuery] int accountId,
            [FromQuery] string? vpcId = null)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.SyncSubnets(user, accountId, vpcId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
