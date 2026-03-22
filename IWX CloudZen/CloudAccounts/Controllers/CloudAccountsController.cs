using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IWX_CloudZen.CloudAccounts.Services;
using IWX_CloudZen.CloudAccounts.DTOs;
using IWX_CloudZen.CloudAccounts.Entities;

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

        [HttpPost("connect")]
        [Authorize]
        public async Task<IActionResult>
        Connect(ConnectCloudRequest req)
        {
            var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            var account = new CloudAccount
            {
                UserEmail = user,
                Provider = req.Provider,
                AccountName = req.AccountName,
                AccessKey = req.AccessKey,
                SecretKey = req.SecretKey,
                TenantId = req.TenantId,
                ClientId = req.ClientId,
                ClientSecret = req.ClientSecret,
                Region = req.Region,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _service.ConnectAccount(account);

            return Ok(result);
        }

        [HttpGet("accounts")]
        [Authorize]
        public IActionResult GetAccounts()
        {
            var user = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            var accounts = _service.GetUserAccounts(user);

            return Ok(accounts);
        }

    }
}
