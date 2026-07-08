namespace Adle.Contracts;

/// <summary>
/// Read model for an area (room/zone). Flattens the EF entity into the shape
/// the web client consumes; TypeScript types are generated from this via the
/// API's OpenAPI document.
/// </summary>
public record AreaDto(
    int Id,
    string? Name,
    double Width,
    double Height,
    int? ParentAreaId,
    int? AreaTypeId,
    string? AreaTypeName);
