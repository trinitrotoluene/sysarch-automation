using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace sysarch_automation
{
    public class AddressInformation
    {
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

        public static AddressInformation Read(JsonDocument doc)
        {
            var root = doc.RootElement;
            var locationElement = root.GetProperty("location");
            var country = locationElement.GetProperty("country").GetString();
            var region = locationElement.GetProperty("region").GetString();
            var city = locationElement.GetProperty("city").GetString();
            var asElement = root.GetProperty("as");
            var asName = asElement.GetProperty("name").GetString();
            var asBlock = asElement.GetProperty("route").GetString();
            var asType = asElement.GetProperty("type").GetString();
            var isp = root.GetProperty("isp").GetString();

            Console.WriteLine($"{country} | {region} | {city} | {asName} | {asBlock} | {isp}");

            return new AddressInformation()
            {
                Country = country,
                Region = region,
                City = city,
                ASName = asName,
                ASBlock = asBlock,
                ASType = asType,
                ISP = isp
            };
        }
    }
}
