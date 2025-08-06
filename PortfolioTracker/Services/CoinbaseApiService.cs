using PortfolioTracker.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Policy;
using System.Text.Json;
using System.Threading.Tasks;

namespace PortfolioTracker.Services
{
    public class CoinbaseApiService : ICoinbaseApiService
    {
        private readonly HttpClient _httpClient;
        private readonly CoinbaseJwtService _jwtService;

        public CoinbaseApiService(HttpClient httpClient, CoinbaseJwtService jwtService)
        {
            _httpClient = httpClient;
            _jwtService = jwtService;
        }

        public async Task<string> GetAccountsAsync()
        {
            var uuid = "4b94d687-885e-50d9-bfd9-2a82c7cb13f4";
            var endpoint = "api.coinbase.com/api/v3/brokerage/accounts"; 
            var token = _jwtService.GenerateJwtToken("GET", endpoint);

            using (var request = new HttpRequestMessage(HttpMethod.Get, $"https://{endpoint}"))
            {
                if (!string.IsNullOrEmpty(token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request);
                return await response.Content.ReadAsStringAsync();
            }
        }

        public async Task<CoinbaseAccountsResponse> GetAccountsDataAsync()
        {
            var jsonResponse = await GetAccountsAsync();
            Debug.WriteLine($"Raw JSON response: {jsonResponse}");

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var accountsResponse = JsonSerializer.Deserialize<CoinbaseAccountsResponse>(jsonResponse, options);
                return accountsResponse ?? new CoinbaseAccountsResponse();
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"JSON parsing error: {ex.Message}");
                throw new InvalidOperationException("Failed to parse Coinbase API response", ex);
            }
        }

        public async Task<List<PortfolioItem>> GetPortfolioItemsAsync()
        {
            var accountsResponse = await GetAccountsDataAsync();
            var portfolioItems = new List<PortfolioItem>();

            foreach (var account in accountsResponse.Accounts.Where(a => a.HasBalance && a.Active))
            {
                portfolioItems.Add(new PortfolioItem
                {
                    Currency = account.Currency,
                    WalletName = account.Name,
                    Balance = account.BalanceValue,
                    LastUpdated = account.UpdatedAt,
                    Status = account.Ready ? "Ready" : "Not Ready",
                    IsActive = account.Active
                });
            }

            return portfolioItems.OrderByDescending(p => p.Balance).ToList();
        }
    }
}

