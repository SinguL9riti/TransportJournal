using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TransportJournal.Models;

public partial class Stop
{
    [Display(Name = "Код остановки")]
    public int StopId { get; set; }

    [Display(Name = "Название")]
    public string Name { get; set; } = null!;

    [Display(Name = "Конечная")]
    public bool IsTerminal { get; set; }

    [Display(Name = "Диспетчер")]
    public bool HasDispatcher { get; set; }
}
