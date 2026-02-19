using System.ComponentModel.DataAnnotations;

namespace EnviroWatch.Models
{
    public class District
    {
        [Key]
        public int DistrictId { get; set; }
        [Required, MaxLength(100)]
        public string DistrictName { get; set; } = string.Empty;
        [Required, MaxLength(100)]
        public string StateName { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public ICollection<WeatherSnapshot> WeatherSnapshots { get; set; }
            = new List<WeatherSnapshot>();
        public ICollection<AQISnapshot> AQISnapshots { get; set; }
            = new List<AQISnapshot>();
    }
}
