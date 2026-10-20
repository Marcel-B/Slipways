using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace Slipways.Infrastructure.Websites;

/// <summary>
/// Connects only to public internet addresses. Website links come from OpenStreetMap, which anyone can edit, so a link
/// must not make the server talk to itself, to the tailnet or to anything else in a private network.
/// </summary>
public static class PublicAddressGuard
{
    /// <summary>A <see cref="SocketsHttpHandler.ConnectCallback"/> that resolves the host itself and skips non-public addresses.</summary>
    public static async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var host = context.DnsEndPoint.Host;
        var addresses = IPAddress.TryParse(host, out var literal)
            ? [literal]
            : await Dns.GetHostAddressesAsync(host, cancellationToken);
        var allowed = addresses.Where(IsPublic).ToArray();
        if (allowed.Length == 0)
            throw new NonPublicAddressException(host);

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(allowed, context.DnsEndPoint.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    public static bool IsPublic(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            return !(b[0] is 0 or 10 or 127 or >= 224
                || (b[0] == 100 && b[1] is >= 64 and < 128)      // carrier-grade NAT, also used by Tailscale
                || (b[0] == 169 && b[1] == 254)                  // link-local
                || (b[0] == 172 && b[1] is >= 16 and < 32)
                || (b[0] == 192 && b[1] == 168)
                || (b[0] == 192 && b[1] == 0 && b[2] is 0 or 2)  // protocol assignments, documentation
                || (b[0] == 198 && b[1] is 18 or 19)             // benchmarking
                || (b[0] == 198 && b[1] == 51 && b[2] == 100)
                || (b[0] == 203 && b[1] == 0 && b[2] == 113));
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // Only global unicast (2000::/3), which also leaves out NAT64 and the like, without documentation addresses.
            var b = address.GetAddressBytes();
            return (b[0] & 0xE0) == 0x20
                && !(b[0] == 0x20 && b[1] == 0x01 && b[2] == 0x0D && b[3] == 0xB8)
                && !(b[0] == 0x20 && b[1] == 0x02);  // 6to4 embeds an IPv4 address
        }

        return false;
    }
}

/// <summary>Thrown when a host resolves to no public address; see <see cref="PublicAddressGuard"/>.</summary>
public sealed class NonPublicAddressException(string host)
    : Exception($"'{host}' does not resolve to a public address.");
