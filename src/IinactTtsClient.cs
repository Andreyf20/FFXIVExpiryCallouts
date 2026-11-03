using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace StatusExpiryCallouts;

public sealed class IinactTtsClient
{
    public async Task SpeakAsync(string endpoint, string text, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "ws" && uri.Scheme != "wss"))
        {
            throw new InvalidOperationException("Enter a valid IINACT WebSocket URL, such as ws://127.0.0.1:10501/ws.");
        }

        var payload = JsonSerializer.Serialize(new { call = "say", text });
        var bytes = Encoding.UTF8.GetBytes(payload);

        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
        await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Callout sent", cancellationToken).ConfigureAwait(false);
    }
}
