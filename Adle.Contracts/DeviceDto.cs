namespace Adle.Contracts;

/// <summary>
/// Read model for a simulation device (from the SimulationDB DeviceBases table).
/// <paramref name="Kind"/> is the EF discriminator (device subtype).
/// </summary>
public record DeviceDto(
    int Id,
    string? Name,
    string? Ip,
    bool? State,
    string Kind,
    int AreaId,
    string? AreaName);
