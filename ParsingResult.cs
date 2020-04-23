using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace sysarch_automation
{
    public class ParsingResult
    {
        [JsonPropertyName("host")]
        public string Host { get; set; }

        [JsonPropertyName("ip")]
        public string Address { get; set; }

        [JsonPropertyName("hops")]
        public List<Hop> Hops { get; set; } = new List<Hop>();

        public override string ToString()
        {
            return $"{Host} ({Address}) ({Hops.Count} hops)";
        }
    }
}
