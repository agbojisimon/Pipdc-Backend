using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using PIPDC.API.Extensions;
using PIPDC.Application.Developments;

namespace PIPDC.API.Controllers;

[Authorize]
[ApiController]
[Route("api/development-tracking")]
public class DevelopmentTrackingController(IDevelopmentTrackingService trackingService) : ControllerBase
{
    private string CurrentUserId => User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;

    [HttpGet]
    public async Task<IActionResult> GetTracked([FromQuery] DevelopmentProjectQueryParameters queryParams, CancellationToken ct)
    {
        var result = await trackingService.GetTrackedAsync(CurrentUserId, queryParams, ct);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Track([FromBody] TrackProjectRequest request, CancellationToken ct)
    {
        var result = await trackingService.TrackAsync(CurrentUserId, request.ProjectId, request.UnitId, ct);
        return result.ToActionResult();
    }

    [HttpDelete("{projectId:int}")]
    public async Task<IActionResult> StopTracking(int projectId, CancellationToken ct)
    {
        var result = await trackingService.StopTrackingAsync(CurrentUserId, projectId, ct);
        return result.ToActionResult();
    }
}
