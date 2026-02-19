using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnviroWatch.Models
{
    [Table("JobExecutionLog")]
    public class JobExecutionLog
    {
        [Key]
        public long LogId { get; set; }
        public DateTime StartedAtUtc { get; set; }
        public DateTime? FinishedAtUtc { get; set; }
        public int DistrictsTotal { get; set; }
        public int DistrictsOk { get; set; }
        public int DistrictsFailed { get; set; }
        public string? ErrorDetails { get; set; }
    }
}