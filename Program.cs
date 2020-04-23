using ConsoleAppFramework;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace sysarch_automation
{
    class Program : ConsoleAppBase
    {
        static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<Program>(args);
        }

        private const string hostPattern = "((([a-z|A-Z|0-9|\\-]+\\.?)+)\\s\\(((\\d{1,}\\.?)+)\\))";
        private readonly Regex hostMatcher = new Regex(hostPattern, RegexOptions.Compiled);
        private const string pingPattern = "(\\d+\\.\\d+)\\sms";
        private readonly Regex pingMatcher = new Regex(pingPattern, RegexOptions.Compiled);

        [Command("parse")]
        public void ParseInput([Option("f", "path of the file to parse")] string path, [Option("o", "output file name")] string fileName)
        {
            var content = File.ReadAllText(path);
            var traceAreas = content.Split("\r\n\r\n");
            var traces = traceAreas.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Split("\r\n"));

            var results = new List<ParsingResult>();
            foreach (var trace in traces)
            {
                var result = new ParsingResult();
                for (int i = 1; i < trace.Length; i++)
                {
                    if (!hostMatcher.IsMatch(trace[i]))
                    {
                        result.Hops.Add(new Hop()
                        {
                            Host = "*",
                            Address = "*"
                        });
                        continue;
                    }

                    var match = hostMatcher.Match(trace[i]);
                    if (i == 1)
                    {
                        result.Host = match.Groups[2].Value;
                        result.Address = match.Groups[4].Value;
                    }
                    else
                    {
                        var hop = new Hop()
                        {
                            Host = match.Groups[2].Value,
                            Address = match.Groups[4].Value
                        };
                        result.Hops.Add(hop);

                        var pingMatches = pingMatcher.Matches(trace[i]);
                        for (int j = 0; j < pingMatches.Count; j++)
                        {
                            if (j == 0) hop.Ping1 = float.Parse(pingMatches[j].Groups[1].Value);
                            else if (j == 1) hop.Ping2 = float.Parse(pingMatches[j].Groups[1].Value);
                            else if (j == 2) hop.Ping3 = float.Parse(pingMatches[j].Groups[1].Value);
                        }
                    }
                }

                results.Add(result);
            }

            File.WriteAllText(fileName + ".json", JsonSerializer.Serialize(results));
        }

        [Command("whois")]
        public async Task WhoisAsync([Option("f", "path of the file to parse")] string path, [Option("o", "output file name")] string fileName)
        {
            var results = new Dictionary<string, HostInformation>();
            if (File.Exists(fileName + ".json")) results = JsonSerializer.Deserialize<Dictionary<string, HostInformation>>(File.ReadAllText(fileName + ".json"));
            foreach (var kvp in results) if (kvp.Value == null) results.Remove(kvp.Key);

            var fs = File.OpenRead(path);
            var traces = await JsonSerializer.DeserializeAsync<List<ParsingResult>>(fs);

            int remaining = traces.Count + traces.SelectMany(x => x.Hops).Count();
            int total = remaining;
            foreach (var trace in traces)
            {
                Console.WriteLine($"({remaining}/{total})");
                try
                {
                    Console.WriteLine("WHOIS " + trace.Host);
                    var resp = await SendWhoisRequestAsync(trace.Host);
                    using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
                    var dest = HostInformation.Read(doc);
                    results[trace.Host] = dest;
                    Console.WriteLine();
                }
                catch(Exception) { Console.WriteLine("Failed to WHOIS destination domain"); }
                remaining -= 1;
                foreach (var hop in trace.Hops)
                {
                    Console.WriteLine($"({remaining}/{total})");
                    if (hop.Host == "*" || results.ContainsKey(hop.Host))
                    {
                        Console.WriteLine("Host is *, or already saved - skipping " + hop.Host);
                        Console.WriteLine();
                        remaining -= 1;
                        continue;
                    }
                    try
                    {
                        Console.WriteLine("WHOIS " + hop.Host);
                        var resp = await SendWhoisRequestAsync(hop.Host);
                        using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
                        var hopInfo = HostInformation.Read(doc);
                        results[hop.Host] = hopInfo;
                        Console.WriteLine();
                    }
                    catch (Exception ex) { Console.WriteLine("Failed to WHOIS hop domain - " + ex.Message); results[hop.Host] = null; }
                    remaining -= 1;
                }
            }

            File.WriteAllText(fileName + ".json", JsonSerializer.Serialize(results));
        }

        private static HttpClient _http = new HttpClient();

        private async Task<HttpResponseMessage> SendWhoisRequestAsync(string host)
        {
            var msg = new HttpRequestMessage(HttpMethod.Get, $"https://www.whoisxmlapi.com/whoisserver/WhoisService?apiKey=REDACTED&domainName={host}&outputFormat=json");
            var response = await _http.SendAsync(msg);

            response.EnsureSuccessStatusCode();
            return response;
        }

        [Command("analysewhois")]
        public void AnalyseWhois([Option("f", "path of the file to parse")] string path)
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, HostInformation>>(File.ReadAllText(path));
            var groups = data.Select(x =>
            {
                if (x.Value == null) return x;
                x.Value.OrgName = x.Value.OrgName.Split("\n")[0];
                return x;
            }).GroupBy(x => x.Value?.OrgName ?? "unknown");
            var sb = new StringBuilder()
                .Append("WHOIS Result Analysis")
                .AppendLine();

            foreach (var group in groups)
            {
                sb.Append("== ").Append(group.Key).AppendLine(" ==");
                const string indent = "    ";
                foreach (var kvp in group)
                {
                    sb.Append(indent).Append("- ").Append(kvp.Key).AppendLine();
                }
                sb.AppendLine();
            }

            Console.WriteLine(sb.ToString());
        }

        private async Task<HttpResponseMessage> SendIPLocationRequestAsync(string address)
        {
            var msg = new HttpRequestMessage(HttpMethod.Get, $"https://ip-geolocation.whoisxmlapi.com/api/v1?apiKey=REDACTED&ipAddress={address}");
            var response = await _http.SendAsync(msg);

            response.EnsureSuccessStatusCode();
            return response;
        }

        [Command("fetch-ip-location")]
        public async Task FetchIPLocationAsync([Option("f", "path of the file to parse")] string path, [Option("o", "output file name")] string fileName)
        {
            var results = new Dictionary<string, AddressInformation>();
            if (File.Exists(fileName + ".json")) results = JsonSerializer.Deserialize<Dictionary<string, AddressInformation>>(File.ReadAllText(fileName + ".json"));
            foreach (var kvp in results) if (kvp.Value == null) results.Remove(kvp.Key);

            var fs = File.OpenRead(path);
            var traces = await JsonSerializer.DeserializeAsync<List<ParsingResult>>(fs);

            int remaining = traces.Count + traces.SelectMany(x => x.Hops).Count();
            int total = remaining;

            foreach (var trace in traces)
            {
                Console.WriteLine($"({remaining}/{total})");
                try
                {
                    Console.WriteLine("LOCATE " + trace.Address);
                    var resp = await SendIPLocationRequestAsync(trace.Address);
                    using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
                    var dest = AddressInformation.Read(doc);
                    results[trace.Address] = dest;
                    Console.WriteLine();
                }
                catch (Exception) { Console.WriteLine("Failed to locate destination IP address - " + trace.Address); }
                remaining -= 1;
                foreach (var hop in trace.Hops)
                {
                    Console.WriteLine($"({remaining}/{total})");
                    if (hop.Address == "*" || results.ContainsKey(hop.Address))
                    {
                        Console.WriteLine("Address is *, or already saved - skipping " + hop.Address);
                        Console.WriteLine();
                        remaining -= 1;
                        continue;
                    }
                    try
                    {
                        Console.WriteLine("LOCATE " + hop.Address);
                        var resp = await SendIPLocationRequestAsync(hop.Address);
                        using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
                        var hopInfo = AddressInformation.Read(doc);
                        results[hop.Address] = hopInfo;
                        Console.WriteLine();
                    }
                    catch (Exception ex) { Console.WriteLine("Failed to locate hop address - " + ex.Message); results[hop.Host] = null; }
                    remaining -= 1;
                }
            }

            File.WriteAllText(fileName + ".json", JsonSerializer.Serialize(results));
        }

        [Command("hydrate-parse-results")]
        public void HydrateParseResults([Option("p", "file to load parse results from")] string parseLocation, 
                                        [Option("h", "file to load host information from")] string hostLocation,
                                        [Option("l", "file to load location information from")] string ipLocation,
                                        [Option("o", "file to write final results to")] string outputLocation
        )
        {
            var parseResults = JsonSerializer.Deserialize<List<ParsingResult>>(File.ReadAllText(parseLocation));
            var hostResults = JsonSerializer.Deserialize<Dictionary<string, HostInformation>>(File.ReadAllText(hostLocation));
            var locationResults = JsonSerializer.Deserialize<Dictionary<string, AddressInformation>>(File.ReadAllText(ipLocation));

            var results = new List<FinalResult>();
            foreach (var trace in parseResults)
            {
                hostResults.TryGetValue(trace.Host, out var hostInfo);
                locationResults.TryGetValue(trace.Address, out var addrInfo);
                var result = FinalResult.CreateFrom(trace, ResultMetadata.ComposeFrom(hostInfo, addrInfo));

                foreach (var hop in trace.Hops)
                {
                    hostResults.TryGetValue(hop.Host, out hostInfo);
                    locationResults.TryGetValue(hop.Address, out addrInfo);
                    var hopResult = ResultMetadata.ComposeFrom(hostInfo, addrInfo);
                    result.Hops.Add(FinalHop.CreateFrom(hop, hopResult));
                }

                results.Add(result);
            }

             File.WriteAllText(outputLocation + ".json", JsonSerializer.Serialize(results));
        }

        [Command("viewtrace")]
        public void ViewTrace([Option("f", "final result file location")] string resultLocation, [Option("h", "the host to get the trace for")] string host)
        {
            var results = JsonSerializer.Deserialize<List<FinalResult>>(File.ReadAllText(resultLocation));
            var result = results.FirstOrDefault(x => x.Host?.Equals(host, StringComparison.OrdinalIgnoreCase) ?? false);
            if (result == null)
            {
                Console.WriteLine("Host not found");
                return;
            }

            var sb = new StringBuilder()
                .Append(result.Host)
                .Append(" (" + result.Address + ")")
                .AppendLine()
                .AppendLine();

            sb.Append("Organisation: ").AppendLine(result.Metadata.OrgName);
            sb.Append("Location:     ").AppendLine($"{result.Metadata.City}, {result.Metadata.Region}, {result.Metadata.Country}");
            sb.Append("AS:           ").AppendLine($"{result.Metadata.ASName} for {result.Metadata.ISP}");
            sb.AppendLine();

            int i = 1;
            foreach (var hop in result.Hops)
            {
                if (hop.Address == "*")
                {
                    sb.Append("  ").AppendLine($"{i++} * * *");
                    sb.AppendLine();
                    continue;
                }
                sb.Append("  ");
                sb.Append(i++);
                sb.AppendLine($" {hop.Host} ({hop.Address}) {hop.Ping:0.00}ms");
                sb.Append("  ").Append("Organisation: ").AppendLine(hop.Metadata.OrgName);
                sb.Append("  ").Append("Location:     ").AppendLine($"{hop.Metadata.City}, {hop.Metadata.Region}, {hop.Metadata.Country}");
                sb.Append("  ").Append("AS:           ").AppendLine($"{hop.Metadata.ASName} for {hop.Metadata.ISP}");
                sb.AppendLine();
            }

            Console.WriteLine(sb.ToString());
        }

        [Command("viewsummary")]
        public void ViewSummary([Option("f", "final result file location")] string resultLocation)
        {
            var results = JsonSerializer.Deserialize<List<FinalResult>>(File.ReadAllText(resultLocation));
            var sb = new StringBuilder();
            foreach (var result in results)
            {
                sb.Append(result.Host)
                    .Append(" (" + result.Address + ")")
                    .AppendLine()
                    .AppendLine();

                sb.Append("Organisation: ").AppendLine(result.Metadata.OrgName);
                sb.Append("Location:     ").AppendLine($"{result.Metadata.City}, {result.Metadata.Region}, {result.Metadata.Country}");
                sb.Append("AS:           ").AppendLine($"{result.Metadata.ASName} for {result.Metadata.ISP}");
                sb.Append("Hops:         ").AppendLine($"{result.Hops.Count}");
                var lastHop = result.Hops.Last();
                sb.Append("Ping:         ").AppendLine($"{lastHop.Ping}");
                sb.AppendLine();
            }

            Console.WriteLine(sb.ToString());
        }

        [Command("viewstats")]
        public void ViewStats([Option("f", "final result file location")] string resultLocation)
        {
            var results = JsonSerializer.Deserialize<List<FinalResult>>(File.ReadAllText(resultLocation));
            var sb = new StringBuilder();

            var mappings = results.SelectMany(x => x.Hops).GroupBy(x => x.Metadata.Country).Select(x => new { Country = x.Key, Ping = x.Select(x => x.Ping).Average()}).ToArray();
            var destinations = results.GroupBy(x => x.Metadata.Country).Select(x => new { Country = x.Key, Hops = x.Select(x => x.Hops.Count).Average() }).ToArray();

            for (int i = 0; i < mappings.Length; i++)
            {
                sb.Append("Country: ").AppendLine(mappings[i].Country);
                sb.Append("Latency: ").AppendLine(mappings[i].Ping.ToString("0.00") + "ms");
                var dest = destinations.FirstOrDefault(x => x.Country == mappings[i].Country);
                if (dest != null)
                {
                    sb.Append("Hops:    ").AppendLine(dest.Hops.ToString("0.00"));
                }
                sb.AppendLine();
            }

            Console.WriteLine(sb.ToString());
        }

        [Command("viewalltraces")]
        public void ViewAllTraces([Option("f", "final result file location")] string resultLocation, [Option("o", "output file location")] string outputLocation)
        {
            var results = JsonSerializer.Deserialize<List<FinalResult>>(File.ReadAllText(resultLocation));
            var sb = new StringBuilder();
            foreach (var result in results)
            {
                sb
                    .Append(result.Host)
                    .Append(" (" + result.Address + ")")
                    .AppendLine()
                    .AppendLine();

                sb.Append("Organisation: ").AppendLine(result.Metadata.OrgName);
                sb.Append("Location:     ").AppendLine($"{result.Metadata.City}, {result.Metadata.Region}, {result.Metadata.Country}");
                sb.Append("AS:           ").AppendLine($"{result.Metadata.ASName} for {result.Metadata.ISP}");
                sb.AppendLine();

                int i = 1;
                foreach (var hop in result.Hops)
                {
                    if (hop.Address == "*")
                    {
                        sb.Append("  ").AppendLine($"{i++} * * *");
                        sb.AppendLine();
                        continue;
                    }
                    sb.Append("  ");
                    sb.Append(i++);
                    sb.AppendLine($" {hop.Host} ({hop.Address}) {hop.Ping:0.00}ms");
                    sb.Append("  ").Append("Organisation: ").AppendLine(hop.Metadata.OrgName);
                    sb.Append("  ").Append("Location:     ").AppendLine($"{hop.Metadata.City}, {hop.Metadata.Region}, {hop.Metadata.Country}");
                    sb.Append("  ").Append("AS:           ").AppendLine($"{hop.Metadata.ASName} for {hop.Metadata.ISP}");
                    sb.AppendLine();
                }

                sb.AppendLine();
            }

            File.WriteAllText(outputLocation, sb.ToString());
        }
    }
}
