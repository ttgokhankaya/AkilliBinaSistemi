namespace Adle.Contracts;

/// <summary>
/// Read model for an operation/routine. <paramref name="DurationSeconds"/>
/// flattens the DB interval into a plain number for a TypeScript-friendly
/// contract.
/// </summary>
public record OperationDto(
    int Id,
    string? Name,
    DateTime StartTime,
    double DurationSeconds);
