using System;
using System.Collections.Generic;

namespace Adle.Api.Data;

public partial class Actor
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<Habit> HabitHabits { get; set; } = new List<Habit>();

    public virtual ICollection<Scenario> Scenarios { get; set; } = new List<Scenario>();
}
