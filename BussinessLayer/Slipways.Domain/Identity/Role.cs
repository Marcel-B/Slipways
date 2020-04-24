using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace com.b_velop.Slipways.Domain.Identity
{
    public class Role : IdentityRole
    {
        public virtual ICollection<UserRole> UserRoles { get; set; }
    }
}
