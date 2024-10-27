using System;
using System.Collections.Generic;

namespace TransportJournal.Models;

public partial class Route
{
    public int RouteId { get; set; }

    public string Name { get; set; } = null!;

    public string TransportType { get; set; } = null!;

    public int PlannedTravelTime { get; set; }

    public decimal Distance { get; set; }

    public bool IsExpress { get; set; }

    public virtual ICollection<Personnel> Personnel { get; set; } = new List<Personnel>();

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
