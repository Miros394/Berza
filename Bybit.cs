using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Berza
{
    public class Bybit : IAsyncDisposable
    {
        private readonly ClientWebSocket _socket = new ClientWebSocket();

        public async Task ConnectAsync()
        {
            await _socket.ConnectAsync(new Uri("wss://stream.bybit.com/v5/public/spot"), CancellationToken.None);

            string subscribe = "{\"op\": \"subscribe\", \"args\": [\"publicTrade.ETHUSDT\", \"publicTrade.BTCUSDT\"]}";
            byte[] bytes = Encoding.UTF8.GetBytes(subscribe);
            await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);

            _ = Task.Run(async () =>
            {
                while (_socket.State == WebSocketState.Open)
                {
                    await Task.Delay(20000);
                    byte[] ping = Encoding.UTF8.GetBytes("ping");
                    await _socket.SendAsync(ping, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            });
        }

        public async IAsyncEnumerable<string> ReceiveMessagesAsync()
        {
            var buffer = new byte[4096];

            while (_socket.State == WebSocketState.Open)
            {
                var result = await _socket.ReceiveAsync(buffer, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    yield break;
                }

                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                yield return message;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_socket.State == WebSocketState.Open)
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
            _socket.Dispose();
        }
    }
}
