using System;
using com.b_velop.Slipways.Domain.Identity;

namespace com.b_velop.Slipways.Application.Interfaces
{
    public interface IJwtGenerator
    {
        string CreateToken(AppUser user);
    }
}
