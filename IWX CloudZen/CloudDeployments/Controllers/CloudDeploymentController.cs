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
            //try
            //{
                if (request.Package == null || request.Package.Length == 0)
                    return BadRequest("Package file is required.");

                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                var result = await _service.Deploy(user, request.Name, request.DeploymentType, request.CloudAccountId, request.Package);

                return Ok(result);
            //}
            //catch (Exception ex)
            //{
            //    return BadRequest("Failed to Deploy: " + ex.Message);
            //}
        }

        [HttpGet]
        [Authorize]
        public IActionResult List()
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                return Ok(_service.GetDeployments(user));
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to get list of deployments: " + ex.Message);
            }
        }

        [HttpGet("{id}/status")]
        [Authorize]
        public IActionResult Status(int id)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                var dep = _service.GetDeployments(user).First(x => x.Id == id);

                return Ok(dep.Status);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to get status: " + ex.Message);
            }
        }

        [HttpPost("{id}/stop")]
        [Authorize]
        public async Task<IActionResult> Stop(int id)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                await _service.Stop(user, id);

                return Ok("Stoped");
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to Stop: " + ex.Message);
            }
        }

        [HttpPost("{id}/restart")]
        [Authorize]
        public async Task<IActionResult> Restart(int id)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                await _service.Restart(user!, id);
                return Ok("Restarted");
            }
            catch (Exception ex)
            {
                return BadRequest("Failed to restart: " + ex.Message);
            }
        }

        [HttpGet("{id}/health")]
        public IActionResult Health(int id)
        {
            return Ok(
                new
                {
                    status = "Running",
                    health = "Healthy"
                }
            );
        }
    }
}
