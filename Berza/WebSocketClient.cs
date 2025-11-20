using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
                throw;
            }
        }

        public async IAsyncEnumerable<string> ReceiveAsync()
        {
            var buffer = new byte[64 * 1024];

            while (socket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                using var ms = new MemoryStream();

                do
                {
                    result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        yield break;
                    }

                    ms.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    using var reader = new StreamReader(ms, Encoding.UTF8);
                    var text = await reader.ReadToEndAsync();
                    yield return text;
                }
                else if (result.MessageType == WebSocketMessageType.Binary)
                {
                    var payload = ms.ToArray();
                    string text;

                    if (IsGZip(payload))
                        text = DecompressGzip(payload);
                    else
                        text = TryDecompressDeflate(payload) ?? Encoding.UTF8.GetString(payload);

                    yield return text;
                }
                else
                {
                    yield return string.Empty;
                }
            }
        }

        private static bool IsGZip(byte[] data)
        {
            return data.Length >= 2 && data[0] == 0x1f && data[1] == 0x8b;
        }

        private static string DecompressGzip(byte[] data)
        {
            try
            {
                using var ms = new MemoryStream(data);
                using var gz = new GZipStream(ms, CompressionMode.Decompress);
                using var sr = new StreamReader(gz, Encoding.UTF8);
                return sr.ReadToEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine("GZip decompress failed: " + ex.Message);
                return string.Empty;
            }
        }

        private static string? TryDecompressDeflate(byte[] data)
        {
            try
            {
                using var ms = new MemoryStream(data);
                using var ds = new DeflateStream(ms, CompressionMode.Decompress);
                using var sr = new StreamReader(ds, Encoding.UTF8);
                return sr.ReadToEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Deflate decompress failed: " + ex.Message);
                return null;
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
