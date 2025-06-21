using Microsoft.EntityFrameworkCore;
using BotMaker.ServiceLayer.Data;
using BotMaker.ServiceLayer.Models;

namespace ServiceLayer.Services
{
    public class UserService
    {
        private static readonly BotsContext _context = new();


        public async Task<User?> GetUserByIdAsync(long id)
        {
            return await _context.Users.Include(u=>u.Bots).FirstOrDefaultAsync(u => u.UserId == id);
        }

        public async Task AddUserAsync(long tgId, string name, bool isVip)
        {
            var user = GetUserByIdAsync(tgId);
            if (user == null)
            {
                var newUser = new User() { UserId = tgId, Name = name, IsVip = isVip };
                await _context.Users.AddAsync(newUser);
                await _context.SaveChangesAsync();
            }
        }
    }
}
