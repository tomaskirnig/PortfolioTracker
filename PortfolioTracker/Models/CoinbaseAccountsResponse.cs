using System.Text.Json.Serialization;

namespace PortfolioTracker.Models
{
    public class CoinbaseAccountsResponse
    {
        [JsonPropertyName("accounts")]
        public List<CoinbaseAccount> Accounts { get; set; } = new();

        [JsonPropertyName("has_next")]
        public bool HasNext { get; set; }

        [JsonPropertyName("cursor")]
        public string? Cursor { get; set; }

        [JsonPropertyName("size")]
        public int Size { get; set; }
    }

    public class CoinbaseAccount
    {
        [JsonPropertyName("uuid")]
        public string Uuid { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;

        [JsonPropertyName("available_balance")]
        public Balance AvailableBalance { get; set; } = new();

        [JsonPropertyName("default")]
        public bool Default { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("ready")]
        public bool Ready { get; set; }

        [JsonPropertyName("hold")]
        public Balance Hold { get; set; } = new();

        [JsonPropertyName("retail_portfolio_id")]
        public string RetailPortfolioId { get; set; } = string.Empty;

        [JsonPropertyName("platform")]
        public string Platform { get; set; } = string.Empty;

        // Computed property for easier display
        public decimal BalanceValue => decimal.TryParse(AvailableBalance.Value, out var value) ? value : 0;
        
        public bool HasBalance => BalanceValue > 0;
    }

    public class Balance
    {
        [JsonPropertyName("value")]
        public string Value { get; set; } = "0";

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = string.Empty;
    }
}