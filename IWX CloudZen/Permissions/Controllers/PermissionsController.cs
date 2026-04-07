using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IWX_CloudZen.Permissions.DTOs;
using IWX_CloudZen.Permissions.Services;
using System.Security.Claims;

namespace IWX_CloudZen.Permissions.Controllers
{
    [ApiController]
    [Route("api/permissions")]
    [Authorize]
    public class PermissionsController : ControllerBase
    {
        private readonly PermissionsService _service;

        public PermissionsController(PermissionsService service)
        {
            _service = service;
        }

        private string? CurrentUser => User.FindFirst(ClaimTypes.Email)?.Value;

        // -----------------------------------------------------------------
        // GET  api/permissions/aws/list?accountId=1
        // Returns all policies stored in the database (fast, no AWS call).
        // -----------------------------------------------------------------
        [HttpGet("aws/list")]
        public async Task<IActionResult> ListPolicies([FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.ListPolicies(user, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        // -----------------------------------------------------------------
        // GET  api/permissions/aws/summary?accountId=1
        // Returns policy counts and group memberships derived from the
        // database. Run aws/sync first to populate.
        // -----------------------------------------------------------------
        [HttpGet("aws/summary")]
        public async Task<IActionResult> GetSummary([FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.GetSummary(user, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        // -----------------------------------------------------------------
        // GET  api/permissions/aws/policies?accountId=1
        // Returns policies from the database. Run aws/sync first to populate.
        // For full statement detail, use aws/sync which pulls live from AWS.
        // -----------------------------------------------------------------
        [HttpGet("aws/policies")]
        public async Task<IActionResult> GetAllPolicies([FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.GetAllPolicies(user, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        // -----------------------------------------------------------------
        // GET  api/permissions/aws/policies/available?accountId=1&scope=AWS&search=S3
        // Lists AWS-managed or customer-managed policies available to attach.
        // scope: "AWS" (default) | "Local" | "All"
        // search: optional name filter
        // -----------------------------------------------------------------
        [HttpGet("aws/policies/available")]
        public async Task<IActionResult> ListAvailablePolicies(
            [FromQuery] int accountId,
            [FromQuery] string scope = "AWS",
            [FromQuery] string? search = null)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.ListAvailablePolicies(user, accountId, scope, search);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        // -----------------------------------------------------------------
        // POST api/permissions/aws/policies/attach?accountId=1
        // Body: { "policyArn": "arn:aws:iam::aws:policy/AmazonS3FullAccess" }
        // Attaches a managed policy to the IAM user linked to this account.
        // -----------------------------------------------------------------
        [HttpPost("aws/policies/attach")]
        public async Task<IActionResult> AttachPolicy(
            [FromQuery] int accountId,
            [FromBody] AttachPolicyRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                await _service.AttachPolicy(user, accountId, request.PolicyArn);
                return Ok(new { message = $"Policy '{request.PolicyArn}' attached successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        // -----------------------------------------------------------------
        // DELETE api/permissions/aws/policies/detach?accountId=1&policyArn=arn:aws:...
        // Detaches a managed policy from the IAM user linked to this account.
        // -----------------------------------------------------------------
        [HttpDelete("aws/policies/detach")]
        public async Task<IActionResult> DetachPolicy(
            [FromQuery] int accountId,
            [FromQuery] string policyArn)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                if (string.IsNullOrWhiteSpace(policyArn))
                    return BadRequest("policyArn query parameter is required.");

                await _service.DetachPolicy(user, accountId, policyArn);
                return Ok(new { message = $"Policy '{policyArn}' detached successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        // -----------------------------------------------------------------
        // POST api/permissions/aws/check?accountId=1
        // Body: { "actions": ["s3:GetObject", "ec2:DescribeVpcs"],
        //         "resourceArns": ["*"] }
        // Simulates whether the IAM user is allowed or denied each action.
        // resourceArns is optional — defaults to ["*"].
        // -----------------------------------------------------------------
        [HttpPost("aws/check")]
        public async Task<IActionResult> CheckPermissions(
            [FromQuery] int accountId,
            [FromBody] CheckPermissionRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                if (request.Actions == null || request.Actions.Count == 0)
                    return BadRequest("At least one action is required.");

                var result = await _service.CheckPermissions(user, accountId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }

        // -----------------------------------------------------------------
        // POST api/permissions/aws/sync?accountId=1
        // Syncs all policies from AWS into the database.
        // Returns Added / Updated / Removed counts + final list.
        // -----------------------------------------------------------------
        [HttpPost("aws/sync")]
        public async Task<IActionResult> SyncPolicies([FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.SyncPolicies(user, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest("Failed: " + ex.Message);
            }
        }
    }
}
