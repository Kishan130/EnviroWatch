using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace EnviroWatch.Models
{
    public class AppUser : IdentityUser
    {
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        // PhoneNumber is inherited from IdentityUser

        public int? ResidenceCityId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
