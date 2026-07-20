using Microsoft.EntityFrameworkCore;
using PIPDC.Application.Common;
using PIPDC.Application.Data;
using PIPDC.Domain.Common;
using PIPDC.Domain.Entities;
using PIPDC.Domain.Enums;

namespace PIPDC.Application.Enquiries;

public class EnquiryService(IAppDbContext dbContext) : IEnquiryService
{
    public async Task<Result<PaginatedResult<EnquiryDto>>> GetAllAsync(EnquiryQueryParameters q, CancellationToken ct)
    {
        IQueryable<Enquiry> query = dbContext.Enquiries;

        if (!string.IsNullOrWhiteSpace(q.Keyword))
        {
            var keyword = q.Keyword.ToLower();
            query = query.Where(e => e.FullName.ToLower().Contains(keyword)
                                  || e.Email.ToLower().Contains(keyword)
                                  || e.Message.ToLower().Contains(keyword));
        }

        if (Enum.TryParse<EnquiryStatus>(q.Status, true, out var status))
            query = query.Where(e => e.Status == status);

        if (q.PropertyId.HasValue)
            query = query.Where(e => e.PropertyId == q.PropertyId.Value);

        var totalCount = await query.CountAsync(ct);

        query = q.SortBy?.ToLower() switch
        {
            "status" => q.SortDescending ? query.OrderByDescending(e => e.Status)
                                         : query.OrderBy(e => e.Status),
            _ => q.SortDescending ? query.OrderByDescending(e => e.CreatedAt)
                                  : query.OrderBy(e => e.CreatedAt)
        };

        var items = await query
            .Skip((q.PageNumber - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(e => new EnquiryDto(
                e.Id,
                e.FullName,
                e.Email,
                e.Phone,
                e.Message,
                e.Status.ToString(),
                e.PropertyId,
                e.Property.Title,
                e.UserId,
                e.CreatedAt,
                e.UpdatedAt))
            .ToListAsync(ct);

        return Result<PaginatedResult<EnquiryDto>>.Success(
            PaginatedResult<EnquiryDto>.Create(items, totalCount, q.PageNumber, q.PageSize));
    }

    public async Task<Result<PaginatedResult<EnquiryDto>>> GetMineAsync(string userId, EnquiryQueryParameters q, CancellationToken ct)
    {
        IQueryable<Enquiry> query = dbContext.Enquiries.Where(e => e.UserId == userId);

        if (!string.IsNullOrWhiteSpace(q.Keyword))
        {
            var keyword = q.Keyword.ToLower();
            query = query.Where(e => e.FullName.ToLower().Contains(keyword)
                                  || e.Email.ToLower().Contains(keyword)
                                  || e.Message.ToLower().Contains(keyword));
        }

        if (Enum.TryParse<EnquiryStatus>(q.Status, true, out var status))
            query = query.Where(e => e.Status == status);

        if (q.PropertyId.HasValue)
            query = query.Where(e => e.PropertyId == q.PropertyId.Value);

        var totalCount = await query.CountAsync(ct);

        query = q.SortBy?.ToLower() switch
        {
            "status" => q.SortDescending ? query.OrderByDescending(e => e.Status)
                                         : query.OrderBy(e => e.Status),
            _ => q.SortDescending ? query.OrderByDescending(e => e.CreatedAt)
                                  : query.OrderBy(e => e.CreatedAt)
        };

        var items = await query
            .Skip((q.PageNumber - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(e => new EnquiryDto(
                e.Id,
                e.FullName,
                e.Email,
                e.Phone,
                e.Message,
                e.Status.ToString(),
                e.PropertyId,
                e.Property.Title,
                e.UserId,
                e.CreatedAt,
                e.UpdatedAt))
            .ToListAsync(ct);

        return Result<PaginatedResult<EnquiryDto>>.Success(
            PaginatedResult<EnquiryDto>.Create(items, totalCount, q.PageNumber, q.PageSize));
    }

    public async Task<Result<EnquiryDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var enquiry = await dbContext.Enquiries
            .Include(e => e.Property)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (enquiry is null)
            return Result<EnquiryDto>.Failure(
                Error.NotFound("enquiry.notfound", $"Enquiry with id {id} was not found."));

        return Result<EnquiryDto>.Success(enquiry.ToDto());
    }

    public async Task<Result<EnquiryDto>> CreateAsync(CreateEnquiryRequest request, string? currentUserId, CancellationToken ct)
    {
        if (!await dbContext.Properties.AnyAsync(p => p.Id == request.PropertyId, ct))
            return Result<EnquiryDto>.Failure(
                Error.Validation("enquiry.invalidproperty", $"Property with id {request.PropertyId} does not exist."));

        var enquiry = new Enquiry
        {
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Message = request.Message,
            Status = EnquiryStatus.Pending,
            PropertyId = request.PropertyId,
            UserId = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Enquiries.Add(enquiry);
        await dbContext.SaveChangesAsync(ct);

        var created = await dbContext.Enquiries
            .Include(e => e.Property)
            .FirstAsync(e => e.Id == enquiry.Id, ct);

        return Result<EnquiryDto>.Success(created.ToDto());
    }

    public async Task<Result<EnquiryDto>> UpdateAsync(int id, UpdateEnquiryRequest request, CancellationToken ct)
    {
        var enquiry = await dbContext.Enquiries
            .Include(e => e.Property)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (enquiry is null)
            return Result<EnquiryDto>.Failure(
                Error.NotFound("enquiry.notfound", $"Enquiry with id {id} was not found."));

        if (!Enum.TryParse<EnquiryStatus>(request.Status, true, out var status))
            return Result<EnquiryDto>.Failure(
                Error.Validation("enquiry.invalidstatus", $"'{request.Status}' is not a valid enquiry status."));

        enquiry.FullName = request.FullName;
        enquiry.Email = request.Email;
        enquiry.Phone = request.Phone;
        enquiry.Message = request.Message;
        enquiry.Status = status;
        enquiry.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        return Result<EnquiryDto>.Success(enquiry.ToDto());
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        var enquiry = await dbContext.Enquiries.FindAsync([id], ct);

        if (enquiry is null)
            return Result.Failure(
                Error.NotFound("enquiry.notfound", $"Enquiry with id {id} was not found."));

        dbContext.Enquiries.Remove(enquiry);
        await dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }
}
