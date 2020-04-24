using System;
using Microsoft.AspNetCore.Identity;

namespace com.b_velop.Slipways.Domain.Identity
{
    public class UserRole : IdentityUserRole<string>
    {
        public virtual AppUser User { get; set; }
        public virtual Role Role { get; set; }
    }
}
