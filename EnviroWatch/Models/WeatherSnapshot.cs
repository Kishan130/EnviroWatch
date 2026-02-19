using System.ComponentModel.DataAnnotations;

namespace EnviroWatch.Models
{
    public class WeatherSnapshot
    {
        [Key]
        public int SnapshotId { get; set; }
        public int DistrictId { get; set; }
        public District District { get; set; } = null!;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double WindSpeed { get; set; }
        public double Pressure { get; set; }
        public string? WeatherCondition { get; set; }
        public string? WeatherIcon { get; set; }
        public double? UVIndex { get; set; }
        [MaxLength(50)]
        public string Source { get; set; } = "OWM";
    }
}
