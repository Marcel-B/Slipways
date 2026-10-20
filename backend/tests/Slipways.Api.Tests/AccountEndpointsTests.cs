using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Hosting;
using Slipways.Api.Endpoints;

namespace Slipways.Api.Tests;

public class AccountEndpointsTests(SlipwaysApiFactory factory) : IClassFixture<SlipwaysApiFactory>
{
    private readonly HttpClient _client = factory
        .WithWebHostBuilder(builder => builder.UseSetting("Accounts:Enabled", "true"))
        .CreateClient();

    [Fact]
    public async Task Accounts_are_disabled_by_default()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/account/register",
            new { email = "skipper@example.com", password = "Slipway-2026!" },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Me_requires_authentication()
    {
        var response = await _client.GetAsync("/api/account/me", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Registered_user_can_log_in_and_read_own_profile()
    {
        var ct = TestContext.Current.CancellationToken;
        var credentials = new { email = "skipper@example.com", password = "Slipway-2026!" };

        var register = await _client.PostAsJsonAsync("/api/account/register", credentials, ct);
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/account/login", credentials, ct);
        login.EnsureSuccessStatusCode();
        var token = await login.Content.ReadFromJsonAsync<AccessTokenResponse>(ct);
        Assert.NotNull(token);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/account/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        var me = await _client.SendAsync(request, ct);
        me.EnsureSuccessStatusCode();

        var user = await me.Content.ReadFromJsonAsync<UserDto>(ct);
        Assert.Equal(credentials.email, user?.Email);
    }
}
