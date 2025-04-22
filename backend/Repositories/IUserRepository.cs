namespace SmartAutoTrader.API.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using SmartAutoTrader.API.Data;
    using SmartAutoTrader.API.Models;

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
        private readonly ApplicationDbContext context = context;
public Task<User?> GetByEmailAsync(string email)
        {
            return this.context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        /// <inheritdoc/>
        public Task<User?> GetByUsernameAsync(string username)
        {
            return this.context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        /// <inheritdoc/>
        public Task<User?> GetByIdAsync(int id)
        {
            return this.context.Users
                .Include(u => u.Preferences)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        /// <inheritdoc/>
        public Task<bool> ExistsAsync(string email, string username)
        {
            return this.context.Users.AnyAsync(u => u.Email == email || u.Username == username);
        }

        /// <inheritdoc/>
        public Task<List<UserFavorite>> GetFavoritesWithVehiclesAsync(int userId)
        {
            return this.context.UserFavorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Vehicle)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public Task<List<BrowsingHistory>> GetRecentBrowsingHistoryWithVehiclesAsync(int userId, int limit = 5)
        {
            return this.context.BrowsingHistory
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.ViewDate)
                .Take(limit)
                .Include(h => h.Vehicle)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public Task AddAsync(User user)
        {
            _ = this.context.Users.Add(user);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task SaveChangesAsync()
        {
            return this.context.SaveChangesAsync();
        }
    }
}