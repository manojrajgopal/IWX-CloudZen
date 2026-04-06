using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IWX_CloudZen.CloudServices.Cluster.DTOs;
using IWX_CloudZen.CloudServices.Cluster.Services;

namespace IWX_CloudZen.CloudServices.Cluster.Controllers
{

    [ApiController]
    [Route("api/cloud/services/cluster")]
    public class ClusterController : ControllerBase
    {
        private readonly ClusterService _service;

        public ClusterController(ClusterService service)
        {
            _service = service;
        }

        [HttpGet("aws/list")]
        [Authorize]
        public async Task<IActionResult> ListClusters(int accountId)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                if (user is null) return Unauthorized();

                var clusters = await _service.ListClusters(user, accountId);

                return Ok(clusters);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpPost("aws/create")]
        [Authorize]
        public async Task<IActionResult> CreateCluster(int accountId, [FromBody] CreateClusterRequest request)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                if (user is null) return Unauthorized();

                var result = await _service.CreateCluster(user, accountId, request.ClusterName);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpPut("aws/update/{clusterId}")]
        [Authorize]
        public async Task<IActionResult> UpdateCluster(int accountId, int clusterId, [FromBody] UpdateClusterRequest request)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                if (user is null) return Unauthorized();

                var result = await _service.UpdateCluster(user, accountId, clusterId, request.EnableContainerInsights);

                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Cluster not found.");
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpPost("aws/sync")]
        [Authorize]
        public async Task<IActionResult> SyncClusters(int accountId)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                if (user is null) return Unauthorized();

                var result = await _service.SyncClusters(user, accountId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpDelete("aws/delete/{clusterId}")]
        [Authorize]
        public async Task<IActionResult> DeleteCluster(int accountId, int clusterId)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                if (user is null) return Unauthorized();

                var result = await _service.DeleteCluster(user, accountId, clusterId);

                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Cluster not found.");
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }
    }
}
