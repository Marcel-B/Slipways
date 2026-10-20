using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Slipways.Api.Tests;

public sealed class RateLimitingTests : IAsyncLifetime
{
    private readonly SlipwaysApiFactory _factory = new();
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        await _factory.InitializeAsync();
        _client = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RateLimit:Enabled", "true");
            builder.UseSetting("RateLimit:TokenLimit", "2");
            builder.UseSetting("RateLimit:TokensPerPeriod", "1");
            builder.UseSetting("RateLimit:ReplenishmentPeriod", "01:00:00");
            builder.ConfigureServices(services => services.AddTransient<IStartupFilter, FromNginx>());
        }).CreateClient();
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    [Fact]
    public async Task Limits_each_client_behind_the_proxies_on_its_own()
    {
        Assert.Equal(HttpStatusCode.OK, await Get("/api/extras", "203.0.113.1, 172.18.0.2"));
        Assert.Equal(HttpStatusCode.OK, await Get("/api/services", "203.0.113.1, 172.18.0.2"));

        var limited = await Send("/api/extras", "203.0.113.1, 172.18.0.2");
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.True(limited.Headers.RetryAfter?.Delta > TimeSpan.Zero);

        // A client cannot get a fresh bucket by sending its own X-Forwarded-For ahead of the real one.
        Assert.Equal(HttpStatusCode.TooManyRequests, await Get("/api/extras", "198.51.100.7, 203.0.113.1, 172.18.0.2"));

        Assert.Equal(HttpStatusCode.OK, await Get("/api/extras", "203.0.113.2, 172.18.0.2"));
        Assert.Equal(HttpStatusCode.OK, await Get("/health", "203.0.113.1, 172.18.0.2"));
    }

    [Theory]
    [InlineData("203.0.113.1", "203.0.113.1")]
    [InlineData("::ffff:203.0.113.1", "203.0.113.1")]
    [InlineData("2001:db8:1:2:3:4:5:6", "2001:db8:1:2::")]
    public void Groups_ipv6_clients_by_their_network(string address, string key) =>
        Assert.Equal(key, RateLimiting.ClientKey(IPAddress.Parse(address)));

    private async Task<HttpStatusCode> Get(string path, string forwardedFor) =>
        (await Send(path, forwardedFor)).StatusCode;

    private Task<HttpResponseMessage> Send(string path, string forwardedFor)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Forwarded-For", forwardedFor);
        return _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>The test server has no remote address; in production the request comes from nginx.</summary>
    private sealed class FromNginx : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((HttpContext context, RequestDelegate nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = IPAddress.Parse("172.18.0.3");
                return nextMiddleware(context);
            });
            next(app);
        };
    }
}

public sealed class ProductionExposureTests : IAsyncLifetime
{
    private readonly SlipwaysApiFactory _factory = new();
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        await _factory.InitializeAsync();
        _client = _factory.WithWebHostBuilder(b => b.UseEnvironment(Environments.Production)).CreateClient();
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar")]
    [InlineData("/scalar/v1")]
    [InlineData("/api/account/login")]
    public async Task Production_does_not_expose_docs_or_accounts(string path)
    {
        var response = await _client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Other_sites_cannot_read_the_api_from_a_browser()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/extras");
        request.Headers.Add("Origin", "https://example.com");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
