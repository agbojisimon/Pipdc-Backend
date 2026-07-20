using PIPDC.Application.Common;
using PIPDC.Domain.Common;

namespace PIPDC.Application.Enquiries;

public interface IEnquiryService
{
    Task<Result<PaginatedResult<EnquiryDto>>> GetAllAsync(EnquiryQueryParameters queryParams, CancellationToken ct);
    Task<Result<PaginatedResult<EnquiryDto>>> GetMineAsync(string userId, EnquiryQueryParameters queryParams, CancellationToken ct);
    Task<Result<EnquiryDto>> GetByIdAsync(int id, CancellationToken ct);
    Task<Result<EnquiryDto>> CreateAsync(CreateEnquiryRequest request, string? currentUserId, CancellationToken ct);
    Task<Result<EnquiryDto>> UpdateAsync(int id, UpdateEnquiryRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
}
