using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using PIPDC.API.Extensions;
using PIPDC.Application.Properties;

namespace PIPDC.API.Controllers;

[ApiController]
[Route("api/properties")]
public class PropertiesController(IPropertyService propertyService) : ControllerBase
{
    private string? CurrentUserId => User.Identity?.IsAuthenticated == true
        ? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
        : null;

    private IList<string> CurrentUserRoles => User.FindAll("role").Select(c => c.Value).ToList();

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PropertyQueryParameters queryParams, CancellationToken ct)
    {
        var result = await propertyService.GetAllAsync(queryParams, CurrentUserId, ct);
        return result.ToActionResult();
    }

    [HttpGet("featured")]
    public async Task<IActionResult> GetFeatured(CancellationToken ct)
    {
        var result = await propertyService.GetFeaturedAsync(CurrentUserId, ct);
        return result.ToActionResult();
    }

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
    {
        var result = await propertyService.GetBySlugAsync(slug, CurrentUserId, ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await propertyService.GetByIdAsync(id, CurrentUserId, ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}/similar")]
    public async Task<IActionResult> GetSimilar(int id, CancellationToken ct)
    {
        var result = await propertyService.GetSimilarAsync(id, CurrentUserId, ct);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Agent,Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePropertyRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;

        var result = await propertyService.CreateAsync(request, userId, CurrentUserRoles, ct);

        if (result.IsFailure)
            return result.ToActionResult();

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    [Authorize(Roles = "Agent,Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePropertyRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;
        var result = await propertyService.UpdateAsync(id, request, userId, CurrentUserRoles, ct);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}/featured")]
    public async Task<IActionResult> SetFeatured(int id, [FromBody] UpdateFeaturedRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;
        var result = await propertyService.SetFeaturedAsync(id, request.Featured, userId, CurrentUserRoles, ct);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Agent,Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;
        var result = await propertyService.DeleteAsync(id, userId, CurrentUserRoles, ct);
        return result.ToActionResult();
    }
}
