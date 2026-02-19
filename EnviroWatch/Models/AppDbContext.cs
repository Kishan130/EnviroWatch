using Microsoft.EntityFrameworkCore;

namespace EnviroWatch.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<WeatherSnapshot> WeatherSnapshots { get; set; }
        public DbSet<AQISnapshot> AQISnapshots { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<AlertLog> AlertLogs { get; set; }
        public DbSet<JobExecutionLog> JobExecutionLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed Districts (sample data)
            modelBuilder.Entity<District>().HasData(
                new District
                {
                    DistrictId = 1,
                    DistrictName = "Ahmedabad",
                    StateName = "Gujarat",
                    Latitude = 23.0225,
                    Longitude = 72.5714
                },
                new District
                {
                    DistrictId = 2,
                    DistrictName = "Surat",
                    StateName = "Gujarat",
                    Latitude = 21.1702,
                    Longitude = 72.8311
                },
                new District
                {
                    DistrictId = 3,
                    DistrictName = "Mumbai",
                    StateName = "Maharashtra",
                    Latitude = 19.0760,
                    Longitude = 72.8777
                },
                new District
                {
                    DistrictId = 4,
                    DistrictName = "Delhi",
                    StateName = "Delhi",
                    Latitude = 28.7041,
                    Longitude = 77.1025
                },
                new District
                {
                    DistrictId = 5,
                    DistrictName = "Bengaluru",
                    StateName = "Karnataka",
                    Latitude = 12.9716,
                    Longitude = 77.5946
                }
            );
        }
    }
}
