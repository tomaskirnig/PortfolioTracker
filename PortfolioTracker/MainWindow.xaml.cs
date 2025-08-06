using Microsoft.Extensions.DependencyInjection;
using PortfolioTracker.Services;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PortfolioTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ICoinbaseApiService _coinbaseApiService;
        public MainWindow(ICoinbaseApiService _coinbaseApiService)
        {
            this._coinbaseApiService = _coinbaseApiService;
            InitializeComponent();
            LoadData();
        }

        private async void LoadData()
        {
            try
            {
                var items = await _coinbaseApiService.GetAccountsAsync();
                MessageBox.Show(items);
                Debug.WriteLine(items);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba: {ex.Message}, {ex}");
            }
        }


        // Gets price of single crypto
        private async Task<string> GetCryptoPrice()
        {
            string cryptoName = "BTC- USD";
            using (var client = new HttpClient())
            {
                var response = await client.GetStringAsync($"https://api.coinbase.com/v2/prices/{cryptoName}/spot");
                return response;
            }
        }
    }
}