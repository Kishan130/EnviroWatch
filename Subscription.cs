using System.ComponentModel.DataAnnotations;

namespace EnviroWatch.Models
{
    public class Subscription
    {
        [Key]
        public int SubscriptionId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int DistrictId { get; set; }
        public District District { get; set; } = null!;
        public int AQIThreshold { get; set; } = 200;
        public bool EmailEnabled { get; set; } = true;
        public bool SmsEnabled { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
