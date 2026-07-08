using System;
using System.Collections.Generic;

namespace Adle.Api.Data;

public partial class AreaType
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Definition { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string? DeletedBy { get; set; }

    public DateTime? DeletedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public virtual ICollection<Area> Areas { get; set; } = new List<Area>();
}
