using System.Text.Json.Serialization;

namespace sysarch_automation
{
    public class ResultMetadata
    {
        [JsonPropertyName("orgName")]
        public string OrgName { get; set; }

        [JsonPropertyName("hostCountryCode")]
        public string CountryCode { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }

        [JsonPropertyName("region")]
        public string Region { get; set; }

        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("as_name")]
        public string ASName { get; set; }

        [JsonPropertyName("as_block")]
        public string ASBlock { get; set; }

        [JsonPropertyName("as_type")]
        public string ASType { get; set; }

        [JsonPropertyName("isp")]
        public string ISP { get; set; }

        public static ResultMetadata ComposeFrom(HostInformation host, AddressInformation address)
        {
            return new ResultMetadata()
            {
                OrgName = host?.OrgName,
                CountryCode = host?.CountryCode,
                Country = address?.Country,
                Region = address?.Region,
                City = address?.City,
                ASName = address?.ASName,
                ASBlock = address?.ASBlock,
                ASType = address?.ASType,
                ISP = address?.ISP
            };
        }
    }
}
