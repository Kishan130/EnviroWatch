using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnviroWatch.Models
{
    public class WeatherSnapshot
    {
        [Key]
        public int Id { get; set; }

        public int DistrictId { get; set; }

        [ForeignKey("DistrictId")]
        public District? District { get; set; }

        public double Temperature { get; set; }       // °C
        public double FeelsLike { get; set; }          // °C
        public int Humidity { get; set; }              // %
        public double Pressure { get; set; }           // hPa
        public double WindSpeed { get; set; }          // m/s
        public int WindDirection { get; set; }         // degrees
        public int Visibility { get; set; }            // meters
        public int CloudCover { get; set; }            // %

        [MaxLength(100)]
        public string WeatherCondition { get; set; } = string.Empty;  // "Clear", "Rain", etc.

        [MaxLength(20)]
        public string WeatherIcon { get; set; } = string.Empty;       // OpenWeather icon code

        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        public DateTime Sunrise { get; set; }
        public DateTime Sunset { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}
