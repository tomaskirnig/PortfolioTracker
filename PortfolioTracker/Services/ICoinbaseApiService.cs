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
    }
}
