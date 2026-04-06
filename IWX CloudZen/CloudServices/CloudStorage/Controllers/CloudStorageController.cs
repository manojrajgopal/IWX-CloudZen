using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IWX_CloudZen.CloudServices.CloudStorage.DTOs;
using IWX_CloudZen.CloudServices.CloudStorage.Services;

namespace IWX_CloudZen.CloudServices.CloudStorage.Controllers
{
    [ApiController]
    [Route("api/cloud/services/storage")]
    public class CloudStorageController : ControllerBase
    {
        private readonly CloudStorageService _service;

        public CloudStorageController(CloudStorageService service)
        {
            _service = service;
        }

        private string? CurrentUser => User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

        // ===================== BUCKET ENDPOINTS =====================

        [HttpGet("aws/s3/buckets")]
        [Authorize]
        public async Task<IActionResult> ListBuckets(int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                return Ok(await _service.ListBuckets(user, accountId));
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("aws/s3/buckets")]
        [Authorize]
        public async Task<IActionResult> CreateBucket(int accountId, [FromBody] CreateBucketRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                return Ok(await _service.CreateBucket(user, accountId, request.BucketName));
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpDelete("aws/s3/buckets/{bucketId}")]
        [Authorize]
        public async Task<IActionResult> DeleteBucket(int accountId, int bucketId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                await _service.DeleteBucket(user, accountId, bucketId);
                return Ok("Bucket and all its files deleted.");
            }
            catch (KeyNotFoundException) { return NotFound("Bucket not found."); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("aws/s3/buckets/sync")]
        [Authorize]
        public async Task<IActionResult> SyncBuckets(int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                return Ok(await _service.SyncBuckets(user, accountId));
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        // ===================== FILE ENDPOINTS =====================

        [HttpGet("aws/s3/files")]
        [Authorize]
        public async Task<IActionResult> ListFiles(int accountId, int? bucketId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                return Ok(await _service.ListFiles(user, accountId, bucketId));
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("aws/s3/files")]
        [Authorize]
        public async Task<IActionResult> UploadFile(int accountId, int bucketId, [FromForm] IFormFile file, [FromForm] string folder)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                return Ok(await _service.UploadFile(user, accountId, bucketId, file, folder));
            }
            catch (KeyNotFoundException) { return NotFound("Bucket not found."); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("aws/s3/files/{fileId}/download")]
        [Authorize]
        public async Task<IActionResult> DownloadFile(int fileId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var (stream, contentType, fileName) = await _service.DownloadFile(user, fileId);
                return File(stream, contentType, fileName);
            }
            catch (KeyNotFoundException) { return NotFound("File not found."); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPut("aws/s3/files/{fileId}")]
        [Authorize]
        public async Task<IActionResult> UpdateFile(int fileId, [FromForm] IFormFile file)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                return Ok(await _service.UpdateFile(user, fileId, file));
            }
            catch (KeyNotFoundException) { return NotFound("File not found."); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpDelete("aws/s3/files/{fileId}")]
        [Authorize]
        public async Task<IActionResult> DeleteFile(int fileId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                await _service.DeleteFile(user, fileId);
                return Ok("File deleted.");
            }
            catch (KeyNotFoundException) { return NotFound("File not found."); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("aws/s3/files/sync")]
        [Authorize]
        public async Task<IActionResult> SyncFiles(int accountId, int bucketId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                return Ok(await _service.SyncFiles(user, accountId, bucketId));
            }
            catch (KeyNotFoundException) { return NotFound("Bucket not found."); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("aws/s3/sync")]
        [Authorize]
        public async Task<IActionResult> SyncAll(int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                return Ok(await _service.SyncAll(user, accountId));
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
    }
}
