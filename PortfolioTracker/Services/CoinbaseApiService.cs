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
        private readonly string _baseAPIPath = "api.coinbase.com/api/v2/accounts";

        public CoinbaseApiService(HttpClient httpClient, CoinbaseJwtService jwtService)
        {
            _httpClient = httpClient;
            _jwtService = jwtService;
        }

        /// <summary>
        /// This method can be used to call any Coinbase API endpoint.
        /// It handles JWT authentication, query parameters, and deserialization of the response.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the API response into</typeparam>
        /// <param name="endpoint">Whole API addres.</param>
        /// <param name="queryParams">Optional query parameters to append to the endpoint (e.g., "?limit=100&idk=123")</param>
        /// <param name="requiresAuth">Whether the endpoint requires JWT authentication (default is true)</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the deserialized API response of type T</returns>
        /// <exception cref="HttpRequestException">Thrown when the API call fails</exception>
        /// <exception cref="InvalidOperationException">Thrown when the response cannot be deserialized</exception>
        private async Task<T> GetApiResponseAsync<T>(string endpoint, string queryParams = "", bool requiresAuth = true)
        {
            try
            {
                string? token = null;
                string fullUrl = $"https://{endpoint}{queryParams}";

                // Generate JWT token if authentication is required
                if (requiresAuth)
                {
                    token = _jwtService.GenerateJwtToken("GET", endpoint);
                }

                using var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);
                
                // Add authorization header if token is available
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"API Call: {fullUrl}");
                Debug.WriteLine($"Response Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Error: {response.ReasonPhrase}");
                    Debug.WriteLine($"Content: {content}");
                    throw new HttpRequestException($"API call failed with status {response.StatusCode}: {response.ReasonPhrase}");
                }

                // Deserialize JSON to specified type
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<T>(content, options);
                
                if (result == null)
                {
                    throw new InvalidOperationException($"Failed to deserialize API response to type {typeof(T).Name}");
                }

                Debug.WriteLine($"Successfully deserialized to {typeof(T).Name}");
                return result;
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"JSON parsing error: {ex.Message}");
                throw new InvalidOperationException($"Failed to parse API response as {typeof(T).Name}", ex);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"API call error: {ex.Message}");
                throw;
            }
        }

        private async Task<string> GetApiResponseRawAsync(string endpoint, string queryParams = "", bool requiresAuth = true)
        {
            try
            {
                string? token = null;
                string fullUrl = $"https://{endpoint}{queryParams}";

                // Generate JWT token if authentication is required
                if (requiresAuth)
                {
                    token = _jwtService.GenerateJwtToken("GET", endpoint);
                }

                using var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);

                // Add authorization header if token is available
                if (!string.IsNullOrEmpty(token))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                var response = await _httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"API Call: {fullUrl}");
                Debug.WriteLine($"Response Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Error: {response.ReasonPhrase}");
                    Debug.WriteLine($"Content: {content}");
                    throw new HttpRequestException($"API call failed with status {response.StatusCode}: {response.ReasonPhrase}");
                }

                return content;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"API call error: {ex.Message}");
                throw;
            }
        }


        // Gets portfolio items from Coinbase API (serialized and in List<PortfolioItem>)
        public async Task<List<PortfolioItem>> GetPortfolioItemsAsync()
        {
            var accountsResponse = await GetApiResponseAsync<CoinbaseV2Response>(_baseAPIPath, "?limit=100");
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

            portfolioItems = await AddTranslatedCurrencyAsync(portfolioItems);

            return portfolioItems;
        }

        // Adds translated currency to portfolio items (price per unit in _translateCurrency)
        private async Task<List<PortfolioItem>> AddTranslatedCurrencyAsync(List<PortfolioItem> items)
        {
            foreach (var item in items) 
            {
                try
                {
                    var singleCryptoItem = await GetSingleCryptoPriceAsync(item.CurrencyName, _translateCurrency);
                    
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
        private async Task<SingleCryptoItem> GetSingleCryptoPriceAsync(string cryptoName, string baseCurrrency)
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

        /// <summary>
        /// Gets all transactions for a specific cryptocurrency account
        /// </summary>
        /// <param name="currency">The currency code (e.g., "BTC", "ETH")</param>
        /// <returns>The transaction data as JSON string</returns>
        /// <exception cref="InvalidOperationException">Thrown when transaction retrieval fails</exception>
        public async Task<string> GetAllTransactionsAsync(string currency)
        {
            try
            {
                var accountsResponse = await GetApiResponseAsync<CoinbaseV2Response>(_baseAPIPath, "?limit=100");
                
                var account = accountsResponse.Data.FirstOrDefault(a => a.Currency.Code.Equals(currency, StringComparison.OrdinalIgnoreCase));
                
                if (account == null)
                {
                    Debug.WriteLine($"No account found for currency: {currency}");
                    throw new InvalidOperationException($"No account found for currency: {currency}");
                }
                
                var accountId = account.Id; // This is the UUID needed for the API
                Debug.WriteLine($"Found account ID for {currency}: {accountId}");
                
                var transactionsEndpoint = $"{_baseAPIPath}/562f7025-d33f-5067-96e2-49e1029a5e55/transactions";
                var response = await GetApiResponseRawAsync(transactionsEndpoint, "?limit=100");
                
                Debug.WriteLine($"Successfully retrieved transactions for {currency}");
                return response;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving transactions for {currency}: {ex.Message}");
                throw new InvalidOperationException($"Failed to retrieve transactions for {currency}", ex);
            }
        }
    }
}

