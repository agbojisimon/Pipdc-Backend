using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PIPDC.Application.Auth;

namespace PIPDC.API.Controllers;

[ApiController]
[Route("api/secured")]
public class SecuredController : ControllerBase
{
    [Authorize]
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = $"Hello {User.Identity?.Name}, you are authenticated." });
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpGet("admin")]
    public IActionResult Admin()
    {
        return Ok(new { message = "Hello Admin, you have access to this resource." });
    }
}
