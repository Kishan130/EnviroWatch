using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnviroWatch.Models
{
    public class AQISnapshot
    {
        [Key]
        public int Id { get; set; }

        public int DistrictId { get; set; }

        [ForeignKey("DistrictId")]
        public District? District { get; set; }

        public int AQI { get; set; }                   // Computed Indian AQI (0-500)

        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;  // Good, Satisfactory, Moderate, Poor, Very Poor, Severe

        public double PM25 { get; set; }               // µg/m³
        public double PM10 { get; set; }               // µg/m³
        public double O3 { get; set; }                 // µg/m³
        public double NO2 { get; set; }                // µg/m³
        public double SO2 { get; set; }                // µg/m³
        public double CO { get; set; }                 // µg/m³

        [MaxLength(20)]
        public string DominantPollutant { get; set; } = string.Empty;

        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    }
}
