using System.Text.Json.Serialization;

namespace PortfolioTracker.Models
{
    public class CoinbaseV2TransactionResponse
    {
        [JsonPropertyName("data")]
        public List<CoinbaseTransaction> Data { get; set; } = new();

        [JsonPropertyName("pagination")]
        public Pagination? Pagination { get; set; }
    }

    public class CoinbaseTransaction
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty; // "buy", "sell", "send", "receive", "trade", "fiat_deposit", "fiat_withdrawal"

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // "completed", "pending", etc.

        [JsonPropertyName("amount")]
        public V2Balance Amount { get; set; } = new();

        [JsonPropertyName("native_amount")]
        public V2Balance NativeAmount { get; set; } = new();

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}