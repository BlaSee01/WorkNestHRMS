using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WorkNestHRMS.Models;

namespace WorkNestHRMS.Controllers;

[ApiController]
[Route("api/workplaces")]


public class WorkplaceController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public WorkplaceController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private UserDto MapToUserDto(User user)     // jeszce zostaw 
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username
        };
    }

    private WorkplaceDto MapToWorkplaceDto(Workplace workplace) 
    {
        return new WorkplaceDto
        {
            Id = workplace.Id,
            Name = workplace.Name,
            Description = workplace.Description,
            Address = workplace.Address,
            PostalCode = workplace.PostalCode,
            Email = workplace.Email,
            Phone = workplace.Phone

        };
    }

    [Authorize]
    [HttpPost("create")]
    public async Task<IActionResult> CreateWorkplace([FromBody] WorkplaceDto workplaceDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized("Brak ID użytkownika w tokenie.");

        Console.WriteLine($"Odczytane ID z tokena: {userIdClaim.Value}");   // DEBUG czy pobieramy "id" przez claima "NameIdentifier"

        if (!int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized("Nieprawidłowy format ID użytkownika.");

        var user = await _dbContext.Users.FindAsync(userId);

        if (user == null)
            return Unauthorized("Nie znaleziono użytkownika.");

        var newWorkplace = new Workplace
        {
            Name = workplaceDto.Name,
            Description = workplaceDto.Description,
            Address = workplaceDto.Address,
            PostalCode = workplaceDto.PostalCode,
            Email = workplaceDto.Email,
            Phone = workplaceDto.Phone,
            OwnerId = userId
        };

        _dbContext.Workplaces.Add(newWorkplace);
        await _dbContext.SaveChangesAsync();

        var userWorkplace = new UserWorkplace
        {
            UserId = userId,
            WorkplaceId = newWorkplace.Id,
            Role = "owner"
        };

        _dbContext.UserWorkplaces.Add(userWorkplace);
        await _dbContext.SaveChangesAsync();

        

        var workplaceWithOwner = await _dbContext.Workplaces   
            .Include(w => w.UserWorkplaces)  
            .ThenInclude(uw => uw.User)  
            .FirstOrDefaultAsync(w => w.Id == newWorkplace.Id);     

        return Ok(new { message = "Miejsce pracy zostało utworzone.", workplace = workplaceWithOwner });

    }

    [Authorize]
    [HttpGet("my")]
    public async Task<IActionResult> GetUserWorkplaces()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized("Brak ID użytkownika w tokenie.");

        if (!int.TryParse(userIdClaim.Value, out var userIntId))
            return Unauthorized("Nieprawidłowy format ID użytkownika.");

        var workplaces = await _dbContext.UserWorkplaces
            .Where(uw => uw.UserId == userIntId)  
            .Include(uw => uw.Workplace)  
            .ThenInclude(w => w.Owner)  
            .ToListAsync();

        var workplaceDtos = workplaces.Select(uw => new
        {
            uw.Workplace.Id,
            uw.Workplace.Name,
            uw.Workplace.Description,
            OwnerId = uw.Workplace.OwnerId
        }).ToList();

        return Ok(workplaceDtos);
    }

    [Authorize]
    [HttpGet("{id}")]   // ścieżka przyjmuje ID miejsca pracy (wazne w kontekście implementacji frontendu, bo klikniety kafelek będzie wymagał id (może onClick pobierający id przedsiębiorstwa?)) (edit1: udało się , jestem kot)
    public async Task<IActionResult> GetWorkplaceDetails(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized("Brak ID użytkownika w tokenie.");

        if (!int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized("Nieprawidłowy format ID użytkownika.");

        var workplace = await _dbContext.Workplaces
           .Include(w => w.UserWorkplaces)
               .ThenInclude(uw => uw.User)
                   .ThenInclude(u => u.Employee)
           .FirstOrDefaultAsync(w => w.Id == id);

        if (workplace == null)
            return NotFound("Nie znaleziono miejsca pracy.");

        var userWorkplace = workplace.UserWorkplaces.FirstOrDefault(uw => uw.UserId == userId);
        if (userWorkplace == null)
            return Forbid("Nie masz dostępu do tego miejsca pracy.");

        var workplaceDto = new
        {
            workplace.Id,
            workplace.Name,
            workplace.Description,
            workplace.Address,
            workplace.PostalCode,
            workplace.Email,
            workplace.Phone,
            workplace.OwnerId,
            Members = workplace.UserWorkplaces.Select(uw => new
            {
                uw.Role,
                uw.UserId, 
                Employee = uw.User.Employee == null ? null : new
                {
                    //uw.User.Employee.Id,
                    uw.User.Employee.FirstName,
                    uw.User.Employee.LastName,
                    uw.User.Employee.Email,
                    uw.User.Employee.PhoneNumber,
                    uw.User.Employee.Address
                }
            })
        };

        return Ok(workplaceDto);
    }
    [Authorize]
    [HttpGet("{id}/search-users")]
    public async Task<IActionResult> SearchUsers(int id, [FromQuery] string query = "")
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized("Brak ID użytkownika w tokenie.");

        if (!int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized("Nieprawidłowy format ID użytkownika.");

        var workplace = await _dbContext.Workplaces
            .Include(w => w.UserWorkplaces)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (workplace == null)
            return NotFound("Nie znaleziono miejsca pracy.");

        var userWorkplace = workplace.UserWorkplaces.FirstOrDefault(uw => uw.UserId == userId && uw.Role == "owner");
        if (userWorkplace == null)
            return Forbid("Nie masz uprawnień do dodawania pracowników w tym miejscu pracy.");

        var users = await _dbContext.Users
        .Include(u => u.Employee)
        .Where(u => string.IsNullOrEmpty(query) ||
                    (u.Employee.FirstName + " " + u.Employee.LastName).Contains(query))
        .Where(u => !_dbContext.UserWorkplaces.Any(uw => uw.UserId == u.Id && uw.WorkplaceId == id))
        .Select(u => new
        {
            u.Id,
            Employee = u.Employee == null ? null : new
            {
                u.Employee.Id,
                u.Employee.FirstName,
                u.Employee.LastName,
                u.Employee.Email,
                u.Employee.PhoneNumber,
                u.Employee.Address
            }
        })
        .ToListAsync();

        return Ok(users);

    }

    [Authorize]
    [HttpGet("{id}/search-users-workgroup")]
    public async Task<IActionResult> SearchUsersWorkGroup(int id, [FromQuery] string query = "")
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized("Brak ID użytkownika w tokenie.");

        if (!int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized("Nieprawidłowy format ID użytkownika.");

        var workplace = await _dbContext.Workplaces
            .Include(w => w.UserWorkplaces)
            .ThenInclude(uw => uw.User)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (workplace == null)
            return NotFound("Nie znaleziono miejsca pracy.");

        var users = await _dbContext.UserWorkplaces
            .Where(uw => uw.WorkplaceId == id && (string.IsNullOrEmpty(query) || (uw.User.Employee.FirstName + " " + uw.User.Employee.LastName).Contains(query)))
            .Select(uw => new
            {
                uw.User.Id,
                Employee = uw.User.Employee == null ? null : new
                {
                    uw.User.Employee.Id,
                    uw.User.Employee.FirstName,
                    uw.User.Employee.LastName,
                    uw.User.Employee.Email,
                    uw.User.Employee.PhoneNumber,
                    uw.User.Employee.Address
                }
            })
            .ToListAsync();

        return Ok(users);
    }

    [Authorize]
    [HttpGet("{id}/search-workgroups")]
    public async Task<IActionResult> SearchWorkGroups(int id, [FromQuery] string query = "")
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized("Brak ID użytkownika w tokenie.");

        if (!int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized("Nieprawidłowy format ID użytkownika.");

        var workplace = await _dbContext.Workplaces
            .Include(w => w.WorkGroups) 
            .FirstOrDefaultAsync(w => w.Id == id);

        if (workplace == null)
            return NotFound("Nie znaleziono miejsca pracy.");

        var workGroups = workplace.WorkGroups
            .Where(wg => string.IsNullOrEmpty(query) || wg.Name.Contains(query))
            .Select(wg => new
            {
                wg.Id,
                wg.Name
            })
            .ToList();

        return Ok(workGroups);
    }

    [Authorize]
    [HttpPost("{id}/add-user")]
    public async Task<IActionResult> AddUserToWorkplace(int id, [FromBody] int userIdToAdd)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized("Brak ID użytkownika w tokenie.");

        if (!int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized("Nieprawidłowy format ID użytkownika.");

        var workplace = await _dbContext.Workplaces
            .Include(w => w.UserWorkplaces)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (workplace == null)
            return NotFound("Nie znaleziono miejsca pracy.");

        var userWorkplace = workplace.UserWorkplaces.FirstOrDefault(uw => uw.UserId == userId && uw.Role == "owner");
        if (userWorkplace == null)
            return Forbid("Nie masz uprawnień do dodawania pracowników w tym miejscu pracy.");

        var userToAdd = await _dbContext.Users.FindAsync(userIdToAdd);
        if (userToAdd == null)
            return NotFound("Nie znaleziono użytkownika do dodania.");

        var existingRelation = await _dbContext.UserWorkplaces
            .FirstOrDefaultAsync(uw => uw.UserId == userIdToAdd && uw.WorkplaceId == id);
        if (existingRelation != null)
            return BadRequest("Użytkownik jest już przypisany do tego miejsca pracy.");

        var newUserWorkplace = new UserWorkplace
        {
            UserId = userIdToAdd,
            WorkplaceId = id,
            Role = "member"
        };
        _dbContext.UserWorkplaces.Add(newUserWorkplace);
        await _dbContext.SaveChangesAsync();

        return Ok("Użytkownik został dodany do miejsca pracy.");
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWorkplace(int id, [FromBody] WorkplaceDto workplaceDto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized("Brak ID użytkownika w tokenie.");

        if (!int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized("Nieprawidłowy format ID użytkownika.");

        var workplace = await _dbContext.Workplaces.FirstOrDefaultAsync(w => w.Id == id);
        if (workplace == null)
            return NotFound("Nie znaleziono miejsca pracy.");

        if (workplace.OwnerId != userId)
            return Forbid("Nie masz uprawnień do edycji tego miejsca pracy.");

        workplace.Name = workplaceDto.Name;
        workplace.Description = workplaceDto.Description;
        workplace.Address = workplaceDto.Address;
        workplace.PostalCode = workplaceDto.PostalCode;
        workplace.Email = workplaceDto.Email;
        workplace.Phone = workplaceDto.Phone;

        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Dane miejsca pracy zostały zaktualizowane." });
    }

    [HttpPut("{workplaceId}/update-role")]
    public async Task<IActionResult> UpdateRole(int workplaceId, [FromBody] UpdateRoleRequest request)
    {
        var userWorkplace = await _dbContext.UserWorkplaces
            .FirstOrDefaultAsync(uw => uw.UserId == request.UserId && uw.WorkplaceId == workplaceId);

        if (userWorkplace == null)
        {
            return NotFound("Nie znaleziono użytkownika w tym miejscu pracy.");
        }

        userWorkplace.Role = request.Role;
        await _dbContext.SaveChangesAsync();

        return Ok("Rola została zaktualizowana.");
    }
    public class UpdateRoleRequest
    {
        public int UserId { get; set; }
        public string Role { get; set; }
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWorkplace(int id)
    {
        var workplace = await _dbContext.Workplaces.FindAsync(id);
        if (workplace == null)
        {
            return NotFound(new { message = "Miejsce pracy nie zostało znalezione." });
        }

        _dbContext.Workplaces.Remove(workplace);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [Authorize]
    [HttpDelete("{workplaceId}/remove-user/{userId}")]
    public async Task<IActionResult> RemoveUserFromWorkplace(int workplaceId, int userId)
    {
        var userWorkplace = await _dbContext.UserWorkplaces
            .FirstOrDefaultAsync(uw => uw.UserId == userId && uw.WorkplaceId == workplaceId);

        if (userWorkplace == null)
        {
            return NotFound("Nie znaleziono użytkownika w tym miejscu pracy.");
        }

        _dbContext.UserWorkplaces.Remove(userWorkplace);
        await _dbContext.SaveChangesAsync();

        return Ok("Użytkownik został usunięty z miejsca pracy.");
    }

    [HttpGet("{workplaceId}/user-role")]
    public async Task<IActionResult> GetUserRole(int workplaceId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);    // może jakoś globalnei to w końcu zrobić (?)
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
        if (userWorkplace == null)
        {
            return NotFound("Nie znaleziono użytkownika w miejscu pracy.");
        }

        return Ok(new { Role = userWorkplace.Role });
    }
}