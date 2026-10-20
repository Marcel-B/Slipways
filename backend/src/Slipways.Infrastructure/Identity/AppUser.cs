using Microsoft.AspNetCore.Identity;

namespace Slipways.Infrastructure.Identity;

public class AppUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
