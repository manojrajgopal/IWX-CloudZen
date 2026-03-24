using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IWX_CloudZen.CloudDeployments.Services;
using IWX_CloudZen.CloudDeployments.DTOs;

namespace IWX_CloudZen.CloudDeployments.Controllers
{
    [ApiController]
    [Route("api/cloud/deploy")]
    public class CloudDeploymentController : ControllerBase
    {
        private readonly CloudDeploymentService _service;

        public CloudDeploymentController(CloudDeploymentService service) 
        { 
            _service = service;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Deploy([FromForm] DeploymentRequest request)
        {
            var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            var result = await _service.Deploy(user, request.Name, request.DeploymentType, request.CloudAccountId, request.Package);

            return Ok(result);
        }

        [HttpGet]
        [Authorize]
        public IActionResult List()
        {
            var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            return Ok(_service.GetDeployments(user));
        }
    }
}
