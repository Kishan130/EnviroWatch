using System.ComponentModel.DataAnnotations;

namespace EnviroWatch.Models
{
    public class AlertLog
    {
        [Key]
        public int AlertId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int DistrictId { get; set; }
        public District District { get; set; } = null!;
        public int AQIValue { get; set; }
        public int ThresholdBreached { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        [MaxLength(20)]
        public string NotificationType { get; set; } = "Email";
        public bool DeliverySuccess { get; set; }
    }
}
