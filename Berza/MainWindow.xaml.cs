using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Berza
{
    public partial class MainWindow : Window
    {
        private WebSocketClient binance;
        private WebSocketClient bybit;

        private bool konekcija = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs ev)
        {
            if (!konekcija)
            {
                binance = new WebSocketClient("wss://stream.binance.com:443/ws/btcusdt@trade/ethusdt@trade");

                string bybitS = "{\"op\": \"subscribe\", \"args\": [\"publicTrade.BTCUSDT\", \"publicTrade.ETHUSDT\"]}";
                bybit = new WebSocketClient("wss://stream.bybit.com/v5/public/spot", bybitS, Ping: true);

                await binance.ConnectAsync();
                await bybit.ConnectAsync();

                _ = Listen(binance, "Binance");
                _ = Listen(bybit, "Bybit");

                konekcija = true;
                Glavno_Dugme_Slika.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Resources/Stop_Dugme.png"));
            }
            else 
            {
                await binance.DisposeAsync();
                await bybit.DisposeAsync();

                BinanceBtc.Text = "$-";
                BinanceEth.Text = "$-";
                BybitBtc.Text = "$-";
                BybitEth.Text = "$-";

                konekcija = false;
                Glavno_Dugme_Slika.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Resources/Start_Dugme.png"));
            }                
        }

        private async Task Listen(WebSocketClient client, string source)
        {
            await foreach (var por in client.ReceiveAsync())
            {
                using var doc = JsonDocument.Parse(por);
                var root = doc.RootElement;

                string? simbol = root.GetPropertyOrDefault("s") ?? root.GetPropertyOrDefault("symbol");
                string? cena = root.GetPropertyOrDefault("p") ?? root.GetPropertyOrDefault("price");
                string? boja = root.GetPropertyOrDefault("S") ?? root.GetPropertyOrDefault("side");

                if (source == "Binance" && root.TryGetProperty("m", out var maker))
                {
                    bool isSell = maker.GetBoolean();
                    boja = isSell ? "Sell" : "Buy";
                }

                if (source == "Bybit" && root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0)
                {
                    var pocetni = data[0];
                    simbol ??= pocetni.GetPropertyOrDefault("s") ?? pocetni.GetPropertyOrDefault("symbol");
                    cena ??= pocetni.GetPropertyOrDefault("p") ?? pocetni.GetPropertyOrDefault("price");
                    boja ??= pocetni.GetPropertyOrDefault("S") ?? pocetni.GetPropertyOrDefault("side");
                }

                if (simbol is null || cena is null || boja is null) continue;

                var color = boja.Equals("Buy", StringComparison.OrdinalIgnoreCase) ? System.Windows.Media.Brushes.LimeGreen : System.Windows.Media.Brushes.Crimson; 

                Dispatcher.Invoke(() =>
                {
                    if (simbol.Contains("BTC", StringComparison.OrdinalIgnoreCase))
                    {
                        if (source == "Binance")
                        {
                            BinanceBtc.Text = $"${cena}";
                            BinanceBtc.Foreground = color;
                        }
                        else
                        {
                            BybitBtc.Text = $"${cena}";
                            BybitBtc.Foreground = color;
                        }
                    }
                    else if (simbol.Contains("ETH", StringComparison.OrdinalIgnoreCase))
                    {
                        if (source == "Binance")
                        {
                            BinanceEth.Text = $"${cena}";
                            BinanceEth.Foreground = color;
                        }
                        else
                        {
                            BybitEth.Text = $"${cena}";
                            BybitEth.Foreground = color;
                        }
                    }
                });
            }
        }
    }

    public static class Json
    {
        public static string? GetPropertyOrDefault(this JsonElement element, string ime)
        {
            if (element.ValueKind != JsonValueKind.Object) return null;
            if (element.TryGetProperty(ime, out var property))
            {
                return property.ValueKind switch
                {
                    JsonValueKind.String => property.GetString(), JsonValueKind.Number => property.GetRawText(), _ => null
                };
            }
            return null;
        }
    }
}
