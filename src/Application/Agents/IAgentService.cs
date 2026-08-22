using PIPDC.Application.Common;
using PIPDC.Domain.Common;

namespace PIPDC.Application.Agents;

public interface IAgentService
{
    Task<Result<PaginatedResult<AgentDto>>> GetAllAsync(AgentQueryParameters queryParams, CancellationToken ct);
    Task<Result<AgentDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<AgentDto>> GetMyProfileAsync(string userId, CancellationToken ct);
    Task<Result<AgentDto>> CreateAsync(CreateAgentRequest request, CancellationToken ct);
    Task<Result<AgentDto>> UpdateAsync(int id, UpdateAgentRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
    Task<Result<AgentDto>> ToggleVerificationAsync(int agentId, CancellationToken ct);
    Task<Result<AgentSummaryDto>> GetSummaryAsync(int agentId, CancellationToken ct);
}
