using System.Text.Json.Serialization;

namespace sysarch_automation
{
    public class Hop
    {
        [JsonPropertyName("host")]
        public string Host { get; set; }

        [JsonPropertyName("ip")]
        public string Address { get; set; }

        [JsonPropertyName("ping1")]
        public float Ping1 { get; set; }

        [JsonPropertyName("ping2")]
        public float Ping2 { get; set; }

        [JsonPropertyName("ping3")]
        public float Ping3 { get; set; }

        public override string ToString()
        {
            return $"{Host} ({Address})";
        }
    }
}
