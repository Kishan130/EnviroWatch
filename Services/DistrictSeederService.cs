using System.Text.Json;
using EnviroWatch.Models;
using Microsoft.EntityFrameworkCore;

namespace EnviroWatch.Services
{
    public static class DistrictSeederService
    {
        public static async Task SeedAsync(AppDbContext db, ILogger logger)
        {
            // Check if we already have districts beyond the old seed data
            var existingCount = await db.Districts.CountAsync();

            var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "indian_districts.json");
            if (!File.Exists(jsonPath))
            {
                logger.LogWarning("District seed file not found at {Path}", jsonPath);
                return;
            }

            var json = await File.ReadAllTextAsync(jsonPath);
            var districtData = JsonSerializer.Deserialize<List<DistrictSeedDto>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (districtData == null || districtData.Count == 0)
            {
                logger.LogWarning("No district data found in seed file");
                return;
            }

            // Get existing district names for deduplication
            var existingDistricts = await db.Districts
                .Select(d => new { d.Name, d.State })
                .ToListAsync();

            var existingSet = new HashSet<string>(
                existingDistricts.Select(d => $"{d.Name}|{d.State}"),
                StringComparer.OrdinalIgnoreCase
            );

            var newDistricts = districtData
                .Where(d => !existingSet.Contains($"{d.Name}|{d.State}"))
                .Select(d => new District
                {
                    Name = d.Name,
                    State = d.State,
                    Latitude = d.Latitude,
                    Longitude = d.Longitude,
                    IsMetroCity = d.IsMetroCity
                })
                .ToList();

            if (newDistricts.Count > 0)
            {
                await db.Districts.AddRangeAsync(newDistricts);
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} new districts. Total: {Total}",
                    newDistricts.Count, existingCount + newDistricts.Count);
            }
            else
            {
                logger.LogInformation("All {Count} districts already present in database", existingCount);
            }
        }

        private class DistrictSeedDto
        {
            public string Name { get; set; } = string.Empty;
            public string State { get; set; } = string.Empty;
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public bool IsMetroCity { get; set; }
        }
    }
}
