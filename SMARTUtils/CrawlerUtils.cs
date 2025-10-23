using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SMARTUtils
{
    public class CrawlerUtils
    {

        public static bool TryParseAmount(string s, out decimal amount)
        {
            // Try with both common decimal separators; strip group separators.
            s = s.Trim();

            var lastComma = s.LastIndexOf(',');
            var lastDot = s.LastIndexOf('.');
            var decimalSep = (lastComma > lastDot) ? ',' : '.';

            var normalized = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (char.IsDigit(ch) || ch == decimalSep) normalized.Append(ch);
            }

            var style = NumberStyles.AllowDecimalPoint;
            var culture = (decimalSep == ',') ? new CultureInfo("fr-FR") : CultureInfo.InvariantCulture;

            return decimal.TryParse(normalized.ToString(), style, culture, out amount);
        }

        public static string? FindNearbyIdOrName(string html, int index)
        {
            const int window = 240; // scan a small window around the price for id/name
            int start = Math.Max(0, index - window);
            int end = Math.Min(html.Length, index + window);
            var slice = html.Substring(start, end - start);

            // Try to capture id="..." or name="..." closest to the price
            var mId = Regex.Match(slice, @"(?i)\bid\s*=\s*[""'](?<v>[^""']+)[""']");
            if (mId.Success) return mId.Groups["v"].Value.Trim();

            var mName = Regex.Match(slice, @"(?i)\bname\s*=\s*[""'](?<v>[^""']+)[""']");
            if (mName.Success) return mName.Groups["v"].Value.Trim();

            // Try an aria-label or class as fallback
            var mAria = Regex.Match(slice, @"(?i)\baria-label\s*=\s*[""'](?<v>[^""']+)[""']");
            if (mAria.Success) return mAria.Groups["v"].Value.Trim();

            var mClass = Regex.Match(slice, @"(?i)\bclass\s*=\s*[""'](?<v>[^""']+)[""']");
            if (mClass.Success) return mClass.Groups["v"].Value.Trim();

            return null;
        }

        public static IEnumerable<Uri> ExtractUrls(Uri baseUrl, string html, Regex urlRegex)
        {
            foreach (Match m in urlRegex.Matches(html))
            {
                var raw = m.Groups["u"].Value.Trim();

                // Skip fragments, javascript:, mailto:, etc.
                if (raw.StartsWith("#") || raw.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) ||
                    raw.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (TryMakeAbsolute(raw, baseUrl, out var abs)
                    && (abs.Scheme == Uri.UriSchemeHttp || abs.Scheme == Uri.UriSchemeHttps))
                {
                    yield return abs;
                }
            }
        }

        public static bool TryMakeAbsolute(string input, Uri? baseUrl, out Uri absolute)
        {
            absolute = null!;
            if (!input.StartsWith("http://") && !input.StartsWith("https://"))
            {
                input = string.Format("https://{0}",input.Trim());
            }
            if (Uri.TryCreate(input, UriKind.Absolute, out var abs)) { absolute = abs; return true; }
            if (baseUrl != null && Uri.TryCreate(baseUrl, input, out abs)) { absolute = abs; return true; }
            return false;
        }

        public static bool UriHostsEqual(Uri u, string host) => u.Host.Equals(host, StringComparison.OrdinalIgnoreCase);

        public static string InferCurrencyFromSymbol(string token)
        {
            if (token.Contains("$")) return "$";
            if (token.Contains("€")) return "€";
            if (token.Contains("£")) return "£";
            return string.Empty;
        }

    }
}
