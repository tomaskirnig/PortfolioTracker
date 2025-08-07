using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Serialization;

namespace PortfolioTracker.Models
{
    public class CoinbaseV2Response
    {
        [JsonPropertyName("data")]
        public List<CoinbaseV2Account> Data { get; set; } = new();

        [JsonPropertyName("pagination")]
        public Pagination? Pagination { get; set; }
    }

    public class CoinbaseV2Account
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("primary")]
        public bool Primary { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("balance")]
        public V2Balance Balance { get; set; } = new();

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("resource")]
        public string Resource { get; set; } = string.Empty;

        [JsonPropertyName("resource_path")]
        public string ResourcePath { get; set; } = string.Empty;

        [JsonPropertyName("currency")]
        public CurrencyInfo Currency { get; set; } = new();

        [JsonPropertyName("allow_deposits")]
        public bool AllowDeposits { get; set; }

        [JsonPropertyName("allow_withdrawals")]
        public bool AllowWithdrawals { get; set; }

        [JsonPropertyName("portfolio_id")]
        public string? PortfolioId { get; set; }

        // Computed values for better handeling
        public decimal BalanceValue 
        { 
            get
            {
                if (decimal.TryParse(Balance.Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
                {
                    return value;
                }
                Debug.WriteLine($"Failed to parse balance: '{Balance.Amount}' for {Currency.Code}");
                return 0;
            }
        }
        
        public bool HasBalance => BalanceValue > 0;
        public string CurrencyCode => Currency.Code;
    }

    public class V2Balance
    {
        [JsonPropertyName("amount")]
        public string Amount { get; set; } = "0";

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;
    }

    public class CurrencyInfo
    {
        [JsonPropertyName("asset_id")]
        public string? AssetId { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("color")]
        public string? Color { get; set; }

        [JsonPropertyName("exponent")]
        public int Exponent { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("slug")]
        public string? Slug { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("rewards")]
        public RewardInfo? Rewards { get; set; }
    }

    public class RewardInfo
    {
        [JsonPropertyName("apy")]
        public string Apy { get; set; } = "0";

        [JsonPropertyName("formatted_apy")]
        public string FormattedApy { get; set; } = string.Empty;

        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;
    }

    public class Pagination
    {
        [JsonPropertyName("ending_before")]
        public string? EndingBefore { get; set; }

        [JsonPropertyName("starting_after")]
        public string? StartingAfter { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("order")]
        public string Order { get; set; } = string.Empty;

        [JsonPropertyName("previous_uri")]
        public string? PreviousUri { get; set; }

        [JsonPropertyName("next_uri")]
        public string? NextUri { get; set; }
    }
}