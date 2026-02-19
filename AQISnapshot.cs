using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnviroWatch.Models
{
    [Table("AQISnapshots")]
    public class AQISnapshot
    {
        [Key]
        public long AQISnapshotId { get; set; }

        public int DistrictId { get; set; }

        [Column("FetchedAtUtc")]   // 🔥 IMPORTANT FIX
        public DateTime Timestamp { get; set; }

        public int? AQIValue { get; set; }
        public string? Category { get; set; }
        public double? PM25 { get; set; }
        public double? PM10 { get; set; }
        public double? NO2 { get; set; }
        public double? SO2 { get; set; }
        public double? CO { get; set; }
        public double? O3 { get; set; }
        public string SourceApi { get; set; } = "OWM";

        [ForeignKey("DistrictId")]
        public District? District { get; set; }
    }
}
