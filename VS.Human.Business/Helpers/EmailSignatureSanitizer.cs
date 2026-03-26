using System.Net;
using System.Text.RegularExpressions;

namespace VS.Human.Business.Helpers
{
    public static class EmailSignatureSanitizer
    {
        private static readonly Regex DangerousBlockTagsRegex = new(
            @"<\s*(script|iframe|object|embed|form|style|link|meta)[^>]*>.*?<\s*/\s*\1\s*>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex DangerousSingleTagsRegex = new(
            @"<\s*(script|iframe|object|embed|form|style|link|meta)[^>]*?/?>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex EventHandlerAttributeRegex = new(
            @"\son[a-z0-9_-]+\s*=\s*(?:""[^""]*""|'[^']*'|[^\s>]+)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex ScriptProtocolRegex = new(
            @"\s(href|src)\s*=\s*(?:""\s*(javascript|vbscript):[^""]*""|'\s*(javascript|vbscript):[^']*'|\s*(javascript|vbscript):[^\s>]+)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex HtmlTagRegex = new(
            @"<[^>]+>",
            RegexOptions.Singleline | RegexOptions.Compiled);

        public static string? Sanitize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Trim();
            if (!HtmlTagRegex.IsMatch(normalized))
            {
                return WebUtility.HtmlEncode(normalized)
                    .Replace("\r\n", "<br/>", StringComparison.Ordinal)
                    .Replace("\n", "<br/>", StringComparison.Ordinal);
            }

            normalized = DangerousBlockTagsRegex.Replace(normalized, string.Empty);
            normalized = DangerousSingleTagsRegex.Replace(normalized, string.Empty);
            normalized = EventHandlerAttributeRegex.Replace(normalized, string.Empty);
            normalized = ScriptProtocolRegex.Replace(normalized, string.Empty);

            return string.IsNullOrWhiteSpace(normalized) ? null : normalized.Trim();
        }
    }
}
