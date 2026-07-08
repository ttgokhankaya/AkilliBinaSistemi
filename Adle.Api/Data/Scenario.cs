using System;
using System.Collections.Generic;

namespace Adle.Api.Data;

public partial class Scenario
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public virtual ICollection<Actor> Actors { get; set; } = new List<Actor>();

    public virtual ICollection<AreaBasis> AreaBases { get; set; } = new List<AreaBasis>();
}
