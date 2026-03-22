using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudAccounts.Services;

namespace IWX_CloudZen.CloudAccounts.Controllers
{
    [ApiController]
    [Route("api/cloud")]
    public class CloudAccountsController : ControllerBase
    {
        private readonly CloudAccountService _service;

        public CloudAccountsController(CloudAccountService service)
        {
            _service = service;
        }

        [HttpGet("providers")]
        [Authorize]
        public async Task<IActionResult> GetProviders()
        {
            var providers = await _service.GetSupportedProvidersAsync();
            return Ok(providers);
        }

        [HttpPost("connect")]
        [Authorize]
        public async Task<IActionResult> Connect([FromBody] ConnectCloudRequest request)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(userEmail))
                return Unauthorized(new { message = "User email not found in token." });

            try
            {
                var result = await _service.ConnectAccountAsync(userEmail, request);
                return Ok(new
                {
                    message = "Cloud account connected successfully",
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("accounts")]
        [Authorize]
        public async Task<IActionResult> GetAccounts()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(userEmail))
                return Unauthorized(new { message = "User email not found in token." });

            var accounts = await _service.GetUserAccountsAsync(userEmail);
            return Ok(accounts);
        }

        [HttpGet("accounts/default")]
        [Authorize]
        public async Task<IActionResult> GetDefaultAccount()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(userEmail))
                return Unauthorized(new { message = "User email not found in token." });

            var account = await _service.GetDefaultAccountAsync(userEmail);
            return Ok(account);
        }

        [HttpPost("accounts/{accountId:int}/default")]
        [Authorize]
        public async Task<IActionResult> SetDefaultAccount(int accountId)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrWhiteSpace(userEmail))
                return Unauthorized(new { message = "User email not found in token." });

            var updated = await _service.SetDefaultAccountAsync(userEmail, accountId);

            if (updated == null)
                return NotFound(new { message = "Cloud account not found." });

            return Ok(new
            {
                message = "Default cloud account updated",
                data = updated
            });
        }
    }
}