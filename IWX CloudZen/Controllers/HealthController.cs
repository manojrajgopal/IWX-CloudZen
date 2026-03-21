using Microsoft.AspNetCore.Mvc;
using IWX_CloudZen.DTOs.Responses;

namespace IWX_CloudZen.Controllers
{
    [ApiController]
    [Produces("application/json")]
    public class HealthController : ControllerBase
    {
        [HttpGet("/")]
        [ProducesResponseType(typeof(ApiStatusResponse), 200)]
        public IActionResult Index()
        {
            var response = new ApiStatusResponse
            {
                StatusCode = 200,
                Service = "IWX CloudZen API is running.."
            };

            return Ok(response);
        }

        [HttpGet("api/health")]
        [ProducesResponseType(typeof(HealthResponses), 200)]
        public IActionResult Health()
        {
            var response = new HealthResponses
            {
                StatusCode = 200,
                Status = "Running",
                Service = "IWX CloudZen API is running..",
                Time = DateTime.UtcNow
            };

            return Ok(response);
        }
    }
}
