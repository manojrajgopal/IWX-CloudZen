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

                var clusters = await _service.ListAwsClusters(user, accountId);

                return Ok(clusters);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpPost("aws/create")]
        [Authorize]
        public async Task<IActionResult> Setup(int accountId, [FromBody] CreateClusterRequest request)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                if (user is null) return Unauthorized();

                var result = await _service.SetupAwsInfrastructure(user, accountId, request.ClusterName);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }
    }
}
