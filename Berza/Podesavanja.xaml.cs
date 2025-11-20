using System;
using System.Globalization;
using System.Windows;

namespace Berza
{
    public partial class Podesavanja : Window
    {
        public string Par { get; private set; }
        public string Smer { get; private set; }
        public decimal? Granica { get; private set; }

        public Podesavanja()
        {
            InitializeComponent();

            Btc.IsChecked = true;
            Iznad.IsChecked = true;
        }

        private void Upamti(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Btc.IsChecked != true && Eth.IsChecked != true)
                {
                    MessageBox.Show("Izaberite valutu!");
                    return;
                }

                if (Iznad.IsChecked != true && Ispod.IsChecked != true)
                {
                    MessageBox.Show("Izaberite smer granice!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(TB_Granica.Text))
                {
                    MessageBox.Show("Unesite graničnu cenu!");
                    return;
                }

                if (!decimal.TryParse(TB_Granica.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal granica))
                {
                    MessageBox.Show("Unesite validnu cenu!");
                    return;
                }

                Par = Btc.IsChecked == true ? "BTC" : "ETH";
                Smer = Iznad.IsChecked == true ? "Iznad" : "Ispod";
                Granica = granica;

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška: {ex.Message}");
            }
        }
    }
}