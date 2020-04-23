using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace sysarch_automation
{
    public class HostInformation
    {
        [JsonPropertyName("orgName")]
        public string OrgName { get; set; }

        [JsonPropertyName("countryCode")]
        public string CountryCode { get; set; }

        public static HostInformation Read(JsonDocument document)
        {
            var data = document.RootElement.GetProperty("WhoisRecord");

            string orgName = null;
            string countryCode = null;

            if (data.TryGetProperty("registrant", out var regData))
            {
                if (regData.TryGetProperty("organization", out var orgValue)) orgName = orgValue.GetString();
                else if (regData.TryGetProperty("name", out var nameValue)) orgName = nameValue.GetString();
                if (regData.TryGetProperty("countryCode", out var ccValue)) countryCode = ccValue.GetString();
            }
            if (data.TryGetProperty("registryData", out regData))
            {
                if (regData.TryGetProperty("registrant", out regData))
                {
                    if (orgName == null)
                    {
                        if (regData.TryGetProperty("organization", out var orgValue)) orgName = orgValue.GetString();
                        else orgName = regData.GetProperty("name").GetString();
                    }
                    if (countryCode == null)
                    {
                        countryCode = regData.GetProperty("countryCode").GetString();
                    }
                }
            }

            if (orgName != null && countryCode != null)
            {
                Console.WriteLine(orgName + " (" + countryCode + ")");
                return new HostInformation()
                {
                    OrgName = orgName,
                    CountryCode = countryCode
                };
            }

            throw new Exception("Missing data!");
        }

        public override string ToString()
        {
            return $"{OrgName} ({CountryCode})";
        }
    }
}
