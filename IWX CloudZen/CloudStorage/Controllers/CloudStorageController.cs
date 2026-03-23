using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IWX_CloudZen.CloudStorage.Services;
using IWX_CloudZen.CloudStorage.DTOs;

namespace IWX_CloudZen.CloudStorage.Controllers
{
    [ApiController]
    [Route("api/storage")]
    public class CloudStorageController : ControllerBase
    {
        private readonly CloudFileService _service;

        public CloudStorageController(CloudFileService service)
        {
            _service = service;
        }

        [HttpPost("upload")]
        [Authorize]
        public async Task<IActionResult> Upload([FromForm] FileUploadRequest request)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                var result = await _service.Upload(user, request.File, request.Folder, request.CloudAccountId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            
        }
    }
}
