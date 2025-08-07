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
                var portfolioItems = await _coinbaseApiService.GetPortfolioItemsAsync();

                MessageBox.Show($"Number of items loaded: {portfolioItems.Count}");

                // Display in DataGrid
                PortfolioDataGrid.ItemsSource = portfolioItems;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Chyba: {ex.Message}");
                Debug.WriteLine($"Error: {ex}");
            }
        }
    }
}