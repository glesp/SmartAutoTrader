using Microsoft.EntityFrameworkCore;
using SmartAutoTrader.API.Data;
using SmartAutoTrader.API.Models;

namespace SmartAutoTrader.API.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);

        Task<User?> GetByUsernameAsync(string username);

        Task<User?> GetByIdAsync(int id);

        Task<bool> ExistsAsync(string email, string username);

        Task<List<UserFavorite>> GetFavoritesWithVehiclesAsync(int userId);

        Task<List<BrowsingHistory>> GetRecentBrowsingHistoryWithVehiclesAsync(int userId, int limit = 5);

        Task AddAsync(User user);

        Task SaveChangesAsync();
    }

    public class UserRepository(ApplicationDbContext context) : IUserRepository
    {
        private readonly ApplicationDbContext _context = context;

        public Task<User?> GetByEmailAsync(string email)
        {
            return _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public Task<User?> GetByUsernameAsync(string username)
        {
            return _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public Task<User?> GetByIdAsync(int id)
        {
            return _context.Users
                .Include(u => u.Preferences)
                .FirstOrDefaultAsync(u => u.Id == id);
        }


        public Task<bool> ExistsAsync(string email, string username)
        {
            return _context.Users.AnyAsync(u => u.Email == email || u.Username == username);
        }

        public Task<List<UserFavorite>> GetFavoritesWithVehiclesAsync(int userId)
        {
            return _context.UserFavorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Vehicle)
                .ToListAsync();
        }

        public Task<List<BrowsingHistory>> GetRecentBrowsingHistoryWithVehiclesAsync(int userId, int limit = 5)
        {
            return _context.BrowsingHistory
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.ViewDate)
                .Take(limit)
                .Include(h => h.Vehicle)
                .ToListAsync();
        }


        public Task AddAsync(User user)
        {
            _ = _context.Users.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
