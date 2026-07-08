using System;
using System.Collections.Generic;

namespace Adle.Api.Data;

public partial class GraphObject
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? MatrixValue { get; set; }

    public virtual ICollection<GraphNodeDeviceMapping> GraphNodeDeviceMappings { get; set; } = new List<GraphNodeDeviceMapping>();
}
