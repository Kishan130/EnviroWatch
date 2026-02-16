using System.ComponentModel.DataAnnotations;

namespace EnviroWatch.Models
{
    public class AQISnapshot
    {
        [Key]
        public int SnapshotId { get; set; }
        public int DistrictId { get; set; }
        public District District { get; set; } = null!;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int AQI { get; set; }
        public double? PM25 { get; set; }
        public double? PM10 { get; set; }
        public double? NO2 { get; set; }
        public double? SO2 { get; set; }
        public double? CO { get; set; }
        public double? O3 { get; set; }
        [MaxLength(50)]
        public string Source { get; set; } = "OWM";
    }
}
