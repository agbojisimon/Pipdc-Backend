using PIPDC.Domain.Common;

namespace PIPDC.Application.Dashboard;

public interface IDashboardService
{
    Task<Result<AdminDashboardDto>> GetAdminAsync(string currentUserId, IList<string> currentUserRoles, CancellationToken ct);
    Task<Result<AgentDashboardDto>> GetAgentAsync(string currentUserId, IList<string> currentUserRoles, CancellationToken ct);
    Task<Result<ClientDashboardDto>> GetClientAsync(string currentUserId, CancellationToken ct);
}
