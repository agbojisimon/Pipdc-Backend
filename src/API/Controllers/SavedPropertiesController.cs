using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using PIPDC.API.Extensions;
using PIPDC.Application.SavedProperties;

namespace PIPDC.API.Controllers;

[Authorize]
[ApiController]
[Route("api/saved-properties")]
public class SavedPropertiesController(ISavedPropertyService savedPropertyService) : ControllerBase
{
    private string CurrentUserId => User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;

    [HttpGet]
    public async Task<IActionResult> GetSaved([FromQuery] SavedPropertyQueryParameters queryParams, CancellationToken ct)
    {
        var result = await savedPropertyService.GetSavedAsync(CurrentUserId, queryParams, ct);
        return result.ToActionResult();
    }

    [HttpGet("ids")]
    public async Task<IActionResult> GetSavedIds(CancellationToken ct)
    {
        var result = await savedPropertyService.GetSavedIdsAsync(CurrentUserId, ct);
        return result.ToActionResult();
    }

    [HttpPost("{propertyId:int}")]
    public async Task<IActionResult> Save(int propertyId, CancellationToken ct)
    {
        var result = await savedPropertyService.SaveAsync(CurrentUserId, propertyId, ct);
        return result.ToActionResult();
    }

    [HttpDelete("{propertyId:int}")]
    public async Task<IActionResult> Unsave(int propertyId, CancellationToken ct)
    {
        var result = await savedPropertyService.UnsaveAsync(CurrentUserId, propertyId, ct);
        return result.ToActionResult();
    }
}
