using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IWX_CloudZen.CloudServiceCreation.Services;
using IWX_CloudZen.CloudServiceCreation.DTOs;

namespace IWX_CloudZen.CloudServiceCreation.Controllers
{
    [ApiController]
    [Route("api/cloud/service")]
    public class CloudServiceController : ControllerBase
    {
        private readonly CloudInfrastructureService _service;

        public CloudServiceController(CloudInfrastructureService service)
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

        [HttpPost("aws/s3/create")]
        [Authorize]
        public async Task<IActionResult> CreateBucket([FromBody] S3BucketCreateRequest request)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                var result = await _service.CreateAwsBucket(user, request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
