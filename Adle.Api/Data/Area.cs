using System;
using System.Collections.Generic;

namespace Adle.Api.Data;

public partial class Area
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public double Width { get; set; }

    public double Height { get; set; }

    public int? AreaId { get; set; }

    public int? AreaTypeId { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string? DeletedBy { get; set; }

    public DateTime? DeletedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual Area? AreaNavigation { get; set; }

    public virtual AreaType? AreaType { get; set; }

    public virtual ICollection<Area> InverseAreaNavigation { get; set; } = new List<Area>();

    public virtual ICollection<Item> Items { get; set; } = new List<Item>();

    public virtual ICollection<Memory> Memories { get; set; } = new List<Memory>();
}
