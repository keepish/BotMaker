using BotMaker.ServiceLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotMaker.ServiceLayer.Services
{
    public class CurrentUserService
    {
        public static User? CurrentUser { get; set; } = null;
        public static string CurrentInstruction;
    }
}
