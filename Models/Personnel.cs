using System;
using System.Collections.Generic;

namespace TransportJournal.Models;

public partial class Personnel
{
    public int PersonnelId { get; set; }

    public int RouteId { get; set; }

    public DateOnly Date { get; set; }

    public string Shift { get; set; } = null!;

    public string EmployeeList { get; set; } = null!;

    public virtual Route Route { get; set; } = null!;
}
