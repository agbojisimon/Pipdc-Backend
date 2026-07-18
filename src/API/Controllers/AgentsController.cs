using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PIPDC.API.Extensions;
using PIPDC.Application.Agents;
using PIPDC.Application.Auth;

namespace PIPDC.API.Controllers;

[ApiController]
[Route("api/agents")]
public class AgentsController(IAgentService agentService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] AgentQueryParameters queryParams, CancellationToken ct)
    {
        var result = await agentService.GetAllAsync(queryParams, ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await agentService.GetByIdAsync(id, ct);
        return result.ToActionResult();
    }

    [Authorize(Roles = Roles.Agent)]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await agentService.GetMyProfileAsync(userId, ct);
        return result.ToActionResult();
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAgentRequest request, CancellationToken ct)
    {
        var result = await agentService.CreateAsync(request, ct);

        if (result.IsFailure)
            return result.ToActionResult();

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAgentRequest request, CancellationToken ct)
    {
        var result = await agentService.UpdateAsync(id, request, ct);
        return result.ToActionResult();
    }

    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await agentService.DeleteAsync(id, ct);
        return result.ToActionResult();
    }
}
