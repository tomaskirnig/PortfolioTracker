using PortfolioTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PortfolioTracker.Services
{
    public interface ICoinbaseApiService
    {
        Task<string> GetAccountsAsync();
        Task<CoinbaseV2Response> GetAccountsDataAsync();
        Task<List<PortfolioItem>> GetPortfolioItemsAsync();
    }
}
