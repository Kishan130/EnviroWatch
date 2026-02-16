using System.ComponentModel.DataAnnotations;

namespace EnviroWatch.Models
{
    public class JobExecutionLog
    {
        [Key]
        public int LogId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int DistrictsProcessed { get; set; }
        public int FailuresCount { get; set; }
        public string? ExceptionDetails { get; set; }
        public bool Success { get; set; }
    }
}
