using System.Text.Json.Serialization;

namespace sysarch_automation
{
    public class FinalHop
    {
        [JsonPropertyName("host")]
        public string Host { get; set; }

        [JsonPropertyName("ip")]
        public string Address { get; set; }

        [JsonPropertyName("ping")]
        public float Ping { get; set; }

        [JsonPropertyName("meta")]
        public ResultMetadata Metadata { get; set; }

        public static FinalHop CreateFrom(Hop hop, ResultMetadata result)
        {
            var avg = (hop.Ping1 + hop.Ping2 + hop.Ping3) / 3;
            return new FinalHop()
            {
                Host = hop.Host,
                Address = hop.Address,
                Metadata = result,
                Ping = avg
            };
        }

        public override string ToString()
        {
            return $"{Host} ({Address})";
        }
    }
}
