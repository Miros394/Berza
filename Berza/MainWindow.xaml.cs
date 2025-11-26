using System;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Windows.Media;

namespace Berza
{
    public partial class MainWindow : Window
    {
        private WebSocketClient binance;
        private WebSocketClient bybit;

        private bool konekcija = false;

        private string? par = null;

        private string ne_par = null;
        private string ne_smer = null;
        private decimal? ne_granica = null;
        private bool notifikacija = false;

        private readonly List<decimal> lista_btc_binance = new();
        private readonly List<decimal> lista_eth_binance = new();

        private readonly List<decimal> lista_btc_bybit = new();
        private readonly List<decimal> lista_eth_bybit = new();

        private int X_broj = 10;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Levi_Klik(object sender, MouseButtonEventArgs ev)
        {
            var tb = sender as TextBlock;
            par = tb.Tag.ToString(); 

            BinanceBtc.FontWeight = FontWeights.Normal;
            BybitBtc.FontWeight = FontWeights.Normal;
            BinanceEth.FontWeight = FontWeights.Normal;
            BybitEth.FontWeight = FontWeights.Normal;

            tb.FontWeight = FontWeights.Bold;

            PrikaziPreporuku(par);
        }

        private void Otvori_Podesavanja(object sender, RoutedEventArgs e)
        {
            try
            {
                var p = new Podesavanja();
                bool? result = p.ShowDialog();
        
                if (result == true)
                {
                    ne_par = p.Par;
                    ne_smer = p.Smer;
                    ne_granica = p.Granica;
                    notifikacija = false;
                    X_broj = p.X;

                    MessageBox.Show($"Notifikacija podešena!\n{ne_par} {ne_smer} ${ne_granica}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri otvaranju podešavanja: {ex.Message}");
            }
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

        private void DodajCenu(List<decimal> lista, decimal cena)
        {
            lista.Add(cena);

            if (lista.Count > 60)
                lista.RemoveAt(0); 
        }

        private decimal Prosek(List<decimal> lista)
        {
            if (lista.Count == 0) return 0;
            decimal suma = 0;

            foreach (var v in lista)
                suma += v;

            return suma / lista.Count;
        }

        private void PrikaziPreporuku(string par)
        {
            int lokalniX = X_broj;

            List<decimal> lista = par switch
            {
                "Binance_Btc" => lista_btc_binance,
                "Bybit_Btc" => lista_btc_bybit,
                "Binance_Eth" => lista_eth_binance,
                "Bybit_Eth" => lista_eth_bybit,
                _ => null
            };

            if (lista == null)
            {
                Preporuka.Text = "Greška: Nepoznat par.";
                return;
            }

            var (tekst, smer, bp) = IzracunajPreporuku(lista, lokalniX);

            Preporuka.Text = tekst;

            if (bp > 0)
                Preporuka.Foreground = Brushes.LimeGreen;
            else
                Preporuka.Foreground = Brushes.Crimson;
        }

        private (string tekst, string smer, int bazniPoeni) IzracunajPreporuku(List<decimal> lista, int X_broj)
        {
            if (lista.Count < X_broj || lista.Count < 60)
                return ("Nema dovoljno podataka", "", 0);

            var poslednjihX = lista.TakeLast(X_broj).ToList();
            var poslednjih60 = lista.TakeLast(60).ToList();

            var prosekX = Prosek(poslednjihX);
            var prosek60 = Prosek(poslednjih60);

            if (prosek60 == 0)
                return ("N/A", "", 0);

            int bp = (int)(((prosekX - prosek60) / prosek60) * 10000);

            string smer = bp > 0 ? "Kupovina" : "Prodaja";
            string strelica = bp > 0 ? "↑" : "↓";

            string tekst = $"{strelica} {smer} (razlika: {bp} baznih poena)";

            return (tekst, smer, bp);
        }

        private void PosaljiNotifikaciju(string poruka)
        {
            try
            {
                MessageBox.Show(poruka, "Berza Notifikacija", MessageBoxButton.OK, MessageBoxImage.Information);

                Console.WriteLine($"NOTIFIKACIJA: {poruka}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri notifikaciji: {ex.Message}");
            }
        }

        private async Task Listen(WebSocketClient client, string source)
        {
            await foreach (var por in client.ReceiveAsync())
            {
                try
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

                    if (!decimal.TryParse(cena, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal c))
                        continue;

                    if (simbol.Contains("BTC", StringComparison.OrdinalIgnoreCase))
                    {
                        if (source == "Binance")
                            DodajCenu(lista_btc_binance, c);
                        else
                            DodajCenu(lista_btc_bybit, c);
                    }
                    else if (simbol.Contains("ETH", StringComparison.OrdinalIgnoreCase))
                    {
                        if (source == "Binance")
                            DodajCenu(lista_eth_binance, c);
                        else
                            DodajCenu(lista_eth_bybit, c);
                    }


                    var color = boja.Equals("Buy", StringComparison.OrdinalIgnoreCase) ? System.Windows.Media.Brushes.LimeGreen : System.Windows.Media.Brushes.Crimson;

                    if (ne_granica != null && !notifikacija)
                    {
                        if (simbol.StartsWith(ne_par, StringComparison.OrdinalIgnoreCase))
                        {
                            if (decimal.TryParse(cena, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal trenutnaCena))
                            {
                                if ((ne_smer == "Iznad" && trenutnaCena > ne_granica) ||
                                    (ne_smer == "Ispod" && trenutnaCena < ne_granica))
                                {
                                    string poruka = $"{ne_par} je {ne_smer.ToLower()} ${ne_granica} - Trenutno: ${trenutnaCena}";
                                    PosaljiNotifikaciju(poruka);
                                    notifikacija = true;
                                }
                            }
                        }
                    }

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
                catch (Exception ex)
                {
                    Console.WriteLine($"Listen parse/process error ({source}): {ex.Message}");
                    continue;
                }
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
