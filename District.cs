using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnviroWatch.Models
{
    [Table("Districts")]
    public class District
    {
        [Key]
        public int DistrictId { get; set; }

        [Column("Name")]          // DB column = Name
        public string DistrictName { get; set; } = "";

        [Column("State")]         // DB column = State
        public string StateName { get; set; } = "";

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsActive { get; set; }
    }
}
