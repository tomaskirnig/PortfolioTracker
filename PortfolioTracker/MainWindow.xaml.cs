using Microsoft.Extensions.DependencyInjection;
using PortfolioTracker.Services;
using PortfolioTracker.Models;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.Windows.Input;

namespace PortfolioTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ICoinbaseApiService _coinbaseApiService;
        private System.Windows.Threading.DispatcherTimer _refreshTimer;
        
        public MainWindow(ICoinbaseApiService _coinbaseApiService)
        {
            this._coinbaseApiService = _coinbaseApiService;
            InitializeComponent();
            InitializeTimer();
            LoadData();
        }

        private void InitializeTimer()
        {
            _refreshTimer = new System.Windows.Threading.DispatcherTimer();
            _refreshTimer.Tick += (s, e) => LoadData(forceRefresh: true);
            _refreshTimer.Interval = TimeSpan.FromMinutes(5);
            _refreshTimer.Start();
        }

        private async void LoadData(bool forceRefresh = false)
        {
            try
            {
                // Get parsed portfolio items
                var portfolioItems = await _coinbaseApiService.GetPortfolioItemsAsync(forceRefresh);

                // Display in DataGrid
                PortfolioDataGrid.ItemsSource = portfolioItems;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba: {ex.Message}");
                Debug.WriteLine($"Error: {ex}");
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            // Visual feedback - Start
            RefreshButton.IsEnabled = false;
            var originalContent = RefreshButton.Content;
            RefreshButton.Content = "Refreshing...";
            Mouse.OverrideCursor = Cursors.Wait;

            // Perform refresh
            LoadData(forceRefresh: true);

            // Visual feedback - End
            Mouse.OverrideCursor = null;
            RefreshButton.Content = "Refreshed!";
            
            // Wait briefly to show success message
            await Task.Delay(1500);

            // Revert state
            RefreshButton.Content = originalContent;
            RefreshButton.IsEnabled = true;
        }
    }
}