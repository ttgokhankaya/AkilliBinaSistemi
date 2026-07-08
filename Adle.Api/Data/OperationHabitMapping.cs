using System;
using System.Collections.Generic;

namespace Adle.Api.Data;

public partial class OperationHabitMapping
{
    public int Id { get; set; }

    public int MaxDuration { get; set; }

    public int MinDuration { get; set; }

    public int? OperationId { get; set; }

    public int? HabitId { get; set; }

    public virtual Habit? Habit { get; set; }

    public virtual Operation? Operation { get; set; }
}
