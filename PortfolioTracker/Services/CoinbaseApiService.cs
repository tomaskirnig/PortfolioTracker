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
            var endpoint = "api.coinbase.com/api/v3/brokerage/accounts"; 
            var token = _jwtService.GenerateJwtToken("GET", endpoint);
            //var stringResponse = "";

            using (var request = new HttpRequestMessage(HttpMethod.Get, $"https://{endpoint}"))
            {
                if (!string.IsNullOrEmpty(token))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = _httpClient.SendAsync(request).Result;
                return response.Content.ReadAsStringAsync().Result;
            }

            //_httpClient.DefaultRequestHeaders.Authorization =
            //    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            //var response = await _httpClient.GetStringAsync($"https://api.coinbase.com/{endpoint}");
            //return stringResponse;
        }
    }
}

