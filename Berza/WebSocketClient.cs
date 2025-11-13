using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Berza
{
    public class WebSocketClient : IAsyncDisposable
    {
        private ClientWebSocket socket;
        private string url;
        private string poruka;
        private bool ping;

        public WebSocketClient(string url, string Poruka = null, bool Ping = false)
        {
            this.url = url;
            this.poruka = Poruka;
            this.ping = Ping;
            socket = new ClientWebSocket();
        }

        public async Task ConnectAsync()
        {
            try
            {
                Console.WriteLine("Povezivanje na " + url);
                await socket.ConnectAsync(new Uri(url), CancellationToken.None);
                Console.WriteLine("Povezano");

                if (!string.IsNullOrEmpty(poruka))
                {
                    var bytes = Encoding.UTF8.GetBytes(poruka);
                    await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
                    Console.WriteLine("Poslata subscribe poruka.");
                }

                if (ping)
                {
                    _ = Task.Run(async () =>
                    {
                        while (socket.State == WebSocketState.Open)
                        {
                            await Task.Delay(20000);
                            try
                            {
                                var pingData = Encoding.UTF8.GetBytes("ping");
                                await socket.SendAsync(pingData, WebSocketMessageType.Text, true, CancellationToken.None);
                                Console.WriteLine("Ping poslat.");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Ping greška: " + ex.Message);
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greška prilikom konekcije: " + ex.Message);
            }
        }

        public async IAsyncEnumerable<string> ReceiveAsync()
        {
            var buffer = new byte[4096];
            while (socket.State == WebSocketState.Open)
            {
                var rezultat = await socket.ReceiveAsync(buffer, CancellationToken.None);
                if (rezultat.MessageType == WebSocketMessageType.Close)
                    yield break;

                yield return Encoding.UTF8.GetString(buffer, 0, rezultat.Count);
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (socket.State == WebSocketState.Open)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Zatvaranje veze", CancellationToken.None);
                    Console.WriteLine("Veza zatvorena.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greška prilikom zatvaranja: " + ex.Message);
            }

            socket.Dispose();
        }
    }
}
