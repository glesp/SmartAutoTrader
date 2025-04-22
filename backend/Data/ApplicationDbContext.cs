namespace SmartAutoTrader.API.Data
{
    using Microsoft.EntityFrameworkCore;
    using SmartAutoTrader.API.Models;

    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<Vehicle> Vehicles { get; set; }

        public DbSet<VehicleImage> VehicleImages { get; set; }

        public DbSet<VehicleFeature> VehicleFeatures { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<UserFavorite> UserFavorites { get; set; }

        public DbSet<UserPreference> UserPreferences { get; set; }

        public DbSet<BrowsingHistory> BrowsingHistory { get; set; }

        public DbSet<Inquiry> Inquiries { get; set; }

        public DbSet<ChatHistory> ChatHistory { get; set; }

        public DbSet<ConversationSession> ConversationSessions { get; set; }

        public DbSet<Role> Roles { get; set; }

        public DbSet<UserRole> UserRoles { get; set; }

        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships

            // Vehicle - VehicleImage (one-to-many)
            _ = modelBuilder.Entity<VehicleImage>()
                .HasOne(vi => vi.Vehicle)
                .WithMany(v => v.Images)
                .HasForeignKey(vi => vi.VehicleId);

            // Vehicle - VehicleFeature (one-to-many)
            _ = modelBuilder.Entity<VehicleFeature>()
                .HasOne(vf => vf.Vehicle)
                .WithMany(v => v.Features)
                .HasForeignKey(vf => vf.VehicleId);

            // User - UserFavorite (one-to-many)
            _ = modelBuilder.Entity<UserFavorite>()
                .HasOne(uf => uf.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(uf => uf.UserId);

            // Vehicle - UserFavorite (one-to-many)
            _ = modelBuilder.Entity<UserFavorite>()
                .HasOne(uf => uf.Vehicle)
                .WithMany(v => v.FavoritedBy)
                .HasForeignKey(uf => uf.VehicleId);

            // User - UserPreference (one-to-many)
            _ = modelBuilder.Entity<UserPreference>()
                .HasOne(up => up.User)
                .WithMany(u => u.Preferences)
                .HasForeignKey(up => up.UserId);

            // User - BrowsingHistory (one-to-many)
            _ = modelBuilder.Entity<BrowsingHistory>()
                .HasOne(bh => bh.User)
                .WithMany(u => u.BrowsingHistory)
                .HasForeignKey(bh => bh.UserId);

            // Vehicle - BrowsingHistory (one-to-many)
            _ = modelBuilder.Entity<BrowsingHistory>()
                .HasOne(bh => bh.Vehicle)
                .WithMany()
                .HasForeignKey(bh => bh.VehicleId);

            // User - Inquiry (one-to-many)
            _ = modelBuilder.Entity<Inquiry>()
                .HasOne(i => i.User)
                .WithMany(u => u.SentInquiries)
                .HasForeignKey(i => i.UserId);

            // Vehicle - Inquiry (one-to-many)
            _ = modelBuilder.Entity<Inquiry>()
                .HasOne(i => i.Vehicle)
                .WithMany()
                .HasForeignKey(i => i.VehicleId);

            // User - ChatHistory (one-to-many)
            _ = modelBuilder.Entity<ChatHistory>()
                .HasOne(ch => ch.User)
                .WithMany()
                .HasForeignKey(ch => ch.UserId);
            _ = modelBuilder.Entity<ConversationSession>()
                .HasOne(cs => cs.User)
                .WithMany()
                .HasForeignKey(cs => cs.UserId);

            _ = modelBuilder.Entity<ConversationSession>()
                .HasMany(cs => cs.Messages)
                .WithOne(ch => ch.Session)
                .HasForeignKey(ch => ch.ConversationSessionId)
                .IsRequired(false); // Make the relationship optional
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId }); // Define composite key
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles) // Use navigation property in User
                .HasForeignKey(ur => ur.UserId);
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles) // Use navigation property in Role
                .HasForeignKey(ur => ur.RoleId);

            // Seed initial roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin" },
                new Role { Id = 2, Name = "User" });
        }
    }
}