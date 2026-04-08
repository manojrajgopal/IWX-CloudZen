using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IWX_CloudZen.CloudServices.KeyPair.DTOs;
using IWX_CloudZen.CloudServices.KeyPair.Services;
using System.Security.Claims;

namespace IWX_CloudZen.CloudServices.KeyPair.Controllers
{
    [ApiController]
    [Route("api/cloud/services/keypair")]
    public class KeyPairController : ControllerBase
    {
        private readonly KeyPairService _service;

        public KeyPairController(KeyPairService service)
        {
            _service = service;
        }

        private string? CurrentUser => User.FindFirst(ClaimTypes.Email)?.Value;

        // ---- List ----

        /// <summary>List all key pairs stored in the database for the given cloud account.</summary>
        [HttpGet("aws/list")]
        [Authorize]
        public async Task<IActionResult> ListKeyPairs([FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.ListKeyPairs(user, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        // ---- Get ----

        /// <summary>Get a single key pair record by its database ID.</summary>
        [HttpGet("aws/get/{keyPairDbId}")]
        [Authorize]
        public async Task<IActionResult> GetKeyPair([FromRoute] int keyPairDbId, [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.GetKeyPair(user, accountId, keyPairDbId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        // ---- Create ----

        /// <summary>
        /// Create a new key pair in AWS. The private key PEM is returned in the response
        /// and stored in the database. Save it immediately — AWS will not return it again.
        /// </summary>
        [HttpPost("aws/create")]
        [Authorize]
        public async Task<IActionResult> CreateKeyPair([FromQuery] int accountId, [FromBody] CreateKeyPairRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.CreateKeyPair(user, accountId, request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        // ---- Import ----

        /// <summary>
        /// Import an existing public key into AWS.
        /// Use this when you already have an SSH key pair and want to register the public half with AWS.
        /// </summary>
        [HttpPost("aws/import")]
        [Authorize]
        public async Task<IActionResult> ImportKeyPair([FromQuery] int accountId, [FromBody] ImportKeyPairRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.ImportKeyPair(user, accountId, request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        // ---- Update Tags ----

        /// <summary>
        /// Update tags on a key pair.
        /// Note: AWS does not allow renaming a key pair — the KeyName is immutable after creation.
        /// </summary>
        [HttpPut("aws/update/{keyPairDbId}")]
        [Authorize]
        public async Task<IActionResult> UpdateKeyPairTags(
            [FromRoute] int keyPairDbId,
            [FromQuery] int accountId,
            [FromBody] UpdateKeyPairRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.UpdateKeyPairTags(user, accountId, keyPairDbId, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        // ---- Delete ----

        /// <summary>Permanently delete a key pair from AWS and remove its record from the database.</summary>
        [HttpDelete("aws/delete/{keyPairDbId}")]
        [Authorize]
        public async Task<IActionResult> DeleteKeyPair([FromRoute] int keyPairDbId, [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                await _service.DeleteKeyPair(user, accountId, keyPairDbId);
                return Ok(new { message = "Key pair deleted successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        // ---- Download Private Key ----

        /// <summary>
        /// Download the stored private key PEM for a key pair created via this API.
        /// Returns 404 if the key pair was imported or synced (private key was never stored).
        /// </summary>
        [HttpGet("aws/download-private-key/{keyPairDbId}")]
        [Authorize]
        public async Task<IActionResult> DownloadPrivateKey([FromRoute] int keyPairDbId, [FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.DownloadPrivateKey(user, accountId, keyPairDbId);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        // ---- Sync ----

        /// <summary>
        /// Sync key pairs from AWS into the local database.
        /// Adds newly discovered key pairs, updates changed fingerprints/tags,
        /// and removes records for key pairs that no longer exist on AWS.
        /// </summary>
        [HttpPost("aws/sync")]
        [Authorize]
        public async Task<IActionResult> SyncKeyPairs([FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.SyncKeyPairs(user, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }
    }
}
