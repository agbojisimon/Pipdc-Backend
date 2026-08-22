using Microsoft.EntityFrameworkCore;
using PIPDC.Application.Data;
using PIPDC.Domain.Common;
using PIPDC.Domain.Entities;
using PIPDC.Domain.Enums;

namespace PIPDC.Application.Developments;

public class DevelopmentUnitService(IAppDbContext dbContext) : IDevelopmentUnitService
{
    public async Task<Result<IReadOnlyList<DevelopmentUnitDto>>> GetByProjectAsync(int projectId, CancellationToken ct)
    {
        if (!await dbContext.DevelopmentProjects.AnyAsync(p => p.Id == projectId, ct))
            return Result<IReadOnlyList<DevelopmentUnitDto>>.Failure(
                Error.NotFound("development.notfound", $"Development project with id {projectId} was not found."));

        var units = await dbContext.DevelopmentUnits
            .Where(u => u.DevelopmentProjectId == projectId)
            .OrderBy(u => u.UnitIdentifier)
            .ToListAsync(ct);

        var dtos = units.Select(u => new DevelopmentUnitDto(
            u.Id, u.UnitIdentifier, u.UnitType, u.Status.ToString(),
            u.Price, u.Currency, u.Description, u.CreatedAt, u.UpdatedAt)).ToList();

        return Result<IReadOnlyList<DevelopmentUnitDto>>.Success(dtos);
    }

    public async Task<Result<DevelopmentUnitDto>> CreateAsync(int projectId, CreateDevelopmentUnitRequest request, CancellationToken ct)
    {
        if (!await dbContext.DevelopmentProjects.AnyAsync(p => p.Id == projectId, ct))
            return Result<DevelopmentUnitDto>.Failure(
                Error.NotFound("development.notfound", $"Development project with id {projectId} was not found."));

        if (await dbContext.DevelopmentUnits.AnyAsync(u => u.DevelopmentProjectId == projectId && u.UnitIdentifier == request.UnitIdentifier, ct))
            return Result<DevelopmentUnitDto>.Failure(
                Error.Conflict("unit.conflict", $"A unit with identifier '{request.UnitIdentifier}' already exists in this project."));

        var unit = new DevelopmentUnit
        {
            DevelopmentProjectId = projectId,
            UnitIdentifier = request.UnitIdentifier,
            UnitType = request.UnitType,
            Status = string.IsNullOrWhiteSpace(request.Status)
                ? DevelopmentUnitStatus.Available
                : Enum.Parse<DevelopmentUnitStatus>(request.Status, true),
            Price = request.Price,
            Currency = request.Currency ?? "NGN",
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.DevelopmentUnits.Add(unit);
        await dbContext.SaveChangesAsync(ct);

        return Result<DevelopmentUnitDto>.Success(new DevelopmentUnitDto(
            unit.Id, unit.UnitIdentifier, unit.UnitType, unit.Status.ToString(),
            unit.Price, unit.Currency, unit.Description, unit.CreatedAt, unit.UpdatedAt));
    }

    public async Task<Result<DevelopmentUnitDto>> UpdateAsync(int projectId, int unitId, UpdateDevelopmentUnitRequest request, CancellationToken ct)
    {
        var unit = await dbContext.DevelopmentUnits
            .FirstOrDefaultAsync(u => u.Id == unitId && u.DevelopmentProjectId == projectId, ct);

        if (unit is null)
            return Result<DevelopmentUnitDto>.Failure(
                Error.NotFound("unit.notfound", $"Development unit with id {unitId} was not found in project {projectId}."));

        if (await dbContext.DevelopmentUnits.AnyAsync(u => u.DevelopmentProjectId == projectId && u.UnitIdentifier == request.UnitIdentifier && u.Id != unitId, ct))
            return Result<DevelopmentUnitDto>.Failure(
                Error.Conflict("unit.conflict", $"A unit with identifier '{request.UnitIdentifier}' already exists in this project."));

        unit.UnitIdentifier = request.UnitIdentifier;
        unit.UnitType = request.UnitType;
        unit.Status = Enum.Parse<DevelopmentUnitStatus>(request.Status, true);
        unit.Price = request.Price;
        unit.Currency = request.Currency ?? "NGN";
        unit.Description = request.Description;
        unit.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(ct);

        return Result<DevelopmentUnitDto>.Success(new DevelopmentUnitDto(
            unit.Id, unit.UnitIdentifier, unit.UnitType, unit.Status.ToString(),
            unit.Price, unit.Currency, unit.Description, unit.CreatedAt, unit.UpdatedAt));
    }

    public async Task<Result> DeleteAsync(int projectId, int unitId, CancellationToken ct)
    {
        var unit = await dbContext.DevelopmentUnits
            .FirstOrDefaultAsync(u => u.Id == unitId && u.DevelopmentProjectId == projectId, ct);

        if (unit is null)
            return Result.Failure(
                Error.NotFound("unit.notfound", $"Development unit with id {unitId} was not found in project {projectId}."));

        dbContext.DevelopmentUnits.Remove(unit);
        await dbContext.SaveChangesAsync(ct);
        return Result.Success();
    }
}
