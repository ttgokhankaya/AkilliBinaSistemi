using System;
using System.Collections.Generic;

namespace Adle.Api.Data;

public partial class Operation
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public DateTime StartTime { get; set; }

    public TimeSpan Duration { get; set; }

    public virtual ICollection<OperationDevice> OperationDevices { get; set; } = new List<OperationDevice>();

    public virtual ICollection<OperationHabitMapping> OperationHabitMappings { get; set; } = new List<OperationHabitMapping>();
}
