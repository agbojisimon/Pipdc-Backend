using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using PIPDC.API.Extensions;
using PIPDC.Application.Enquiries;

namespace PIPDC.API.Controllers;

[ApiController]
[Route("api/enquiries")]
public class EnquiriesController(IEnquiryService enquiryService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEnquiryRequest request, CancellationToken ct)
    {
        string? userId = null;

        if (User.Identity?.IsAuthenticated == true)
            userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        var result = await enquiryService.CreateAsync(request, userId, ct);

        if (result.IsFailure)
            return result.ToActionResult();

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    [Authorize(Roles = "Agent,Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] EnquiryQueryParameters queryParams, CancellationToken ct)
    {
        var result = await enquiryService.GetAllAsync(queryParams, ct);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine([FromQuery] EnquiryQueryParameters queryParams, CancellationToken ct)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)!;
        var result = await enquiryService.GetMineAsync(userId, queryParams, ct);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Agent,Admin")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await enquiryService.GetByIdAsync(id, ct);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Agent,Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEnquiryRequest request, CancellationToken ct)
    {
        var result = await enquiryService.UpdateAsync(id, request, ct);
        return result.ToActionResult();
    }

    [Authorize(Roles = "Agent,Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await enquiryService.DeleteAsync(id, ct);
        return result.ToActionResult();
    }
}
