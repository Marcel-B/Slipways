using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Slipways.Domain;

namespace Slipways.Infrastructure.Websites;

/// <summary>A home page as read, or why it was not; <see cref="Html"/> is null unless the page could be read as HTML.</summary>
public sealed record WebsitePage(string Result, Uri? Url = null, string? Html = null);

/// <summary>
/// Reads home pages of places, politely: robots.txt first (cached per host for the lifetime of the client, i.e. one run),
/// one request at a time with <see cref="WebsiteOptions.RequestSpacing"/> in between, and redirects followed by hand
/// so robots.txt is checked for every host on the way. The handler only connects to public addresses
/// (<see cref="PublicAddressGuard"/>).
/// </summary>
public sealed class WebsiteClient(HttpClient http, IOptions<WebsiteOptions> options, TimeProvider time)
{
    private const int MaxRedirects = 5;
    private const int MaxRobotsBytes = 500 * 1024;

    private readonly Dictionary<string, RobotsTxt?> _robots = [];
    private bool _sentRequest;

    public async Task<WebsitePage> GetHomePageAsync(Uri url, CancellationToken cancellationToken = default)
    {
        try
        {
            for (var redirects = 0; redirects <= MaxRedirects; redirects++)
            {
                if (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps)
                    return new WebsitePage(WebsiteCheckResults.Refused, url);

                var robots = await GetRobotsAsync(url, cancellationToken);
                if (robots is null)
                    return new WebsitePage(WebsiteCheckResults.Failed, url);
                if (!robots.Allows(url))
                    return new WebsitePage(WebsiteCheckResults.Disallowed, url);

                var (status, location, body) = await GetAsync(url, options.Value.MaxPageBytes, htmlOnly: true, cancellationToken);
                if (location is not null)
                {
                    url = new Uri(url, location);
                    continue;
                }
                if (status is not { } code || (int)code >= 400)
                    return new WebsitePage(WebsiteCheckResults.Failed, url);
                return body is null
                    ? new WebsitePage(WebsiteCheckResults.Empty, url)
                    : new WebsitePage(WebsiteCheckResults.Found, url, body);
            }
            return new WebsitePage(WebsiteCheckResults.Failed, url);
        }
        catch (Exception ex) when (IsRefused(ex))
        {
            return new WebsitePage(WebsiteCheckResults.Refused, url);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException or DecoderFallbackException
                                   && !cancellationToken.IsCancellationRequested)
        {
            return new WebsitePage(WebsiteCheckResults.Failed, url);
        }
    }

    /// <summary>The rules for the host of <paramref name="url"/>; null when robots.txt cannot be read, so nothing may be read.</summary>
    private async Task<RobotsTxt?> GetRobotsAsync(Uri url, CancellationToken cancellationToken)
    {
        var key = url.GetLeftPart(UriPartial.Authority);
        if (_robots.TryGetValue(key, out var cached))
            return cached;

        // RFC 9309: missing (4xx) means everything is allowed, unreachable (5xx, no answer) means nothing is.
        var robotsUrl = new Uri(new Uri(key), "/robots.txt");
        RobotsTxt? robots = null;
        for (var redirects = 0; redirects <= MaxRedirects; redirects++)
        {
            var (status, location, body) = await GetAsync(robotsUrl, MaxRobotsBytes, htmlOnly: false, cancellationToken);
            if (location is not null)
            {
                robotsUrl = new Uri(robotsUrl, location);
                continue;
            }
            robots = status switch
            {
                { } code when (int)code is >= 200 and < 300 => RobotsTxt.Parse(body ?? ""),
                { } code when (int)code is >= 400 and < 500 => RobotsTxt.AllowAll,
                _ => null,
            };
            break;
        }
        return _robots[key] = robots;
    }

    /// <summary>
    /// The status, the target of a redirect, and the body as text: at most <paramref name="maxBytes"/>, and with
    /// <paramref name="htmlOnly"/> only for HTML.
    /// </summary>
    private async Task<(HttpStatusCode? Status, Uri? Location, string? Body)> GetAsync(
        Uri url, int maxBytes, bool htmlOnly, CancellationToken cancellationToken)
    {
        if (_sentRequest)
            await Task.Delay(options.Value.RequestSpacing, time, cancellationToken);
        _sentRequest = true;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Value.Timeout);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is { } location)
            return (response.StatusCode, location, null);
        if (!response.IsSuccessStatusCode)
            return (response.StatusCode, null, null);

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (htmlOnly && mediaType is not null && mediaType is not ("text/html" or "application/xhtml+xml"))
            return (response.StatusCode, null, null);

        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        var buffer = new byte[maxBytes];
        var length = 0;
        int read;
        while (length < maxBytes && (read = await stream.ReadAsync(buffer.AsMemory(length), timeout.Token)) > 0)
            length += read;

        return (response.StatusCode, null, Decode(buffer.AsSpan(0, length), response.Content.Headers.ContentType?.CharSet));
    }

    /// <summary>UTF-8 unless the header or a meta tag says Latin-1 (or Windows-1252, its superset, read as Latin-1).</summary>
    private static string Decode(ReadOnlySpan<byte> bytes, string? charset)
    {
        charset ??= Encoding.ASCII.GetString(bytes[..Math.Min(bytes.Length, 2048)]) is var head
            && head.IndexOf("charset=", StringComparison.OrdinalIgnoreCase) is >= 0 and var index
            ? head[(index + 8)..].TrimStart('"', '\'').Split('"', '\'', ';', ' ', '>', '/')[0]
            : null;
        var latin = charset?.Trim().ToLowerInvariant() is "iso-8859-1" or "iso-8859-15" or "latin1" or "windows-1252";
        return (latin ? Encoding.Latin1 : Encoding.UTF8).GetString(bytes);
    }

    private static bool IsRefused(Exception ex)
    {
        for (var inner = ex; inner is not null; inner = inner.InnerException)
        {
            if (inner is NonPublicAddressException)
                return true;
        }
        return false;
    }
}
