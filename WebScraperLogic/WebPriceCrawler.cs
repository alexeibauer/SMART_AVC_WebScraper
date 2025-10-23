using SMARTUtils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WebScraperLogic;

namespace WebScraperLogic;

public class WebPriceCrawler
{
    // Reuse a single HttpClient
    private static readonly HttpClient _http = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(20)
    };

    // Simple URL discovery: href/src="...". We’ll normalize to absolute URIs.
    private static readonly Regex _urlRegex = new Regex(
        @"(?i)\b(?:href|src)\s*=\s*[""'](?<u>[^""'#>]+)[""']",
        RegexOptions.Compiled);

    // Price regex: $, €, £, MXN, USD, etc. Examples: $1,234.56  |  99.99 USD  |  €12,34
    private static readonly Regex _priceRegex = new Regex(
        @"(?ix)
        (?<cur>\$|USD|US\$|€|EUR|£|GBP|MXN|R\$|CAD|AUD)?     
        \s*
        (?<val>
            (?:
                \d{1,3} (?:[.,]\d{3})* (?:[.,]\d{2})?        
            |
                \d+ (?:[.,]\d{2})?                          
            )
        )
        \s*
        (?<cur2>USD|US\$|EUR|GBP|MXN|CAD|AUD)?               # optional currency after
        ",
        RegexOptions.Compiled);

    /// <summary>
    /// Crawl starting from the given URL, spawning tasks that find prices per page.
    /// Returns a single merged dictionary: key = "url::thread-{id}::#occurrence", value = PriceRecord.
    /// </summary>
    public static async Task<Dictionary<string, PriceRecord>> CrawlAsync(
        string? startUrl,
        int maxDepth = 1,
        bool sameHostOnly = true,
        CancellationToken ct = default)
    {
        if(string.IsNullOrEmpty(startUrl)) throw new ArgumentNullException(nameof(startUrl));
        if (!CrawlerUtils.TryMakeAbsolute(startUrl, null, out var start)) throw new ArgumentException("Invalid start URL.", nameof(startUrl));

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allPageDictionaries = new ConcurrentBag<Dictionary<string, PriceRecord>>();
        var pending = new ConcurrentBag<Task>();
        var startHost = start.Host;

        async Task CrawlOneAsync(Uri url, int depth)
        {
            if (ct.IsCancellationRequested) return;
            var key = url.AbsoluteUri;

            lock (visited)
            {
                if (!visited.Add(key)) return; // already seen
            }

            string html;
            try
            {
                html = await _http.GetStringAsync(url, ct);
            }
            catch
            {
                return; 
            }

            // Per-page dictionary
            var pageDict = new ConcurrentDictionary<string, PriceRecord>();

            // 1) Immediately spawn a task to find prices in THIS page
            var priceTask = Task.Run(() =>
            {
                FindPricesIntoDictionary(url.AbsoluteUri, html, pageDict);
            }, ct);
            pending.Add(priceTask);

            // 2) Then, parse this HTML to discover more URLs and recurse
            if (depth < maxDepth)
            {
                foreach (var found in CrawlerUtils.ExtractUrls(url, html, _urlRegex))
                {
                    if (sameHostOnly && !CrawlerUtils.UriHostsEqual(found, startHost)) continue;
                    pending.Add(CrawlOneAsync(found, depth + 1));
                }
            }

            // 3) When price task finishes, add this page’s dict to the bag for merging later
            await priceTask;
            allPageDictionaries.Add(new Dictionary<string, PriceRecord>(pageDict));
        }

        // Kick off
        await CrawlOneAsync(start, 0);

        // Drain & wait everything spawned
        while (pending.TryTake(out var t))
        {
            try { await t; } catch { /* swallow */ }
        }

        // Join all per-page dictionaries into one
        var merged = new Dictionary<string, PriceRecord>(StringComparer.Ordinal);
        foreach (var dict in allPageDictionaries)
        {
            foreach (var kv in dict)
            {
                // Keys should already be unique (url::threadId::#n), but be defensive
                if (!merged.ContainsKey(kv.Key))
                    merged[kv.Key] = kv.Value;
            }
        }

        return merged;
    }

    private static void FindPricesIntoDictionary(string pageUrl, string html, ConcurrentDictionary<string, PriceRecord> dict)
    {
        var threadId = Thread.CurrentThread.ManagedThreadId;
        int occurrence = 0;

        foreach (Match m in _priceRegex.Matches(html))
        {
            var raw = m.Groups["val"].Value;
            var cur = m.Groups["cur"].Success ? m.Groups["cur"].Value
                    : m.Groups["cur2"].Success ? m.Groups["cur2"].Value
                    : CrawlerUtils.InferCurrencyFromSymbol(m.Value);

            if (!CrawlerUtils.TryParseAmount(raw, out var amount)) continue;

            // Heuristic: try to find a nearby element id/name as "price name"
            var priceName = CrawlerUtils.FindNearbyIdOrName(html, m.Index) ?? "price";

            var key = $"{pageUrl}::thread-{threadId}::#{++occurrence}";
            if (!string.IsNullOrEmpty(cur))
            { //Ignore false positives with no currency
                dict[key] = new PriceRecord(amount, cur, priceName);
                Console.WriteLine("Price found:: " + key + " => " + dict[key].ToString());
            }
        }
    }
}