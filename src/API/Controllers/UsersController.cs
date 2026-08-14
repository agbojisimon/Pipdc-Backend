using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PIPDC.API.Extensions;
using PIPDC.Application.Auth;
using PIPDC.Application.Users;

namespace PIPDC.API.Controllers;

[Authorize(Roles = Roles.Admin)]
[ApiController]
[Route("api/users")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] UserQueryParameters queryParams, CancellationToken ct)
    {
        var result = await userService.GetAllAsync(queryParams, ct);
        return result.ToActionResult();
    }
}
