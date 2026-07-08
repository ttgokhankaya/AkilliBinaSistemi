using System;
using System.Collections.Generic;

namespace Adle.Api.Data;

public partial class GraphNodeDeviceMapping
{
    public int Id { get; set; }

    public string? NodeName { get; set; }

    public int DeviceId { get; set; }

    public int GraphId { get; set; }

    public virtual DeviceBasis Device { get; set; } = null!;

    public virtual GraphObject Graph { get; set; } = null!;
}
