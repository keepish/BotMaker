using System;
using System.Collections.Generic;

namespace BotMaker.ServiceLayer.Models;

public partial class Bot
{
    public long UserId { get; set; }

    public string Token { get; set; } = null!;

    public string? Name { get; set; }

    public virtual User? User { get; set; } = null!;
}
