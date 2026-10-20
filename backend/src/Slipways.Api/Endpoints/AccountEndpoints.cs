using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Slipways.Infrastructure.Identity;

namespace Slipways.Api.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var account = app.MapGroup("/api/account").WithTags("Account").RequireRateLimiting(RateLimiting.ApiPolicy);

        // register, login, refresh, confirmEmail, manage/info, ... (bearer tokens for web and mobile clients)
        account.MapIdentityApi<AppUser>();

        account.MapGet("/me", GetCurrentUser).RequireAuthorization();

        return app;
    }

    private static async Task<Results<Ok<UserDto>, UnauthorizedHttpResult>> GetCurrentUser(
        ClaimsPrincipal principal, UserManager<AppUser> userManager)
    {
        var user = await userManager.GetUserAsync(principal);
        return user is null
            ? TypedResults.Unauthorized()
            : TypedResults.Ok(new UserDto(user.Id, user.Email ?? string.Empty, user.DisplayName));
    }
}
