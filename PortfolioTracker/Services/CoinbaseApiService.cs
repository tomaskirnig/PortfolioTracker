using System;
using System.Collections.Generic;
using System.Net.Http;
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
            var endpoint = "/api/v3/brokerage/accounts";
            var token = _jwtService.GenerateJwtToken("GET", endpoint);

            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetStringAsync($"https://api.coinbase.com{endpoint}");
            return response;
        }
    }
}

