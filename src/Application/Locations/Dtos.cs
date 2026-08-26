using System.ComponentModel.DataAnnotations;

namespace PIPDC.Application.Locations;

public record LocationDto(
    int Id,
    string Name,
    string Slug,
    string Type,
    int? ParentId,
    string? ParentName,
    int ChildCount);

public record CreateLocationRequest(
    [Required, MaxLength(100)] string Name,
    [MaxLength(100)] string? Slug,
    [Required] string Type,
    int? ParentId);
