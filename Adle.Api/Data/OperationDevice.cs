using System;
using System.Collections.Generic;

namespace Adle.Api.Data;

public partial class OperationDevice
{
    public int Id { get; set; }

    public int Sira { get; set; }

    public string? ActionName { get; set; }

    public int OperationId { get; set; }

    public int DeviceBaseId { get; set; }

    public int? AreaId { get; set; }

    public virtual AreaBasis? Area { get; set; }

    public virtual DeviceBasis DeviceBase { get; set; } = null!;

    public virtual Operation Operation { get; set; } = null!;
}
