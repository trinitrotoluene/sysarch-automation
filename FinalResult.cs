using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace sysarch_automation
{
    public class FinalResult
    {
        [JsonPropertyName("host")]
        public string Host { get; set; }

        [JsonPropertyName("ip")]
        public string Address { get; set; }

        [JsonPropertyName("meta")]
        public ResultMetadata Metadata { get; set; }

        [JsonPropertyName("hops")]
        public List<FinalHop> Hops { get; set; } = new List<FinalHop>();

        public static FinalResult CreateFrom(ParsingResult result, ResultMetadata metadata)
        {
            return new FinalResult()
            {
                Host = result.Host,
                Address = result.Address,
                Metadata = metadata
            };
        }
    }
}
