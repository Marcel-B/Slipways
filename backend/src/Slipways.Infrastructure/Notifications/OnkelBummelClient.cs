using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Slipways.Infrastructure.Notifications;

/// <param name="Click">Address opened when the message is tapped.</param>
public sealed record OnkelBummelMessage(string Title, string Message, Uri? Click)
{
    /// <summary>
    /// Same key for the same message, so a repeated send after a lost answer does not show up twice; Onkel Bummel
    /// answers a repeat within 24 hours with the stored message.
    /// </summary>
    public string? IdempotencyKey { get; init; }
}

/// <summary>
/// Publishes messages to a topic of the own Onkel Bummel server (<c>POST /api/v1/messages</c>), authenticated with a
/// publish token (<c>ob_…</c>) that is limited to the topic.
/// </summary>
public sealed class OnkelBummelClient(HttpClient http, IOptions<NotificationOptions> options)
{
    public const string MessagesPath = "api/v1/messages";

    // Default priority of Onkel Bummel: a normal push with sound.
    private const int Priority = 3;

    public async Task SendAsync(OnkelBummelMessage message, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, MessagesPath)
        {
            Content = JsonContent.Create(new
            {
                topic = options.Value.Topic,
                title = message.Title,
                message = message.Message,
                priority = Priority,
                tags = new[] { "anchor" },
                click = message.Click?.ToString(),
            }, options: JsonSerializerOptions.Web),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.Token);
        if (message.IdempotencyKey is { Length: > 0 } key)
            request.Headers.Add("Idempotency-Key", key);

        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(await DescribeAsync(response, cancellationToken), null, response.StatusCode);
    }

    // Errors come as problem JSON (RFC 7807); title and detail say what was wrong, e.g. a topic the token may not use.
    private static async Task<string> DescribeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var status = $"Onkel Bummel answered {(int)response.StatusCode} {response.ReasonPhrase}";
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<Problem>(JsonSerializerOptions.Web, cancellationToken);
            var text = string.Join(": ", new[] { problem?.Title, problem?.Detail }.Where(t => !string.IsNullOrWhiteSpace(t)));
            return text.Length > 0 ? $"{status}: {text}" : status;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            return status;
        }
    }

    private sealed record Problem(string? Title, string? Detail);
}
