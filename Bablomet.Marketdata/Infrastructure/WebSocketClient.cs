namespace Bablomet.Marketdata.Infrastructure;

using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bablomet.Marketdata.WebSocket;

public class WebSocketClient
{
    private readonly Uri _serverUri;
    private const int ReceiveChunkSize = 1024;
    private const int PingIntervalSeconds = 30;  // Send ping every 30 seconds

    public WebSocketClient(string serverUrl)
    {
        _serverUri = new Uri(serverUrl);
    }

    public async Task ConnectAsync()
    {
        using (var webSocket = new ClientWebSocket())
        {
            await webSocket.ConnectAsync(_serverUri, CancellationToken.None);
            Console.WriteLine($"Connected to {_serverUri}");

            foreach(var instrument in InstrumentsCache.Instruments.Values)
            {
                var request = JsonSerializer.Serialize(new SubscribeInstrumentsDto
                {
                    Code = instrument.Symbol,
                    Exchange = instrument.Exchange
                });
                Console.WriteLine($"Request: {request}");
                await webSocket.SendAsync(Encoding.UTF8.GetBytes(request), WebSocketMessageType.Text, false, CancellationToken.None);
                break;
            }


            var receiveTask = ReceiveMessagesAsync(webSocket);
            var pingTask = SendPeriodicPingAsync(webSocket, CancellationToken.None);

            await Task.WhenAny(receiveTask, pingTask);  // Await any task to complete
        }
    }

    private static async Task ReceiveMessagesAsync(ClientWebSocket webSocket)
    {
        var buffer = new byte[ReceiveChunkSize];
        while (webSocket.State == WebSocketState.Open)
        {
            var stringResult = new StringBuilder();

            WebSocketReceiveResult result;
            do
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    stringResult.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
                    // Pong frame received, you can log or ignore this
                    Console.WriteLine("Pong received.");
                }

            } while (!result.EndOfMessage);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                Console.WriteLine("Server requested close. Closing...");
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
            else if (result.MessageType == WebSocketMessageType.Text)
            {
                Console.WriteLine($"Received message: {stringResult}");
            }
        }

        Console.WriteLine($"Completed receiving messages, websocket state is {webSocket.State}");
    }

    private static async Task SendPeriodicPingAsync(ClientWebSocket webSocket, CancellationToken ct)
    {
        while (webSocket.State == WebSocketState.Open)
        {
            // Sending empty ping frame
            await webSocket.SendAsync(new ArraySegment<byte>(new byte[0]), WebSocketMessageType.Binary, true, ct);
            Console.WriteLine("Ping sent.");
            await Task.Delay(TimeSpan.FromSeconds(PingIntervalSeconds), ct);
        }

        Console.WriteLine($"Completed sending periodic ping, websocket state is {webSocket.State}");
    }
}