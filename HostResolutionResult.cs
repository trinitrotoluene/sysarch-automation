using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace sysarch_automation
{
    public class HostResolutionResult
    {
        [JsonPropertyName("destination")]
        public HostInformation Destination { get; set; }

        [JsonPropertyName("hops")]
        public List<HostInformation> Hops { get; set; } = new List<HostInformation>();
    }
}
