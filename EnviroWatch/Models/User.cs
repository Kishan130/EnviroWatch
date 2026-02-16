using System.ComponentModel.DataAnnotations;

namespace EnviroWatch.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
        [Required, MaxLength(100)]
        public string Email { get; set; } = string.Empty;
        [MaxLength(100)]
        public string? Name { get; set; }
        [MaxLength(50)]
        public string? Provider { get; set; } // Google, Facebook
        [MaxLength(200)]
        public string? ProviderId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsAdmin { get; set; } = false;
        public ICollection<Subscription> Subscriptions { get; set; }
            = new List<Subscription>();
    }
}
