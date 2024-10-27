using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TransportJournal.Models;

public partial class Schedule
{
    [Display(Name = "Код расписания")]
    public int ScheduleId { get; set; }

    [Display(Name = "Код маршрута")]
    public int RouteId { get; set; }

    [Display(Name = "День Недели")]
    public string Weekday { get; set; } = null!;

    [Display(Name = "Время прибытия")]
    public TimeOnly ArrivalTime { get; set; }

    [Display(Name = "Год")]
    public int Year { get; set; }

    public virtual Route Route { get; set; } = null!;
}
