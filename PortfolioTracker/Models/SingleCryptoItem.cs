using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PortfolioTracker.Models
{
    public class SingleCryptoItem
    {
        public Data? Data { get; set; }
    }

    public class Data
    {
        [JsonPropertyName("amount")]
        public string? Amount { get; set; }

        [JsonPropertyName("base")]
        public string? Base { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }
    }
}
