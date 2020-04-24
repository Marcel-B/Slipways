using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace com.b_velop.Slipways.Domain.Identity
{
    public class AppUser : IdentityUser
    {
        public string DisplayName { get; set; }
        public string Token { get; set; }
        public string Image { get; set; }
        public virtual ICollection<UserRole> UserRoles { get; set; }

    }
}
