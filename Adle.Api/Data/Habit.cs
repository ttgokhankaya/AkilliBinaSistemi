using System;
using System.Collections.Generic;

namespace Adle.Api.Data;

public partial class Habit
{
    public int HabitId { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<OperationHabitMapping> OperationHabitMappings { get; set; } = new List<OperationHabitMapping>();

    public virtual ICollection<Actor> Actors { get; set; } = new List<Actor>();
}
