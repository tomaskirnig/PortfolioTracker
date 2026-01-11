using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortfolioTracker.Models
{
    public class PortfolioItem
    {
        public string? CurrencyName { get; set; }           // Currency code (e.g., BTC, ETH)
        public decimal Balance { get; set; }                // Balance amount in the specified currency        
        public string? APY { get; set; } = "None";          // Annual Percentage Yield (APY) for the currency
        public decimal? BalanceTranslated { get; set; }     // Balance amount in the specified currency
        public string? CurrencyTranslated { get; set; }     // Specified currency code (e.g., USD, CZK)
        public decimal? Invested { get; set; }              // Total invested amount (Net: Buys - Sells) in USD
    }
}

