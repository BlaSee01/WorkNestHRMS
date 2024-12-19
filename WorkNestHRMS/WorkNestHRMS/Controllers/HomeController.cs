using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WorkNestHRMS.Models;

namespace WorkNestHRMS.Controllers;

[ApiController]
[Route("api/home")]
public class HomeController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public HomeController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [Authorize]
    [HttpGet]
    public IActionResult GetHomeData()
    {
        var username = User.Identity?.Name ?? "Gość";
        var role = User.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value ?? "Brak roli";

        return Ok(new
        {
            message = $"Witaj, {username}!",
            role,
            sections = new[]
            {
                new { title = "Profil", description = "Zarządzaj swoim profilem i danymi osobowymi." },
                new { title = "Ustawienia", description = "Dostosuj ustawienia konta i preferencje." },
                new { title = "Twoje miejsce pracy", description = "Zarządzanie zadaniami i zespołem." },
                new { title = "Urlopy", description = "Planuj i zarządzaj swoimi urlopami." },
                new { title = "Finanse", description = "Przegląd wynagrodzenia i innych informacji finansowych." }
            }
        });
    }


  
}
