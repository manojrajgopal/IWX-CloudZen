using IWX_CloudZen.CloudServices.SecurityGroups.DTOs;
using IWX_CloudZen.CloudServices.SecurityGroups.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IWX_CloudZen.CloudServices.SecurityGroups.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/cloud/services/security-groups")]
    public class SecurityGroupController : ControllerBase
    {
        private readonly SecurityGroupService _service;

        public SecurityGroupController(SecurityGroupService service)
        {
            _service = service;
        }

        private string CurrentUser =>
            User.FindFirstValue(ClaimTypes.Email)
            ?? throw new UnauthorizedAccessException("User identity not found.");

        // ================================================================
        // LIST
        // GET /api/cloud/services/security-groups/aws/list?accountId=1[&vpcId=vpc-xxx]
        // ================================================================

        [HttpGet("aws/list")]
        public async Task<IActionResult> List(
            [FromQuery] int accountId,
            [FromQuery] string? vpcId = null)
        {
            try
            {
                var result = await _service.ListSecurityGroups(CurrentUser, accountId, vpcId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================================================================
        // GET
        // GET /api/cloud/services/security-groups/aws/{id}?accountId=1
        // ================================================================

        [HttpGet("aws/{id:int}")]
        public async Task<IActionResult> Get([FromRoute] int id, [FromQuery] int accountId)
        {
            try
            {
                var result = await _service.GetSecurityGroup(CurrentUser, accountId, id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================================================================
        // CREATE
        // POST /api/cloud/services/security-groups/aws/create?accountId=1
        // ================================================================

        [HttpPost("aws/create")]
        public async Task<IActionResult> Create(
            [FromQuery] int accountId,
            [FromBody] CreateSecurityGroupRequest request)
        {
            try
            {
                var result = await _service.CreateSecurityGroup(CurrentUser, accountId, request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================================================================
        // UPDATE
        // PUT /api/cloud/services/security-groups/aws/update/{id}?accountId=1
        // ================================================================

        [HttpPut("aws/update/{id:int}")]
        public async Task<IActionResult> Update(
            [FromRoute] int id,
            [FromQuery] int accountId,
            [FromBody] UpdateSecurityGroupRequest request)
        {
            try
            {
                var result = await _service.UpdateSecurityGroup(CurrentUser, accountId, id, request);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================================================================
        // DELETE
        // DELETE /api/cloud/services/security-groups/aws/delete/{id}?accountId=1
        // ================================================================

        [HttpDelete("aws/delete/{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id, [FromQuery] int accountId)
        {
            try
            {
                await _service.DeleteSecurityGroup(CurrentUser, accountId, id);
                return Ok(new { message = "Security group deleted." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================================================================
        // ADD INBOUND RULES
        // POST /api/cloud/services/security-groups/aws/{id}/inbound/add?accountId=1
        // ================================================================

        [HttpPost("aws/{id:int}/inbound/add")]
        public async Task<IActionResult> AddInboundRules(
            [FromRoute] int id,
            [FromQuery] int accountId,
            [FromBody] AddInboundRulesRequest request)
        {
            try
            {
                var result = await _service.AddInboundRules(CurrentUser, accountId, id, request.Rules);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================================================================
        // REMOVE INBOUND RULES
        // POST /api/cloud/services/security-groups/aws/{id}/inbound/remove?accountId=1
        // ================================================================

        [HttpPost("aws/{id:int}/inbound/remove")]
        public async Task<IActionResult> RemoveInboundRules(
            [FromRoute] int id,
            [FromQuery] int accountId,
            [FromBody] RemoveInboundRulesRequest request)
        {
            try
            {
                var result = await _service.RemoveInboundRules(CurrentUser, accountId, id, request.RuleIds);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================================================================
        // ADD OUTBOUND RULES
        // POST /api/cloud/services/security-groups/aws/{id}/outbound/add?accountId=1
        // ================================================================

        [HttpPost("aws/{id:int}/outbound/add")]
        public async Task<IActionResult> AddOutboundRules(
            [FromRoute] int id,
            [FromQuery] int accountId,
            [FromBody] AddOutboundRulesRequest request)
        {
            try
            {
                var result = await _service.AddOutboundRules(CurrentUser, accountId, id, request.Rules);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================================================================
        // REMOVE OUTBOUND RULES
        // POST /api/cloud/services/security-groups/aws/{id}/outbound/remove?accountId=1
        // ================================================================

        [HttpPost("aws/{id:int}/outbound/remove")]
        public async Task<IActionResult> RemoveOutboundRules(
            [FromRoute] int id,
            [FromQuery] int accountId,
            [FromBody] RemoveOutboundRulesRequest request)
        {
            try
            {
                var result = await _service.RemoveOutboundRules(CurrentUser, accountId, id, request.RuleIds);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================================================================
        // SYNC
        // POST /api/cloud/services/security-groups/aws/sync?accountId=1[&vpcId=vpc-xxx]
        // ================================================================

        [HttpPost("aws/sync")]
        public async Task<IActionResult> Sync(
            [FromQuery] int accountId,
            [FromQuery] string? vpcId = null)
        {
            try
            {
                var result = await _service.SyncSecurityGroups(CurrentUser, accountId, vpcId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
