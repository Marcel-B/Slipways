using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Slipways.Infrastructure.Notifications;

/// <param name="Click">Address opened when the message is tapped.</param>
public sealed record NtfyMessage(string Title, string Message, Uri? Click);

/// <summary>Publishes messages to a topic of an ntfy server (https://docs.ntfy.sh/publish/).</summary>
public sealed class NtfyClient(HttpClient http, IOptions<NotificationOptions> options)
{
    public async Task SendAsync(NtfyMessage message, CancellationToken cancellationToken = default)
    {
        // Published as JSON to the root, so title and text may hold umlauts, which headers may not.
        using var request = new HttpRequestMessage(HttpMethod.Post, "")
        {
            Content = JsonContent.Create(new
            {
                topic = options.Value.Topic,
                title = message.Title,
                message = message.Message,
                click = message.Click?.ToString(),
                tags = new[] { "anchor" },
            }, options: JsonSerializerOptions.Web),
        };
        if (options.Value.Token is { Length: > 0 } token)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
