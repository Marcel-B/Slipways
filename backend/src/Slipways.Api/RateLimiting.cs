using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Slipways.Api;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public bool Enabled { get; set; } = true;

    /// <summary>Requests a client may send in a burst. Opening the web app takes about ten.</summary>
    public int TokenLimit { get; set; } = 120;

    /// <summary>Requests a client gets back per <see cref="ReplenishmentPeriod"/>.</summary>
    public int TokensPerPeriod { get; set; } = 60;

    public TimeSpan ReplenishmentPeriod { get; set; } = TimeSpan.FromSeconds(10);
}

/// <summary>
/// Limits the API per client address, so a single client cannot flood the server. The limits are generous
/// because many app users can share one address (mobile carriers, company networks).
/// </summary>
public static class RateLimiting
{
    public const string ApiPolicy = "api";

    // Caddy and nginx sit in front of the API inside the Docker network. Caddy replaces any X-Forwarded-For a
    // client sends, nginx appends Caddy's address, so the client is at most two hops back.
    private static readonly string[] ProxyNetworks =
        ["10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16", "fc00::/7"];

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitOptions>().BindConfiguration(RateLimitOptions.SectionName);

        services.Configure<ForwardedHeadersOptions>(forwarded =>
        {
            forwarded.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
            forwarded.ForwardLimit = 2;
            foreach (var network in ProxyNetworks)
                forwarded.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
        });

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                return ValueTask.CompletedTask;
            };

            limiter.AddPolicy(ApiPolicy, context =>
            {
                var options = context.RequestServices.GetRequiredService<IOptions<RateLimitOptions>>().Value;
                if (!options.Enabled)
                    return RateLimitPartition.GetNoLimiter("off");

                return RateLimitPartition.GetTokenBucketLimiter(ClientKey(context.Connection.RemoteIpAddress), _ =>
                    new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = options.TokenLimit,
                        TokensPerPeriod = options.TokensPerPeriod,
                        ReplenishmentPeriod = options.ReplenishmentPeriod,
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    });
            });
        });

        return services;
    }

    /// <summary>One bucket per IPv4 address and per IPv6 /64, which is what a single connection usually gets.</summary>
    public static string ClientKey(IPAddress? address)
    {
        if (address is null)
            return string.Empty;
        if (address.IsIPv4MappedToIPv6)
            return address.MapToIPv4().ToString();
        if (address.AddressFamily != AddressFamily.InterNetworkV6)
            return address.ToString();

        var bytes = address.GetAddressBytes();
        Array.Clear(bytes, 8, 8);
        return new IPAddress(bytes).ToString();
    }
}
