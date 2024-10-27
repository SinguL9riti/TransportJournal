using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TransportJournal.Models;

public partial class Personnel
{
    [Display(Name = "Код Работника")]
    public int PersonnelId { get; set; }

    [Display(Name = "Код маршрута")]
    public int RouteId { get; set; }

    [Display(Name ="День")]
    public DateOnly Date { get; set; }

    [Display(Name = "Смена")]
    public string Shift { get; set; } = null!;

    [Display(Name = "Работники")]
    public string EmployeeList { get; set; } = null!;

    public virtual Route Route { get; set; } = null!;
}
