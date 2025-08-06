using Microsoft.Extensions.DependencyInjection;
using PortfolioTracker.Services;
using PortfolioTracker.Models;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;

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
                // Get parsed portfolio items
                var portfolioItems = await _coinbaseApiService.GetAccountsAsync();

                MessageBox.Show(portfolioItems);
                Debug.WriteLine(portfolioItems);

                // Display in DataGrid
                PortfolioDataGrid.ItemsSource = portfolioItems;
                
                // Show summary
                //var totalAccounts = portfolioItems.Count;
                //var activeBalances = portfolioItems.Count(p => p.Balance > 0);
                
                //MessageBox.Show($"Portfolio loaded successfully!\n" +
                //               $"Total accounts with balance: {activeBalances}\n" +
                //               $"Total accounts: {totalAccounts}");
                
                //Debug.WriteLine($"Loaded {portfolioItems.Count} portfolio items");
                //foreach (var item in portfolioItems.Take(5)) // Log first 5 items
                //{
                //    Debug.WriteLine($"{item.Currency}: {item.BalanceFormatted}");
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba: {ex.Message}");
                Debug.WriteLine($"Error: {ex}");
            }
        }

        // Alternative method to get raw parsed data
        private async void LoadRawData()
        {
            try
            {
                var accountsData = await _coinbaseApiService.GetAccountsDataAsync();
                
                Debug.WriteLine($"Total accounts: {accountsData.Accounts.Count}");
                Debug.WriteLine($"Has more data: {accountsData.HasNext}");
                
                var accountsWithBalance = accountsData.Accounts
                    .Where(a => a.HasBalance)
                    .OrderByDescending(a => a.BalanceValue)
                    .ToList();
                
                foreach (var account in accountsWithBalance)
                {
                    Debug.WriteLine($"{account.Currency}: {account.BalanceValue} (Updated: {account.UpdatedAt:yyyy-MM-dd})");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading raw data: {ex.Message}");
            }
        }

        // Gets price of single crypto
        private async Task<string> GetCryptoPrice()
        {
            string cryptoName = "BTC-USD";
            using (var client = new HttpClient())
            {
                var response = await client.GetStringAsync($"https://api.coinbase.com/v2/prices/{cryptoName}/spot");
                return response;
            }
        }
    }
}