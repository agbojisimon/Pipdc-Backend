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
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PropertyQueryParameters queryParams, CancellationToken ct)
    {
        var result = await propertyService.GetAllAsync(queryParams, ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await propertyService.GetByIdAsync(id, ct);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Agent,Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePropertyRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;
        var roles = User.FindAll("role").Select(c => c.Value).ToList();

        var result = await propertyService.CreateAsync(request, userId, roles, ct);

        if (result.IsFailure)
            return result.ToActionResult();

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    [Authorize(Roles = "Agent,Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePropertyRequest request, CancellationToken ct)
    {
        var result = await propertyService.UpdateAsync(id, request, ct);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Agent,Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await propertyService.DeleteAsync(id, ct);
        return result.ToActionResult();
    }
}
