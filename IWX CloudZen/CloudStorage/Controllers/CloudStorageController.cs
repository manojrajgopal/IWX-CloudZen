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
                return StatusCode(500, "Error: " + ex.Message );
            }
            
        }

        [HttpGet("files")]
        [Authorize]
        public IActionResult Files()
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                return Ok(_service.GetFiles(user));
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }

        [HttpGet("download/{id}")]
        [Authorize]
        public async Task<IActionResult> Download(int id)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                var result = await _service.Download(user, id);

                return File(result.Item1, result.Item2, result.Item3);
            }
            catch
            {
                return NotFound(new { message = "Unable to download, File not found..." });
            }
        }

        [HttpDelete("delete/{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                await _service.Delete(user, id);

                return Ok("Deleted");
            }
            catch
            {
                return NotFound(new { mesasge = "Unable to delete file, File not found..." });
            }
        }

        [HttpPut("update/{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, IFormFile file)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                var result = await _service.UpdateFile(user, id, file);

                return Ok(result);
            }
            catch
            {
                return NotFound(new { message = "Unable to update file, File not found.." });
            }
        }

        [HttpGet("folders")]
        [Authorize]
        public IActionResult Folders()
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                return Ok(_service.GetFolders(user));
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }
    }
}
