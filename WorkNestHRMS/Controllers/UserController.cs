using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WorkNestHRMS.Controllers;

[Authorize(Policy = "UserOnly")]    //? (edit1: działa)
[ApiController]
[Route("api/user")]
public class UserController : ControllerBase  // kontroler nieużyWay, tylko do postmana go potrzeba mi
{
   
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
