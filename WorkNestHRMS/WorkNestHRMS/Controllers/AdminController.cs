using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WorkNestHRMS.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("data")]
    public IActionResult GetAdminData()
    {
        return Ok(new
        {
            Message = "Dane dla administratora",
            DateTime = DateTime.UtcNow
        });
    }
}
