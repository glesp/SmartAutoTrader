/* <copyright file="ApplicationDbContext.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the ApplicationDbContext class, which serves as the Entity Framework Core database context for the Smart Auto Trader application.
</summary>
<remarks>
The ApplicationDbContext class provides DbSet properties for all the entities in the application, enabling CRUD operations and database queries. It also configures relationships between entities and seeds initial data, such as roles. The class uses dependency injection to receive DbContextOptions and is designed to work with Microsoft SQL Server or other compatible databases.
</remarks>
<dependencies>
- Microsoft.EntityFrameworkCore
- SmartAutoTrader.API.Models
</dependencies>
 */

namespace SmartAutoTrader.API.Data
{
    using Microsoft.EntityFrameworkCore;
    using SmartAutoTrader.API.Models;

    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        /// <summary>
        /// Gets or sets the DbSet for vehicles.
        /// </summary>
        /// <value>A collection of <see cref="Vehicle"/> entities.</value>
        public DbSet<Vehicle>? Vehicles { get; set; }

        /// <summary>
        /// Gets or sets the DbSet for vehicle images.
        /// </summary>
        /// <value>A collection of <see cref="VehicleImage"/> entities.</value>
        public DbSet<VehicleImage>? VehicleImages { get; set; }

        /// <summary>
        /// Gets or sets the DbSet for vehicle features.
        /// </summary>
        /// <value>A collection of <see cref="VehicleFeature"/> entities.</value>
        public DbSet<VehicleFeature>? VehicleFeatures { get; set; }

        /// <summary>
        /// Gets or sets the DbSet for users.
        /// </summary>
        /// <value>A collection of <see cref="User"/> entities.</value>
        public DbSet<User>? Users { get; set; }

        /// <summary>
        /// Gets or sets the DbSet for user favorites.
        /// </summary>
        /// <value>A collection of <see cref="UserFavorite"/> entities.</value>
        public DbSet<UserFavorite>? UserFavorites { get; set; }

        /// <summary>
        /// Gets or sets the DbSet for user preferences.
        /// </summary>
        /// <value>A collection of <see cref="UserPreference"/> entities.</value>
        public DbSet<UserPreference>? UserPreferences { get; set; }

        /// <summary>
        /// Gets or sets the DbSet for browsing history.
        /// </summary>
        /// <value>A collection of <see cref="BrowsingHistory"/> entities.</value>
        public DbSet<BrowsingHistory>? BrowsingHistory { get; set; }

        /// <summary>
        /// Gets or sets the DbSet for inquiries.
        /// </summary>
        /// <value>A collection of <see cref="Inquiry"/> entities.</value>
        public DbSet<Inquiry>? Inquiries { get; set; }

        /// <summary>
        /// Gets or sets the DbSet for chat history.
        /// </summary>
        /// <value>A collection of <see cref="ChatHistory"/> entities.</value>
        public DbSet<ChatHistory>? ChatHistory { get; set; }

        /// <summary>
        /// Gets or sets the DbSet for conversation sessions.
        /// </summary>
        /// <value>A collection of <see cref="ConversationSession"/> entities.</value>
        public DbSet<ConversationSession>? ConversationSessions { get; set; }

        /// <summary>
        /// Gets or sets the DbSet for roles.
        /// </summary>
        /// <value>A collection of <see cref="Role"/> entities.</value>
        public DbSet<Role>? Roles { get; set; }

        /// <summary>
        /// Gets or sets the DbSet for user roles.
        /// </summary>
        /// <value>A collection of <see cref="UserRole"/> entities.</value>
        public DbSet<UserRole>? UserRoles { get; set; }

        /// <summary>
        /// Configures the relationships and constraints for the database entities.
        /// </summary>
        /// <param name="modelBuilder">The <see cref="ModelBuilder"/> used to configure entity relationships.</param>
        /// <remarks>
        /// This method defines relationships such as one-to-many and many-to-many, sets up composite keys, and seeds initial data for roles.
        /// </remarks>
        /// <example>
        /// protected override void OnModelCreating(ModelBuilder modelBuilder)
        /// {
        ///     base.OnModelCreating(modelBuilder);
        ///     modelBuilder.Entity.<VehicleImage>()
        ///         .HasOne(vi => vi.Vehicle)
        ///         .WithMany(v => v.Images)
        ///         .HasForeignKey(vi => vi.VehicleId);
        /// }
        /// </example>
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

            // ConversationSession - User (one-to-many)
            _ = modelBuilder.Entity<ConversationSession>()
                .HasOne(cs => cs.User)
                .WithMany()
                .HasForeignKey(cs => cs.UserId);

            // ConversationSession - ChatHistory (one-to-many)
            _ = modelBuilder.Entity<ConversationSession>()
                .HasMany(cs => cs.Messages)
                .WithOne(ch => ch.Session)
                .HasForeignKey(ch => ch.ConversationSessionId)
                .IsRequired(false); // Make the relationship optional

            // UserRole - Composite Key
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId }); // Define composite key
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles) // Use navigation property in User
                .HasForeignKey(ur => ur.UserId);

            // UserRole - Role Relationship
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