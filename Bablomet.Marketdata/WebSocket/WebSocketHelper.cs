using System;
using System.Security.Authentication;
using System.Threading.Tasks;
using WebSocketSharp;

namespace Bablomet.Marketdata.WebSocket;

public static class WebSocketHelper
{
    public static void Subscribe(
        WebSocketSharp.WebSocket ws,
        Func<WebSocketSharp.WebSocket, Task> onOpen,
        Func<MessageEventArgs, Task> onMessage,
        Func<CloseEventArgs, Task> onClose,
        Func<ErrorEventArgs, Task> onError
    )
    {
        if (ws == null) throw new ArgumentNullException(nameof(ws));

        ws.OnMessage += async (sender, e) =>
        {
            if (onMessage == null) return;

            await onMessage(e);
        };

        ws.OnOpen += async (sender, e) =>
        {
            if (onOpen == null) return;

            await onOpen(ws);
        };

        ws.OnClose += async (sender, e) =>
        {
            if (onClose == null) return;

            await onClose(e);
        };

        ws.OnError += async (sender, e) =>
        {
            if (onError == null) return;

            await onError(e);
        };

        ws.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
        ws.Connect();
    }

    public static WebSocketSharp.WebSocket Subscribe(
        string url,
        Func<WebSocketSharp.WebSocket, Task> onOpen,
        Func<MessageEventArgs, Task> onMessage,
        Func<CloseEventArgs, Task> onClose,
        Func<ErrorEventArgs, Task> onError
    )
    {
        if (string.IsNullOrWhiteSpace(url)) throw new ArgumentNullException(nameof(url));

        var ws = new WebSocketSharp.WebSocket(url);
        Subscribe(
            ws: ws,
            onOpen: onOpen,
            onMessage: onMessage,
            onClose: onClose,
            onError: onError
        );

        return ws;
    }
}