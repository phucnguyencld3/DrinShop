using Microsoft.AspNetCore.Identity;

namespace DrinShop.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public User? Customer { get; set; }

    }
}