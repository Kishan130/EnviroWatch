using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EnviroWatch.Models
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<District> Districts { get; set; }
        public DbSet<WeatherSnapshot> WeatherSnapshots { get; set; }
        public DbSet<AQISnapshot> AQISnapshots { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Indexes for performance
            modelBuilder.Entity<WeatherSnapshot>()
                .HasIndex(w => new { w.DistrictId, w.RecordedAt });

            modelBuilder.Entity<AQISnapshot>()
                .HasIndex(a => new { a.DistrictId, a.RecordedAt });

            modelBuilder.Entity<UserSubscription>()
                .HasIndex(s => s.Email);

            // Districts are now seeded from Data/indian_districts.json via DistrictSeederService
        }
    }
}
