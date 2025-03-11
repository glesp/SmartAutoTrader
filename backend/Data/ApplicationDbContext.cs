using Microsoft.EntityFrameworkCore;
using SmartAutoTrader.API.Models;

namespace SmartAutoTrader.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<VehicleImage> VehicleImages { get; set; }
        public DbSet<VehicleFeature> VehicleFeatures { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserFavorite> UserFavorites { get; set; }
        public DbSet<UserPreference> UserPreferences { get; set; }
        public DbSet<BrowsingHistory> BrowsingHistory { get; set; }
        public DbSet<Inquiry> Inquiries { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure relationships
            
            // Vehicle - VehicleImage (one-to-many)
            modelBuilder.Entity<VehicleImage>()
                .HasOne(vi => vi.Vehicle)
                .WithMany(v => v.Images)
                .HasForeignKey(vi => vi.VehicleId);
            
            // Vehicle - VehicleFeature (one-to-many)
            modelBuilder.Entity<VehicleFeature>()
                .HasOne(vf => vf.Vehicle)
                .WithMany(v => v.Features)
                .HasForeignKey(vf => vf.VehicleId);
            
            // User - UserFavorite (one-to-many)
            modelBuilder.Entity<UserFavorite>()
                .HasOne(uf => uf.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(uf => uf.UserId);
            
            // Vehicle - UserFavorite (one-to-many)
            modelBuilder.Entity<UserFavorite>()
                .HasOne(uf => uf.Vehicle)
                .WithMany(v => v.FavoritedBy)
                .HasForeignKey(uf => uf.VehicleId);
            
            // User - UserPreference (one-to-many)
            modelBuilder.Entity<UserPreference>()
                .HasOne(up => up.User)
                .WithMany(u => u.Preferences)
                .HasForeignKey(up => up.UserId);
            
            // User - BrowsingHistory (one-to-many)
            modelBuilder.Entity<BrowsingHistory>()
                .HasOne(bh => bh.User)
                .WithMany(u => u.BrowsingHistory)
                .HasForeignKey(bh => bh.UserId);
            
            // Vehicle - BrowsingHistory (one-to-many)
            modelBuilder.Entity<BrowsingHistory>()
                .HasOne(bh => bh.Vehicle)
                .WithMany()
                .HasForeignKey(bh => bh.VehicleId);
            
            // User - Inquiry (one-to-many)
            modelBuilder.Entity<Inquiry>()
                .HasOne(i => i.User)
                .WithMany(u => u.SentInquiries)
                .HasForeignKey(i => i.UserId);
            
            // Vehicle - Inquiry (one-to-many)
            modelBuilder.Entity<Inquiry>()
                .HasOne(i => i.Vehicle)
                .WithMany()
                .HasForeignKey(i => i.VehicleId);
        }
    }
}