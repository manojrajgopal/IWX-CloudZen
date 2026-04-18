using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IWX_CloudZen.CloudServices.Mapped.Services;
using System.Security.Claims;

namespace IWX_CloudZen.CloudServices.Mapped.Controllers
{
    [ApiController]
    [Route("api/cloud/services/mapped")]
    public class MappedController : ControllerBase
    {
        private readonly MappedService _service;

        public MappedController(MappedService service)
        {
            _service = service;
        }

        private string? CurrentUser => User.FindFirst(ClaimTypes.Email)?.Value;

        // ---- 1. Full Resource Graph (from DB) ----

        [HttpGet("aws/graph")]
        [Authorize]
        public async Task<IActionResult> GetFullGraph([FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.GetFullGraph(user, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed: " + ex.Message });
            }
        }

        // ---- 2. VPC Resource Tree (from DB) ----

        [HttpGet("aws/vpc/{vpcId}/tree")]
        [Authorize]
        public async Task<IActionResult> GetVpcResourceTree([FromRoute] string vpcId, [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.GetVpcResourceTree(user, accountId, vpcId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed: " + ex.Message });
            }
        }

        // ---- 3. VPC Resource Tree (live from cloud) ----

        [HttpGet("aws/vpc/{vpcId}/tree/live")]
        [Authorize]
        public async Task<IActionResult> GetLiveVpcResources([FromRoute] string vpcId, [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.GetLiveVpcResources(user, accountId, vpcId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed: " + ex.Message });
            }
        }

        // ---- 4. Resource Dependencies ----

        [HttpGet("aws/dependencies/{resourceType}/{resourceId}")]
        [Authorize]
        public async Task<IActionResult> GetResourceDependencies(
            [FromRoute] string resourceType,
            [FromRoute] string resourceId,
            [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.GetResourceDependencies(user, accountId, resourceType, resourceId);
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

        // ---- 5. Deletion Blockers ----

        [HttpGet("aws/deletion-blockers/{resourceType}/{resourceId}")]
        [Authorize]
        public async Task<IActionResult> GetDeletionBlockers(
            [FromRoute] string resourceType,
            [FromRoute] string resourceId,
            [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.GetDeletionBlockers(user, accountId, resourceType, resourceId);
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

        // ---- 6. Mapped Public Addresses (live cloud — explains IGW detach errors) ----

        [HttpGet("aws/vpc/{vpcId}/mapped-public-addresses")]
        [Authorize]
        public async Task<IActionResult> GetMappedPublicAddresses([FromRoute] string vpcId, [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.GetMappedPublicAddresses(user, accountId, vpcId);
                return Ok(new
                {
                    vpcId,
                    hasMappedAddresses = result.Count > 0,
                    count = result.Count,
                    mappedAddresses = result,
                    message = result.Count > 0
                        ? $"This VPC has {result.Count} mapped public address(es). Unmap them before detaching the Internet Gateway."
                        : "No mapped public addresses found. Internet Gateway can be safely detached."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed: " + ex.Message });
            }
        }

        // ---- 7. Network Interfaces in VPC (live cloud) ----

        [HttpGet("aws/vpc/{vpcId}/network-interfaces")]
        [Authorize]
        public async Task<IActionResult> GetNetworkInterfaces([FromRoute] string vpcId, [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.GetNetworkInterfaces(user, accountId, vpcId);
                return Ok(new
                {
                    vpcId,
                    count = result.Count,
                    networkInterfaces = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed: " + ex.Message });
            }
        }

        // ---- 8. Sync Resource Graph (live cloud refresh) ----

        [HttpPost("aws/sync")]
        [Authorize]
        public async Task<IActionResult> SyncResourceGraph([FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.SyncResourceGraph(user, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Failed: " + ex.Message });
            }
        }
    }
}
