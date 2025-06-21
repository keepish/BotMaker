using System;
using System.Collections.Generic;

namespace BotMaker.ServiceLayer.Models;

public partial class User
{
    public long UserId { get; set; }

    public string? Name { get; set; }

    public bool IsVip { get; set; }

    public virtual ICollection<Bot> Bots { get; set; } = new List<Bot>();
}
