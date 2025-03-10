using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WorkNestHRMS.Models;
using static WorkNestHRMS.Models.WorkGroupRequests;

namespace WorkNestHRMS.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/workplaces/{workplaceId}/workgroups")]
    public class WorkGroupsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<WorkGroupsController> _logger;

        public WorkGroupsController(ApplicationDbContext dbContext, ILogger<WorkGroupsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateWorkGroup(int workplaceId, [FromBody] WorkGroupRequests.CreateWorkGroupRequest request)
        {
            _logger.LogInformation("Attempting to create work group for workplace ID: {WorkplaceId}", workplaceId);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("Unauthorized request - missing or invalid user ID in token.");
                return Unauthorized("Brak ID użytkownika w tokenie.");
            }

            try
            {
                var workplace = await _dbContext.Workplaces
                    .Include(w => w.UserWorkplaces)
                    .ThenInclude(uw => uw.User)
                    .FirstOrDefaultAsync(w => w.Id == workplaceId);

                if (workplace == null)
                {
                    _logger.LogWarning("Workplace not found for ID: {WorkplaceId}", workplaceId);
                    return NotFound("Nie znaleziono miejsca pracy.");
                }

                var userWorkplace = workplace.UserWorkplaces.FirstOrDefault(uw => uw.UserId == userId);
                if (userWorkplace == null || (userWorkplace.Role != "manager" && workplace.OwnerId != userId))
                {
                    _logger.LogWarning("Forbidden request - user does not have permission to create work groups.");
                    return Forbid("Tylko właściciel i manager mogą tworzyć grupy robocze.");
                }

                var workGroup = new WorkGroup
                {
                    Name = request.Name,
                    Description = request.Description,
                    WorkplaceId = workplaceId,
                    UserWorkGroups = new List<UserWorkGroup>()
                };

                _dbContext.WorkGroups.Add(workGroup);
                await _dbContext.SaveChangesAsync();

                var workGroupDto = new
                {
                    workGroup.Id,
                    workGroup.Name,
                    workGroup.Description,
                    Members = new List<object>()
                };

                _logger.LogInformation("Work group created successfully for workplace ID: {WorkplaceId}, WorkGroup ID: {WorkGroupId}", workplaceId, workGroup.Id);
                return Ok(workGroupDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating a work group.");
                return StatusCode(500, "Wystąpił błąd podczas tworzenia grupy roboczej.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWorkGroups(int workplaceId)
        {
            _logger.LogInformation("Fetching work groups for workplace ID: {WorkplaceId}", workplaceId);

            var workGroups = await _dbContext.WorkGroups
                .Where(wg => wg.WorkplaceId == workplaceId)
                .Include(wg => wg.UserWorkGroups)
                .ThenInclude(uwg => uwg.User)
                .ThenInclude(u => u.Employee) 
                .ToListAsync();

            var workGroupDtos = workGroups.Select(wg => new
            {
                wg.Id,
                wg.Name,
                wg.Description,
                Members = wg.UserWorkGroups.Select(uwg => new
                {
                    uwg.User.Id,
                    uwg.User.Employee.FirstName,
                    uwg.User.Employee.LastName
                }).ToList() 
            }).ToList();

            if (workGroupDtos == null || !workGroupDtos.Any())
            {
                _logger.LogWarning("No work groups found for workplace ID: {WorkplaceId}", workplaceId);
                return Ok(new List<object>()); 
            }

            _logger.LogInformation("Work groups fetched successfully for workplace ID: {WorkplaceId}", workplaceId);
            return Ok(workGroupDtos);
        }


        [HttpGet("{workGroupId}/search-user")]
        public async Task<IActionResult> SearchUser(int workplaceId, int workGroupId, string lastName)
        {
            var existingUserIds = await _dbContext.UserWorkGroups
                .Where(uwg => uwg.WorkGroupId == workGroupId)
                .Select(uwg => uwg.UserId)
                .ToListAsync();

            var users = await _dbContext.UserWorkplaces
                .Where(uw => uw.WorkplaceId == workplaceId && !existingUserIds.Contains(uw.UserId))
                .Include(uw => uw.User)
                .ThenInclude(u => u.Employee)
                .Where(u => u.User.Employee.LastName.Contains(lastName))
                .Select(u => new { u.User.Id, u.User.Employee.FirstName, u.User.Employee.LastName })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("{workGroupId}/add-user")]
        public async Task<IActionResult> AddUserToWorkGroup(int workplaceId, int workGroupId, [FromBody] AddUserToWorkGroupRequest request)
        {
            _logger.LogInformation("Attempting to add user to work group. Workplace ID: {WorkplaceId}, WorkGroup ID: {WorkGroupId}", workplaceId, workGroupId);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                _logger.LogWarning("Unauthorized request - no user ID in token.");
                return Unauthorized("Brak ID użytkownika w tokenie.");
            }

            if (!int.TryParse(userIdClaim.Value, out var userId))
            {
                _logger.LogWarning("Unauthorized request - invalid user ID format.");
                return Unauthorized("Nieprawidłowy format ID użytkownika.");
            }

            var workplace = await _dbContext.Workplaces
                .Include(w => w.UserWorkplaces)
                .FirstOrDefaultAsync(w => w.Id == workplaceId);

            if (workplace == null)
            {
                _logger.LogWarning("Workplace not found for ID: {WorkplaceId}", workplaceId);
                return NotFound("Nie znaleziono miejsca pracy.");
            }

            var userWorkplace = workplace.UserWorkplaces.FirstOrDefault(uw => uw.UserId == userId);
            if (userWorkplace == null || (userWorkplace.Role != "manager" && workplace.OwnerId != userId))
            {
                _logger.LogWarning("Forbidden request - user does not have permission to add users to work groups.");
                return Forbid("Tylko właściciel i manager mogą dodawać użytkowników do grup roboczych.");
            }

            var workGroup = await _dbContext.WorkGroups
                .Include(wg => wg.UserWorkGroups)
                .FirstOrDefaultAsync(wg => wg.Id == workGroupId);

            if (workGroup == null || workGroup.WorkplaceId != workplaceId)
            {
                _logger.LogWarning("Work group not found for ID: {WorkGroupId} in workplace ID: {WorkplaceId}", workGroupId, workplaceId);
                return NotFound("Nie znaleziono grupy roboczej.");
            }

            var existingUser = workGroup.UserWorkGroups.FirstOrDefault(uwg => uwg.UserId == request.UserId);
            if (existingUser != null)
            {
                _logger.LogWarning("User ID: {UserId} already in work group ID: {WorkGroupId}", request.UserId, workGroupId);
                return Conflict("Użytkownik już jest w grupie roboczej.");
            }

            var userWorkGroup = new UserWorkGroup
            {
                UserId = request.UserId,
                WorkGroupId = workGroupId
            };

            workGroup.UserWorkGroups.Add(userWorkGroup);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("User added to work group successfully. Workplace ID: {WorkplaceId}, WorkGroup ID: {WorkGroupId}", workplaceId, workGroupId);
            return Ok();
        }
        [HttpPost("{workGroupId}/remove-user")]
        public async Task<IActionResult> RemoveUserFromWorkGroup(int workplaceId, int workGroupId, [FromBody] RemoveUserFromWorkGroupRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized("Brak ID użytkownika w tokenie.");
            }

            if (!int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("Nieprawidłowy format ID użytkownika.");
            }

            var workplace = await _dbContext.Workplaces
                .Include(w => w.UserWorkplaces)
                .FirstOrDefaultAsync(w => w.Id == workplaceId);

            if (workplace == null)
            {
                return NotFound("Nie znaleziono miejsca pracy.");
            }

            var userWorkplace = workplace.UserWorkplaces.FirstOrDefault(uw => uw.UserId == userId);
            if (userWorkplace == null || (userWorkplace.Role != "manager" && workplace.OwnerId != userId))
            {
                return Forbid("Tylko właściciel i manager mogą zarządzać członkami grup roboczych.");
            }

            var workGroup = await _dbContext.WorkGroups
                .Include(wg => wg.UserWorkGroups)
                .FirstOrDefaultAsync(wg => wg.Id == workGroupId);

            if (workGroup == null || workGroup.WorkplaceId != workplaceId)
            {
                return NotFound("Nie znaleziono grupy roboczej.");
            }

            var userWorkGroup = workGroup.UserWorkGroups.FirstOrDefault(uwg => uwg.UserId == request.UserId);
            if (userWorkGroup == null)
            {
                return NotFound("Użytkownik nie jest członkiem grupy roboczej.");
            }

            _dbContext.UserWorkGroups.Remove(userWorkGroup);
            await _dbContext.SaveChangesAsync();

            return Ok("Użytkownik został usunięty z grupy roboczej.");
        }
        [Authorize]
        [HttpDelete("{workGroupId}")]
        public async Task<IActionResult> DeleteWorkGroup(int workplaceId, int workGroupId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { message = "Brak ID użytkownika w tokenie." });
            }

            if (!int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "Nieprawidłowy format ID użytkownika." });
            }

            var workplace = await _dbContext.Workplaces
                .Include(w => w.UserWorkplaces)
                .FirstOrDefaultAsync(w => w.Id == workplaceId);

            if (workplace == null)
            {
                return NotFound(new { message = "Nie znaleziono miejsca pracy." });
            }

            var userWorkplace = workplace.UserWorkplaces.FirstOrDefault(uw => uw.UserId == userId);
            if (userWorkplace == null || (userWorkplace.Role != "manager" && workplace.OwnerId != userId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Tylko właściciel i manager mogą usuwać grupy robocze." });
            }

            var workGroup = await _dbContext.WorkGroups
                .Include(wg => wg.Tasks)
                .FirstOrDefaultAsync(wg => wg.Id == workGroupId);

            if (workGroup == null || workGroup.WorkplaceId != workplaceId)
            {
                return NotFound(new { message = "Nie znaleziono grupy roboczej." });
            }

            if (workGroup.Tasks.Any())
            {
                return BadRequest(new { message = "Grupa robocza ma przypisane zadania i nie może być usunięta." });
            }

            _dbContext.WorkGroups.Remove(workGroup);
            await _dbContext.SaveChangesAsync();

            return Ok(new { message = "Grupa robocza została usunięta." });
        }
    }
}