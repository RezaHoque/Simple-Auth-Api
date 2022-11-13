using Microsoft.AspNetCore.Identity;

namespace Simple_Auth_Api.Models
{
    public class ApplicationUser: IdentityUser
    {
        public string FullName { get; set; }
    }
}
