using System;
using System.Collections.Generic;

namespace TransportJournal.Models;

public partial class Schedule
{
    public int ScheduleId { get; set; }

    public int RouteId { get; set; }

    public string Weekday { get; set; } = null!;

    public TimeOnly ArrivalTime { get; set; }

    public int Year { get; set; }

    public virtual Route Route { get; set; } = null!;
}
