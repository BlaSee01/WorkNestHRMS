using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WorkNestHRMS.Models;

namespace WorkNestHRMS.Controllers
{
    [ApiController]
    [Route("api/user-tasks")]
    public class UserTasksController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public UserTasksController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetTasksForLoggedUser()
        {
            try
            {

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    Console.WriteLine("Nie udało się pobrać ID użytkownika.");
                    return NotFound("Nie udało się pobrać ID użytkownika.");
                }

                var workplaceIds = await _dbContext.UserWorkplaces
                    .Where(uw => uw.UserId.ToString() == userId) // jak nie zadziała to do int z powrotem i dalej parsować tam gdzie "+" TEMPP
                    .Select(uw => uw.WorkplaceId)
                    .ToListAsync();

                if (!workplaceIds.Any())
                {
                    Console.WriteLine("Użytkownik nie jest przypisany do żadnych miejsc pracy.");
                    return NotFound("Użytkownik nie jest przypisany do żadnych miejsc pracy.");
                }

                var userGroupIds = await _dbContext.UserWorkGroups
                    .Where(uwg => uwg.UserId.ToString() == userId) 
                    .Select(uwg => uwg.WorkGroupId)
                    .ToListAsync();

                var tasks = await _dbContext.Tasks
                    .Where(t => workplaceIds.Contains(t.WorkplaceId) &&
                                (t.AssignedUserId.ToString() == userId || 
                                 (t.AssignedWorkGroupId.HasValue && userGroupIds.Contains(t.AssignedWorkGroupId.Value))))   // +
                    .Include(t => t.AssignedUser)
                        .ThenInclude(u => u.Employee)
                    .Include(t => t.AssignedWorkGroup)
                    .Include(t => t.Workplace)
                    .ToListAsync();
               
                var response = tasks.Select(task => new
                {
                    task.Id,
                    task.Content,
                    task.DueDate,
                    task.Status,
                    task.Priority,
                    Attachments = task.Attachments,
                    WorkplaceName = task.Workplace?.Name ?? "Nieznane",
                    AssignedUser = task.AssignedUser != null
                        ? new
                        {
                            Id = task.AssignedUser.Id,
                            FirstName = task.AssignedUser.Employee.FirstName,
                            LastName = task.AssignedUser.Employee.LastName
                        }
                        : null,
                    AssignedWorkGroup = task.AssignedWorkGroup != null
                        ? new
                        {
                            Id = task.AssignedWorkGroup.Id,
                            Name = task.AssignedWorkGroup.Name
                        }
                        : null
                });

                return Ok(response);
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Błąd formatu: {ex.Message}");
                return BadRequest("Token zawiera niepoprawne znaki Base64.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Wystąpił błąd podczas pobierania zadań: {ex.Message}");
                return StatusCode(500, $"Wystąpił błąd podczas pobierania zadań: {ex.Message}");
            }
        }
    }
}
