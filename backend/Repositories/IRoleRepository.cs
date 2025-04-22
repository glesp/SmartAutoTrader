namespace SmartAutoTrader.API.Repositories
{
    using Microsoft.EntityFrameworkCore;
    using SmartAutoTrader.API.Data;
    using SmartAutoTrader.API.Models;

    public interface IRoleRepository
    {
        Task<IEnumerable<string>> GetUserRolesAsync(int userId);

        Task<Role?> GetRoleByNameAsync(string roleName);

        Task<Role?> GetRoleByIdAsync(int roleId);

        Task AssignRoleToUserAsync(int userId, int roleId);

        Task RemoveRoleFromUserAsync(int userId, int roleId);

        Task<bool> UserHasRoleAsync(int userId, string roleName);

        Task SaveChangesAsync();
    }

    public class RoleRepository : IRoleRepository
    {
        private readonly ApplicationDbContext context;

        public RoleRepository(ApplicationDbContext context)
        {
            this.context = context;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetUserRolesAsync(int userId)
        {
            return await this.context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Join(
                    this.context.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => r.Name)
                .ToListAsync();
        }

        /// <inheritdoc/>
        public async Task<Role?> GetRoleByNameAsync(string roleName)
        {
            return await this.context.Roles
                .FirstOrDefaultAsync(r => r.Name == roleName);
        }

        /// <inheritdoc/>
        public async Task<Role?> GetRoleByIdAsync(int roleId)
        {
            return await this.context.Roles
                .FirstOrDefaultAsync(r => r.Id == roleId);
        }

        /// <inheritdoc/>
        public async Task AssignRoleToUserAsync(int userId, int roleId)
        {
            // Check if the user already has this role
            bool hasRole = await this.context.UserRoles
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (!hasRole)
            {
                // Create a new UserRole record
                var userRole = new UserRole
                {
                    UserId = userId,
                    RoleId = roleId,
                };

                this.context.UserRoles.Add(userRole);
                await this.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task RemoveRoleFromUserAsync(int userId, int roleId)
        {
            var userRole = await this.context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (userRole != null)
            {
                this.context.UserRoles.Remove(userRole);
                await this.SaveChangesAsync();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UserHasRoleAsync(int userId, string roleName)
        {
            return await this.context.UserRoles
                .Join(
                    this.context.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => new { UserRole = ur, Role = r })
                .AnyAsync(x => x.UserRole.UserId == userId && x.Role.Name == roleName);
        }

        /// <inheritdoc/>
        public Task SaveChangesAsync()
        {
            return this.context.SaveChangesAsync();
        }
    }
}