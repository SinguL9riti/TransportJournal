using System;
using System.Collections.Generic;

namespace TransportJournal.Models;

public partial class Stop
{
    public int StopId { get; set; }

    public string Name { get; set; } = null!;

    public bool IsTerminal { get; set; }

    public bool HasDispatcher { get; set; }
}
