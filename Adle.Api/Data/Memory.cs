using System;
using System.Collections.Generic;

namespace Adle.Api.Data;

public partial class Memory
{
    public int Id { get; set; }

    public int? AreaId { get; set; }

    public int? ItemId { get; set; }

    public DateTime Date { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string? DeletedBy { get; set; }

    public DateTime? DeletedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string? Definition { get; set; }

    public string? ActionName { get; set; }

    public string? ActionValue { get; set; }

    public virtual Area? Area { get; set; }

    public virtual Item? Item { get; set; }
}
