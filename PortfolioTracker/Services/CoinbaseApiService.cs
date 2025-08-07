using PortfolioTracker.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace PortfolioTracker.Services
{
    public class CoinbaseApiService : ICoinbaseApiService
    {
        private readonly HttpClient _httpClient;
        private readonly CoinbaseJwtService _jwtService;
        private readonly string _translateCurrency = "USD";

        public CoinbaseApiService(HttpClient httpClient, CoinbaseJwtService jwtService)
        {
            _httpClient = httpClient;
            _jwtService = jwtService;
        }

        // Gets accounts from Coinbase API (serialized and in JSON format)
        public async Task<string> GetAccountsAsync()
        {
            var basePath = "api/v2/accounts";
            var endpoint = $"api.coinbase.com/{basePath}";
            var queryParams = "?limit=100";

            // JWT token must include the full path with query parameters
            var token = _jwtService.GenerateJwtToken("GET", $"{endpoint}");

            using (var request = new HttpRequestMessage(HttpMethod.Get, $"https://{endpoint}{queryParams}"))
            {
                if (!string.IsNullOrEmpty(token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                
                MessageBox.Show($"Response Status: {response.StatusCode}", "Coinbase API Response", MessageBoxButton.OK, MessageBoxImage.Information);
                Debug.WriteLine($"Response Status: {response.StatusCode}");
                Debug.WriteLine($"Response Content: {content}");
                
                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Error fetching accounts: {response.ReasonPhrase}");
                    throw new HttpRequestException($"Error fetching accounts: {response.ReasonPhrase}");
                }

                return content;
            }
        }

        // Gets accounts data from Coinbase API (serialized and in CoinbaseV2Response)
        public async Task<CoinbaseV2Response> GetAccountsDataAsync()
        {
            var jsonResponse = await GetAccountsAsync();

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var accountsResponse = JsonSerializer.Deserialize<CoinbaseV2Response>(jsonResponse, options);
                Debug.WriteLine($"Number of serialized currencies: {accountsResponse.Data.Count}");

                if (accountsResponse == null)
                {
                    Debug.WriteLine("Deserialized response is null.");
                    throw new InvalidOperationException("Failed to deserialize Coinbase API response.");
                }

                return accountsResponse ?? throw new InvalidOperationException("Nothing was serialized from API response.");
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"JSON parsing error: {ex.Message}");
                Debug.WriteLine($"JSON content: {jsonResponse}");
                throw new InvalidOperationException("Failed to parse Coinbase API response", ex);
            }
        }

        // Gets portfolio items from Coinbase API (serialized and in List<PortfolioItem>)
        public async Task<List<PortfolioItem>> GetPortfolioItemsAsync()
        {
            var accountsResponse = await GetAccountsDataAsync();
            var portfolioItems = new List<PortfolioItem>();

            foreach (var account in accountsResponse.Data.Where(a => a.HasBalance))
            {
                portfolioItems.Add(new PortfolioItem
                {
                    CurrencyName = account.Currency.Code,
                    Balance = account.BalanceValue,
                    APY = account.Currency.Rewards?.FormattedApy ?? "N/A",
                    BalanceTranslated = null,           // THE PRICE HAS TO BE CALCULATED SEPARATELY (ANOTHER API CALL)
                    CurrencyTranslated = _translateCurrency 
                });
            }

            portfolioItems = portfolioItems.OrderByDescending(p => p.Balance).ToList();

            portfolioItems = await AddTranslatedCurrency(portfolioItems);

            return portfolioItems;
        }

        // Adds translated currency to portfolio items (price per unit in _translateCurrency)
        private async Task<List<PortfolioItem>> AddTranslatedCurrency(List<PortfolioItem> items)
        {
            foreach (var item in items) 
            {
                try
                {
                    var singleCryptoItem = await GetSingleCryptoPrice(item.CurrencyName, _translateCurrency);
                    
                    if (singleCryptoItem?.Data?.Amount != null)
                    {
                        if (decimal.TryParse(singleCryptoItem.Data.Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var pricePerUnit))
                        {
                            item.BalanceTranslated = pricePerUnit * item.Balance;
                            Debug.WriteLine($"{item.CurrencyName}: {item.Balance} × ${pricePerUnit} = ${item.BalanceTranslated:F2}");
                        }
                        else
                        {
                            Debug.WriteLine($"Failed to parse price for {item.CurrencyName}: '{singleCryptoItem.Data.Amount}'");
                            item.BalanceTranslated = null;
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"No price data available for {item.CurrencyName}");
                        item.BalanceTranslated = null;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error getting price for {item.CurrencyName}: {ex.Message}");
                    item.BalanceTranslated = null;
                }
            }
            
            return items; 
        }

        // Gets price of single crypto
        private async Task<SingleCryptoItem> GetSingleCryptoPrice(string cryptoName, string baseCurrrency)
        {
            if (string.IsNullOrEmpty(cryptoName)) throw new ArgumentException("Crypto name cannot be null or empty.", nameof(cryptoName));
            if (string.IsNullOrEmpty(baseCurrrency)) throw new ArgumentException("Base currency name cannot be null or empty.", nameof(baseCurrrency));

            using (var client = new HttpClient())
            {
                var jsonResponse = await client.GetStringAsync($"https://api.coinbase.com/v2/prices/{cryptoName}-{baseCurrrency}/spot");

                if (jsonResponse == null) throw new InvalidOperationException("Failed to fetch data from Coinbase API.");

                try
                {

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var singleCrypto = JsonSerializer.Deserialize<SingleCryptoItem>(jsonResponse, options);
                    Debug.WriteLine($"Single crypto loaded from API: {singleCrypto.Data.Base}-{singleCrypto.Data.Currency}");

                    if (singleCrypto == null)
                    {
                        Debug.WriteLine("Deserialized single crypto response is null.");
                        throw new InvalidOperationException("Failed to deserialize Coinbase API response.");
                    }

                    return singleCrypto ?? throw new InvalidOperationException("Nothing was serialized from API response.");
                }
                catch (JsonException ex)
                {
                    Debug.WriteLine($"JSON parsing error: {ex.Message}");
                    Debug.WriteLine($"JSON content: {jsonResponse}");
                    throw new InvalidOperationException("Failed to parse Coinbase API response", ex);
                }
            }
        }
    }
}

