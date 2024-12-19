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

    private UserDto MapToUserDto(User user)
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
            Description = workplace.Description
      
        };
    }

    // Endpoint: Utworzenie nowego miejsca pracy
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
            OwnerId = userId // Właściciel przypisany na podstawie ID
        };

        _dbContext.Workplaces.Add(newWorkplace);
        await _dbContext.SaveChangesAsync();

        // Dodanie właściciela do tabeli UserWorkplace
        var userWorkplace = new UserWorkplace
        {
            UserId = userId,
            WorkplaceId = newWorkplace.Id,
            Role = "owner"
        };

        _dbContext.UserWorkplaces.Add(userWorkplace);
        await _dbContext.SaveChangesAsync();

        

        var workplaceWithOwner = await _dbContext.Workplaces    // DEBUG START
            .Include(w => w.UserWorkplaces)  // Załaduj UserWorkplaces
            .ThenInclude(uw => uw.User)  // Załaduj użytkowników w UserWorkplaces
            .FirstOrDefaultAsync(w => w.Id == newWorkplace.Id);     // DEBUG end

        return Ok(new { message = "Miejsce pracy zostało utworzone.", workplace = workplaceWithOwner });

    }


    // Endpoint: Pobierz miejsca pracy użytkownika
    [Authorize]
    [HttpGet("my")]
    public async Task<IActionResult> GetUserWorkplaces()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (int.TryParse(userId, out var userIntId))
        {
            var workplaces = await _dbContext.UserWorkplaces
                .Where(uw => uw.UserId == userIntId)  // Dopasowanie po UserId
                .Include(uw => uw.Workplace)  // Wczytanie Workplace
                .Include(uw => uw.Workplace.Owner)  // Wczytanie Owner powiązanego z Workplace
                .ToListAsync();


            var workplaceDtos = workplaces.Select(uw => MapToWorkplaceDto(uw.Workplace)).ToList();
            return Ok(workplaceDtos);
        }
        else
        {
            return BadRequest("Invalid user ID format.");
        }

    }

    [Authorize]
    [HttpGet("{id}")]   // ścieżka przyjmuje ID miejsca pracy (wazne w kontekście implementacji frontendu, bo klikniety kafelek będzie wymagał id (może onClick pobierający id przedsiębiorstwa?))
    public async Task<IActionResult> GetWorkplaceDetails(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized("Brak ID użytkownika w tokenie.");

        if (!int.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized("Nieprawidłowy format ID użytkownika.");

        // Znajdź miejsce pracy
        var workplace = await _dbContext.Workplaces
            .Include(w => w.UserWorkplaces)
                .ThenInclude(uw => uw.User)
            .FirstOrDefaultAsync(w => w.Id == id);

        if (workplace == null)
            return NotFound("Nie znaleziono miejsca pracy.");

        // Sprawdź, czy użytkownik ma dostęp
        var userWorkplace = workplace.UserWorkplaces.FirstOrDefault(uw => uw.UserId == userId);
        if (userWorkplace == null)
            return Forbid("Nie masz dostępu do tego miejsca pracy.");

        var workplaceDto = new
        {
            workplace.Id,
            workplace.Name,
            workplace.Description,
            UserRole = userWorkplace.Role, // "owner" lub "member"
            Members = workplace.UserWorkplaces.Select(uw => new
            {
                uw.User.Id,
                uw.User.Username,
                uw.Role
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

        // Sprawdzenie, czy użytkownik jest właścicielem
        var userWorkplace = workplace.UserWorkplaces.FirstOrDefault(uw => uw.UserId == userId && uw.Role == "owner");
        if (userWorkplace == null)
            return Forbid("Nie masz uprawnień do dodawania pracowników w tym miejscu pracy.");

        // Jeśli query jest puste, zwróć wszystkich użytkowników niepowiązanych z miejscem pracy
        var users = await _dbContext.Users
            .Where(u => string.IsNullOrEmpty(query) || u.Username.Contains(query))  // Obsługa pustego query
            .Where(u => !_dbContext.UserWorkplaces.Any(uw => uw.UserId == u.Id && uw.WorkplaceId == id))
            .Select(u => new UserDto { Id = u.Id, Username = u.Username })
            .ToListAsync();
        Console.WriteLine($"Query parameter received: {query}"); // DEBUG

        return Ok(users);

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

        // Sprawdzenie, czy użytkownik jest właścicielem
        var userWorkplace = workplace.UserWorkplaces.FirstOrDefault(uw => uw.UserId == userId && uw.Role == "owner");
        if (userWorkplace == null)
            return Forbid("Nie masz uprawnień do dodawania pracowników w tym miejscu pracy.");

        // Sprawdzenie, czy użytkownik do dodania istnieje
        var userToAdd = await _dbContext.Users.FindAsync(userIdToAdd);
        if (userToAdd == null)
            return NotFound("Nie znaleziono użytkownika do dodania.");

        // Sprawdzenie, czy użytkownik już należy do miejsca pracy
        var existingRelation = await _dbContext.UserWorkplaces
            .FirstOrDefaultAsync(uw => uw.UserId == userIdToAdd && uw.WorkplaceId == id);
        if (existingRelation != null)
            return BadRequest("Użytkownik jest już przypisany do tego miejsca pracy.");

        // Dodanie użytkownika do miejsca pracy
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

}
