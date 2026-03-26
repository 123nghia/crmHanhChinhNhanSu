using System.Net;
using System.Text.RegularExpressions;

namespace VS.Human.Business.Helpers
{
    public static class EmailSignatureHtmlNormalizer
    {
        public const string DefaultCompanyLogoPath = "/assets/img/logo.png";

        private static readonly Regex ImageSourceRegex = new(
            "(<img\\b[^>]*?\\bsrc\\s*=\\s*[\"'])(?<src>[^\"']+)([\"'][^>]*>)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string? NormalizeSignatureHtml(string? html, string? fallbackImagePath = DefaultCompanyLogoPath)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return null;
            }

            var normalized = html.Trim();
            if (string.IsNullOrWhiteSpace(fallbackImagePath))
            {
                return normalized;
            }

            return ImageSourceRegex.Replace(normalized, match =>
            {
                var source = WebUtility.HtmlDecode(match.Groups["src"].Value ?? string.Empty).Trim();
                if (!LooksLikeLegacyClipboardImageSource(source) || !LooksLikeCompanyLogoImage(match.Value))
                {
                    return match.Value;
                }

                return match.Value.Replace(match.Groups["src"].Value, fallbackImagePath, StringComparison.Ordinal);
            });
        }

        private static bool LooksLikeLegacyClipboardImageSource(string? source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return false;
            }

            return source.StartsWith("cid:image", StringComparison.OrdinalIgnoreCase)
                || (source.StartsWith("file:///", StringComparison.OrdinalIgnoreCase)
                    && source.Contains("msohtmlclip", StringComparison.OrdinalIgnoreCase))
                || source.Contains("clip_image", StringComparison.OrdinalIgnoreCase);
        }

        private static bool LooksLikeCompanyLogoImage(string tagHtml)
        {
            if (string.IsNullOrWhiteSpace(tagHtml))
            {
                return false;
            }

            return tagHtml.Contains("logo", StringComparison.OrdinalIgnoreCase)
                || tagHtml.Contains("vietstar", StringComparison.OrdinalIgnoreCase);
        }
    }
}
