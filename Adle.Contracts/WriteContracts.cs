namespace Adle.Contracts;

/// <summary>Create/update payload for an area (rooms/zones).</summary>
public record AreaWriteDto(
    string Name,
    double Width,
    double Height,
    int? ParentAreaId,
    int? AreaTypeId);

/// <summary>
/// Create/update payload for a simulation device. <paramref name="AreaId"/>
/// references an AreaBase; <paramref name="Kind"/> is the device subtype
/// (stored as the EF discriminator).
/// </summary>
public record DeviceWriteDto(
    string Name,
    string? Ip,
    bool? State,
    string Kind,
    int AreaId);

/// <summary>Create/update payload for an operation/routine.</summary>
public record OperationWriteDto(
    string Name,
    DateTime StartTime,
    double DurationSeconds);
