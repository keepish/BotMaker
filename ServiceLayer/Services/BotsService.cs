using Microsoft.EntityFrameworkCore;
using BotMaker.ServiceLayer.Data;
using BotMaker.ServiceLayer.Models;

namespace ServiceLayer.Services
{
    public class BotsService
    {
        private static readonly BotsContext _context = new();

        public async Task<Bot?> GetBotByNameAsync(string name, long userId)
        {
            return await _context.Bots.FirstOrDefaultAsync(b => b.Name == name && b.UserId == userId);
        }

        public async Task<bool> AddBotAsync(long userId, string token, string name)
        {
            var bot = await GetBotByNameAsync(name, userId);
            if (bot == null)
            {
                var newBot = new Bot() { UserId = userId, Token = token, Name = name };
                await _context.Bots.AddAsync(newBot);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}
