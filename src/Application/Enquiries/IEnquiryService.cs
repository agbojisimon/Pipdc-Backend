using PIPDC.Application.Common;
using PIPDC.Domain.Common;

namespace PIPDC.Application.Enquiries;

public interface IEnquiryService
{
    Task<Result<PaginatedResult<EnquiryDto>>> GetAllAsync(EnquiryQueryParameters queryParams, string currentUserId, IList<string> currentUserRoles, CancellationToken ct);
    Task<Result<PaginatedResult<EnquiryDto>>> GetMineAsync(string userId, EnquiryQueryParameters queryParams, CancellationToken ct);
    Task<Result<EnquiryDto>> GetByIdAsync(int id, string currentUserId, IList<string> currentUserRoles, CancellationToken ct);
    Task<Result<PaginatedResult<EnquiryDto>>> GetByPropertyAsync(int propertyId, EnquiryQueryParameters queryParams, string currentUserId, IList<string> currentUserRoles, CancellationToken ct);
    Task<Result<EnquiryDto>> CreateAsync(CreateEnquiryRequest request, string currentUserId, CancellationToken ct);
    Task<Result<EnquiryDto>> UpdateAsync(int id, UpdateEnquiryRequest request, string currentUserId, IList<string> currentUserRoles, CancellationToken ct);
    Task<Result> DeleteAsync(int id, string currentUserId, IList<string> currentUserRoles, CancellationToken ct);
    Task<Result<PaginatedResult<AgentEnquirySummaryDto>>> GetAgentSummariesAsync(EnquiryQueryParameters queryParams, CancellationToken ct);
    Task<Result<PaginatedResult<EnquiryDto>>> GetByAgentAsync(int agentId, EnquiryQueryParameters queryParams, CancellationToken ct);
    Task<Result<AgentNotifyResultDto>> NotifyAgentAsync(int id, CancellationToken ct);
}
