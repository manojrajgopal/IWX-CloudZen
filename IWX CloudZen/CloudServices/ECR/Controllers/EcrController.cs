using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IWX_CloudZen.CloudServices.ECR.DTOs;
using IWX_CloudZen.CloudServices.ECR.Services;

namespace IWX_CloudZen.CloudServices.ECR.Controllers
{
    [ApiController]
    [Route("api/cloud/services/ecr")]
    public class EcrController : ControllerBase
    {
        private readonly EcrService _service;

        public EcrController(EcrService service)
        {
            _service = service;
        }

        // ================================================================
        // REPOSITORY ENDPOINTS
        // ================================================================

        [HttpGet("aws/repositories")]
        [Authorize]
        public async Task<IActionResult> ListRepositories(int accountId)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                if (user is null) return Unauthorized();

                var result = await _service.ListRepositories(user, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpGet("aws/repositories/{repoId}")]
        [Authorize]
        public async Task<IActionResult> GetRepository(int accountId, int repoId)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                if (user is null) return Unauthorized();

                var result = await _service.GetRepository(user, accountId, repoId);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Repository not found.");
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpPost("aws/repositories")]
        [Authorize]
        public async Task<IActionResult> CreateRepository(int accountId, [FromBody] CreateRepositoryRequest request)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                if (user is null) return Unauthorized();

                var result = await _service.CreateRepository(user, accountId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpPut("aws/repositories/{repoId}")]
        [Authorize]
        public async Task<IActionResult> UpdateRepository(int accountId, int repoId, [FromBody] UpdateRepositoryRequest request)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                if (user is null) return Unauthorized();

                var result = await _service.UpdateRepository(user, accountId, repoId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Repository not found.");
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpDelete("aws/repositories/{repoId}")]
        [Authorize]
        public async Task<IActionResult> DeleteRepository(int accountId, int repoId, [FromQuery] bool force = true)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                if (user is null) return Unauthorized();

                await _service.DeleteRepository(user, accountId, repoId, force);
                return Ok(new { message = "Repository deleted successfully." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Repository not found.");
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        // ================================================================
        // IMAGE ENDPOINTS
        // ================================================================

        [HttpGet("aws/repositories/{repoId}/images")]
        [Authorize]
        public async Task<IActionResult> ListImages(int accountId, int repoId)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                if (user is null) return Unauthorized();

                var result = await _service.ListImages(user, accountId, repoId);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Repository not found.");
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpGet("aws/images/{imageId}")]
        [Authorize]
        public async Task<IActionResult> GetImage(int accountId, int imageId)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                if (user is null) return Unauthorized();

                var result = await _service.GetImage(user, accountId, imageId);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Image not found.");
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpDelete("aws/images/{imageId}")]
        [Authorize]
        public async Task<IActionResult> DeleteImage(int accountId, int imageId)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                if (user is null) return Unauthorized();

                await _service.DeleteImage(user, accountId, imageId);
                return Ok(new { message = "Image deleted successfully." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Image not found.");
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        // ================================================================
        // SYNC ENDPOINTS
        // ================================================================

        [HttpPost("aws/repositories/sync")]
        [Authorize]
        public async Task<IActionResult> SyncRepositories(int accountId)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                if (user is null) return Unauthorized();

                var result = await _service.SyncRepositories(user, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpPost("aws/repositories/{repoId}/images/sync")]
        [Authorize]
        public async Task<IActionResult> SyncImages(int accountId, int repoId)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                if (user is null) return Unauthorized();

                var result = await _service.SyncImages(user, accountId, repoId);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Repository not found.");
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        [HttpPost("aws/sync")]
        [Authorize]
        public async Task<IActionResult> SyncAll(int accountId)
        {
            try
            {
                var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                if (user is null) return Unauthorized();

                var result = await _service.SyncAll(user, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }
    }
}
