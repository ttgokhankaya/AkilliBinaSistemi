using System;
using System.Collections.Generic;

namespace Adle.Api.Data;

public partial class DeviceBasis
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Ip { get; set; }

    public int AreaId { get; set; }

    public bool? State { get; set; }

    public string Discriminator { get; set; } = null!;

    public virtual AreaBasis Area { get; set; } = null!;

    public virtual ICollection<GraphNodeDeviceMapping> GraphNodeDeviceMappings { get; set; } = new List<GraphNodeDeviceMapping>();

    public virtual ICollection<OperationDevice> OperationDevices { get; set; } = new List<OperationDevice>();
}
