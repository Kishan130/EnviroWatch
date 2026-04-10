using System.ComponentModel.DataAnnotations;

namespace EnviroWatch.Models
{
    public class District
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string State { get; set; } = string.Empty;

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsMetroCity { get; set; }

        // Navigation properties
        public ICollection<WeatherSnapshot> WeatherSnapshots { get; set; } = new List<WeatherSnapshot>();
        public ICollection<AQISnapshot> AQISnapshots { get; set; } = new List<AQISnapshot>();
        public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
    }
}
