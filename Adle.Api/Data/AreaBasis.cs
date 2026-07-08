using System;
using System.Collections.Generic;

namespace Adle.Api.Data;

public partial class AreaBasis
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<DeviceBasis> DeviceBases { get; set; } = new List<DeviceBasis>();

    public virtual ICollection<OperationDevice> OperationDevices { get; set; } = new List<OperationDevice>();

    public virtual ICollection<Scenario> Scenarios { get; set; } = new List<Scenario>();
}
