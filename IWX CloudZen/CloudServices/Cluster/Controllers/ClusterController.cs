using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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

        [HttpPost("aws/setup")]
        [Authorize]
        public async Task<IActionResult> Setup(int accountId)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                var result = await _service.SetupAwsInfrastructure(user, accountId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }
    }
}
