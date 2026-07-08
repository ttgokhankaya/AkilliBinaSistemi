using System;
using System.Collections.Generic;

namespace Adle.Api.Data;

public partial class Item
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public bool Availablity { get; set; }

    public string? IpV4 { get; set; }

    public string? IpV6 { get; set; }

    public string? ItemType { get; set; }

    public int AreaOfItemId { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string? DeletedBy { get; set; }

    public DateTime? DeletedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual Area AreaOfItem { get; set; } = null!;

    public virtual ICollection<Memory> Memories { get; set; } = new List<Memory>();
}
