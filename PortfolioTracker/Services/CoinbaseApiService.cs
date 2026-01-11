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

        // Caching fields
        private List<PortfolioItem>? _cachedPortfolio;
        private DateTime _lastFetchTime;
        private const int CacheDurationMinutes = 5;

        public CoinbaseApiService(HttpClient httpClient, CoinbaseJwtService jwtService)
        {
            _httpClient = httpClient;
            _jwtService = jwtService;
        }
        
        // ... (GetApiResponseAsync and GetApiResponseRawAsync methods remain unchanged)
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
        public async Task<List<PortfolioItem>> GetPortfolioItemsAsync(bool forceRefresh = false)
        {
            // Check cache validity
            if (!forceRefresh && _cachedPortfolio != null && DateTime.Now < _lastFetchTime.AddMinutes(CacheDurationMinutes))
            {
                Debug.WriteLine("Returning cached portfolio data.");
                return _cachedPortfolio;
            }

            Debug.WriteLine("Fetching new portfolio data from API...");
            var accountsResponse = await GetApiResponseAsync<CoinbaseV2Response>(_baseAPIPath, "?limit=100");
            var rawItems = new List<PortfolioItem>();

            // 1. Collect ALL accounts (no filtering by HasBalance yet)
            foreach (var account in accountsResponse.Data)
            {
                rawItems.Add(new PortfolioItem
                {
                    CurrencyName = account.Currency.Code,
                    Balance = account.BalanceValue,
                    APY = account.Currency.Rewards?.FormattedApy ?? "N/A", // Note: APY might differ per account, taking last/first found
                    BalanceTranslated = null,
                    CurrencyTranslated = _translateCurrency 
                });
            }

            // 2. Group by Currency Code and Sum Balances
            var groupedItems = rawItems
                .GroupBy(i => i.CurrencyName)
                .Select(g => new PortfolioItem
                {
                    CurrencyName = g.Key,
                    Balance = g.Sum(x => x.Balance),
                    // For APY, we'll take the first one that isn't "N/A", or default to "N/A"
                    APY = g.FirstOrDefault(x => x.APY != "N/A")?.APY ?? "N/A",
                    CurrencyTranslated = _translateCurrency,
                    BalanceTranslated = null
                })
                .Where(i => i.Balance > 0 || string.Equals(i.CurrencyName, "BTC", StringComparison.OrdinalIgnoreCase)) // Keep positive balance OR BTC
                .ToList();

            // 3. If BTC is still missing (0 balance and wasn't in API at all), add fallback
            if (!groupedItems.Any(p => string.Equals(p.CurrencyName, "BTC", StringComparison.OrdinalIgnoreCase)))
            {
                Debug.WriteLine("[DEBUG] Manually adding fallback BTC account.");
                groupedItems.Add(new PortfolioItem
                {
                    CurrencyName = "BTC",
                    Balance = 0,
                    APY = "N/A",
                    BalanceTranslated = null,
                    CurrencyTranslated = _translateCurrency
                });
            }

            // 4. Calculate prices AND fetch invested amounts for the aggregated list
            // We can do price fetching and transaction fetching in parallel for each item
            var tasks = groupedItems.Select(async item =>
            {
                // Task 1: Get Price/Value
                try
                {
                    var singleCryptoItem = await GetSingleCryptoPriceAsync(item.CurrencyName, _translateCurrency);
                    if (decimal.TryParse(singleCryptoItem.Data.Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var pricePerUnit))
                    {
                        item.BalanceTranslated = pricePerUnit * item.Balance;
                    }
                }
                catch { /* Ignore price errors */ }

                // Task 2: Get Invested Amount (only if we have accounts for this currency)
                // We need to find the Account IDs associated with this currency from the original 'accountsResponse'
                // But we lost them in the GroupBy. We should look them up again.
                var accountIds = accountsResponse.Data
                    .Where(a => a.Currency.Code == item.CurrencyName)
                    .Select(a => a.Id)
                    .ToList();

                decimal? totalInvested = null; // Changed to nullable
                foreach (var accountId in accountIds)
                {
                    try 
                    {
                        var invested = await GetAccountInvestedAmountAsync(accountId);
                        if (invested != null)
                        {
                            totalInvested = (totalInvested ?? 0) + invested;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to get transactions for account {accountId}: {ex.Message}");
                    }
                }
                item.Invested = totalInvested ?? 0; // Default to 0 if null, or keep null if you want UI to show differently

                return item;
            });

            await Task.WhenAll(tasks);

            // 5. Sort: BTC first, then by Value
            groupedItems = groupedItems
                .OrderByDescending(p => string.Equals(p.CurrencyName, "BTC", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(p => p.BalanceTranslated ?? 0)
                .ToList();

            // Update cache
            _cachedPortfolio = groupedItems;
            _lastFetchTime = DateTime.Now;

            return groupedItems;
        }

        // Helper to get invested amount for a single account ID
        private async Task<decimal?> GetAccountInvestedAmountAsync(string accountId)
        {
            decimal invested = 0;
            
            // Initial call
            var currentEndpoint = $"{_baseAPIPath}/{accountId}/transactions";
            var currentQueryParams = "?limit=100";
            bool hasMorePages = true;

            while (hasMorePages)
            {
                CoinbaseV2TransactionResponse response;
                try 
                {
                    response = await GetApiResponseAsync<CoinbaseV2TransactionResponse>(currentEndpoint, currentQueryParams);
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("NotFound") || ex.Message.Contains("404"))
                {
                    Debug.WriteLine($"[WARN] Transactions endpoint not found for account {accountId}. Check API Key permissions (wallet:transactions:read).");
                    return null; // Signal that we couldn't fetch data
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error fetching transactions page: {ex.Message}");
                    return null; 
                }

                foreach (var tx in response.Data)
                {
                    if (decimal.TryParse(tx.NativeAmount.Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                    {
                        if (string.Equals(tx.Type, "buy", StringComparison.OrdinalIgnoreCase))
                        {
                            invested += amount;
                        }
                        else if (string.Equals(tx.Type, "sell", StringComparison.OrdinalIgnoreCase))
                        {
                            invested -= amount; 
                        }
                    }
                }

                // Check for next page
                if (response.Pagination != null && !string.IsNullOrEmpty(response.Pagination.NextUri))
                {
                    currentEndpoint = "api.coinbase.com" + response.Pagination.NextUri;
                    currentQueryParams = ""; 
                    await Task.Delay(100); 
                }
                else
                {
                    hasMorePages = false;
                }
            }
            
            return invested;
        }

        // Adds translated currency to portfolio items (price per unit in _translateCurrency)
        private async Task<List<PortfolioItem>> AddTranslatedCurrencyAsync(List<PortfolioItem> items)
        {
            // This method is now effectively replaced by the parallel block above, 
            // but kept if needed by other logic, though currently unused in the new flow.
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
                
                var transactionsEndpoint = $"{_baseAPIPath}/{accountId}/transactions";
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

