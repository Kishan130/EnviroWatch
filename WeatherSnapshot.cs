using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnviroWatch.Models
{
    [Table("WeatherSnapshots")]
    public class WeatherSnapshot
    {
        [Key]
        public long SnapshotId { get; set; }

        public int DistrictId { get; set; }

        [Column("FetchedAtUtc")]
        public DateTime Timestamp { get; set; }


        public double? TempCelsius { get; set; }
        public double? FeelsLikeCelsius { get; set; }
        public int? HumidityPct { get; set; }
        public double? WindSpeedMs { get; set; }
        public int? PressureHpa { get; set; }
        public int? VisibilityM { get; set; }
        public double? UVIndex { get; set; }
        public string? ConditionText { get; set; }
        public string? ConditionIcon { get; set; }
        public double? PrecipMM { get; set; }
        public string SourceApi { get; set; } = "OWM";

        [ForeignKey("DistrictId")]
        public District? District { get; set; }
    }
}
