using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WorkNestHRMS.Controllers;

[Authorize(Policy = "UserOnly")]    //??
[ApiController]
[Route("api/user")]
public class UserController : ControllerBase
{
    //[Authorize(Policy = "UserOnly")]
    [HttpGet("data")]
    public IActionResult GetUserData()
    {
        return Ok(new
        {
            message = "Dane dla użytkownika",
            dateTime = DateTime.UtcNow
        });
    }
}
