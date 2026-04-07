using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IWX_CloudZen.CloudServices.ECS.DTOs;
using IWX_CloudZen.CloudServices.ECS.Services;
using System.Security.Claims;

namespace IWX_CloudZen.CloudServices.ECS.Controllers
{
    [ApiController]
    [Route("api/cloud/services/ecs")]
    public class EcsController : ControllerBase
    {
        private readonly EcsService _service;

        public EcsController(EcsService service)
        {
            _service = service;
        }

        // ----------------------------------------------------------------
        // Helper
        // ----------------------------------------------------------------

        private string? CurrentUser => User.FindFirst(ClaimTypes.Email)?.Value;

        // ================================================================
        // TASK DEFINITION ENDPOINTS
        // ================================================================

        /// <summary>List all task definitions stored in DB for this account. Filter by ?family=</summary>
        [HttpGet("aws/task-definitions")]
        [Authorize]
        public async Task<IActionResult> ListTaskDefinitions(
            [FromQuery] int accountId,
            [FromQuery] string? family = null)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.ListTaskDefinitions(user, accountId, family);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Get a single task definition by its DB record ID.</summary>
        [HttpGet("aws/task-definitions/{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetTaskDefinition(
            [FromQuery] int accountId, int id)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.GetTaskDefinition(user, accountId, id);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Task definition not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Register a new task definition (or new revision of an existing family).</summary>
        [HttpPost("aws/task-definitions")]
        [Authorize]
        public async Task<IActionResult> RegisterTaskDefinition(
            [FromQuery] int accountId,
            [FromBody] RegisterTaskDefinitionRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.RegisterTaskDefinition(user, accountId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Deregister a task definition revision — marks it INACTIVE in AWS.</summary>
        [HttpPost("aws/task-definitions/{id:int}/deregister")]
        [Authorize]
        public async Task<IActionResult> DeregisterTaskDefinition(
            [FromQuery] int accountId, int id)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.DeregisterTaskDefinition(user, accountId, id);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Task definition not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Permanently delete a task definition revision from AWS (auto-deregisters if ACTIVE)
        /// and removes the record from DB.
        /// </summary>
        [HttpDelete("aws/task-definitions/{id:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteTaskDefinition(
            [FromQuery] int accountId, int id)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                await _service.DeleteTaskDefinition(user, accountId, id);
                return Ok(new { message = "Task definition deleted successfully." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Task definition not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Sync all ACTIVE task definitions from AWS into the DB.</summary>
        [HttpPost("aws/task-definitions/sync")]
        [Authorize]
        public async Task<IActionResult> SyncTaskDefinitions([FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.SyncTaskDefinitions(user, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================================================================
        // SERVICE ENDPOINTS
        // ================================================================

        /// <summary>List ECS services. Optionally filter by ?clusterName=</summary>
        [HttpGet("aws/services")]
        [Authorize]
        public async Task<IActionResult> ListServices(
            [FromQuery] int accountId,
            [FromQuery] string? clusterName = null)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.ListServices(user, accountId, clusterName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Get a single ECS service by its DB record ID.</summary>
        [HttpGet("aws/services/{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetService(
            [FromQuery] int accountId, int id)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.GetService(user, accountId, id);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Service not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Create a new ECS service inside a cluster.</summary>
        [HttpPost("aws/services")]
        [Authorize]
        public async Task<IActionResult> CreateService(
            [FromQuery] int accountId,
            [FromBody] CreateEcsServiceRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.CreateService(user, accountId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Update an ECS service (desired count and/or task definition).</summary>
        [HttpPut("aws/services/{id:int}")]
        [Authorize]
        public async Task<IActionResult> UpdateService(
            [FromQuery] int accountId, int id,
            [FromBody] UpdateEcsServiceRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.UpdateService(user, accountId, id, request);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Service not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Delete an ECS service (scales to 0, then removes from AWS and DB).</summary>
        [HttpDelete("aws/services/{id:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteService(
            [FromQuery] int accountId, int id)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                await _service.DeleteService(user, accountId, id);
                return Ok(new { message = "Service deleted successfully." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Service not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Sync all ECS services for a specific cluster from AWS into the DB.</summary>
        [HttpPost("aws/services/sync")]
        [Authorize]
        public async Task<IActionResult> SyncServices(
            [FromQuery] int accountId,
            [FromQuery] string clusterName)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                if (string.IsNullOrWhiteSpace(clusterName))
                    return BadRequest(new { message = "clusterName query parameter is required." });

                var result = await _service.SyncServices(user, accountId, clusterName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================================================================
        // TASK ENDPOINTS
        // ================================================================

        /// <summary>List ECS task records in DB. Optionally filter by ?clusterName=</summary>
        [HttpGet("aws/tasks")]
        [Authorize]
        public async Task<IActionResult> ListTasks(
            [FromQuery] int accountId,
            [FromQuery] string? clusterName = null)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.ListTasks(user, accountId, clusterName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Get a single ECS task record by its DB ID.</summary>
        [HttpGet("aws/tasks/{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetTask(
            [FromQuery] int accountId, int id)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.GetTask(user, accountId, id);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Task not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Run one or more ECS task instances from a task definition.</summary>
        [HttpPost("aws/tasks/run")]
        [Authorize]
        public async Task<IActionResult> RunTask(
            [FromQuery] int accountId,
            [FromBody] RunTaskRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.RunTask(user, accountId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Stop a running ECS task by its DB ID.</summary>
        [HttpPost("aws/tasks/{id:int}/stop")]
        [Authorize]
        public async Task<IActionResult> StopTask(
            [FromQuery] int accountId, int id,
            [FromBody] StopTaskRequest request)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                await _service.StopTask(user, accountId, id, request.Reason);
                return Ok(new { message = "Stop signal sent. Task will transition to STOPPED shortly." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Task not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Sync all ECS tasks for a specific cluster from AWS into the DB.</summary>
        [HttpPost("aws/tasks/sync")]
        [Authorize]
        public async Task<IActionResult> SyncTasks(
            [FromQuery] int accountId,
            [FromQuery] string clusterName)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                if (string.IsNullOrWhiteSpace(clusterName))
                    return BadRequest(new { message = "clusterName query parameter is required." });

                var result = await _service.SyncTasks(user, accountId, clusterName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ================================================================
        // FULL SYNC ENDPOINT
        // ================================================================

        /// <summary>
        /// Full ECS sync: syncs task definitions (account-wide) then syncs services and tasks
        /// for every cluster known to this account in the DB.
        /// </summary>
        [HttpPost("aws/sync")]
        [Authorize]
        public async Task<IActionResult> SyncAll([FromQuery] int accountId)
        {
            try
            {
                var user = CurrentUser;
                if (user is null) return Unauthorized();

                var result = await _service.SyncAll(user, accountId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
