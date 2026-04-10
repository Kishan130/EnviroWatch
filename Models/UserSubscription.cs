using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnviroWatch.Models
{
    public class UserSubscription
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public AppUser? User { get; set; }

        public int DistrictId { get; set; }

        [ForeignKey("DistrictId")]
        public District? District { get; set; }

        public int AQIThreshold { get; set; } = 200;  // Alert when AQI exceeds this
        public bool NotifyEmail { get; set; } = true;
        public bool NotifySMS { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
