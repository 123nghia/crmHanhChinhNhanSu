using System.Text.RegularExpressions;
using VS.Human.Rep.Model;

namespace crmHuman.Services
{
    public static class ThemeStyleBuilder
    {
        public const string DefaultPrimary = "#1f4e79";
        public const string DefaultPrimaryStrong = "#173b5c";
        public const string DefaultPrimarySoft = "#e8f1ff";
        public const string DefaultButton = "#1f4e79";
        public const string DefaultBackground = "#f6f9ff";

        public static string Build(UserThemeSetting? setting)
        {
            if (setting == null)
            {
                return string.Empty;
            }

            var hasAny = !string.IsNullOrWhiteSpace(setting.PrimaryColor)
                         || !string.IsNullOrWhiteSpace(setting.ButtonColor)
                         || !string.IsNullOrWhiteSpace(setting.BackgroundColor);

            if (!hasAny)
            {
                return string.Empty;
            }

            var primary = NormalizeOrDefault(setting.PrimaryColor, DefaultPrimary);
            var button = NormalizeOrDefault(setting.ButtonColor, primary);
            var background = NormalizeOrDefault(setting.BackgroundColor, DefaultBackground);

            var accentStrong = Darken(primary, 15);
            var accentSoft = Lighten(primary, 80);
            var buttonHover = Darken(button, 12);

            return $"--brand-accent:{primary};--brand-accent-strong:{accentStrong};--brand-accent-soft:{accentSoft};" +
                   $"--button-bg:{button};--button-bg-hover:{buttonHover};--page-bg:{background};";
        }

        public static string NormalizeOrDefault(string? value, string fallback)
        {
            return TryNormalizeHex(value, out var normalized) ? normalized : fallback;
        }

        public static bool TryNormalizeHex(string? value, out string normalized)
        {
            normalized = string.Empty;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var hex = value.Trim();
            if (hex.StartsWith("#"))
            {
                hex = hex.Substring(1);
            }

            if (hex.Length == 3)
            {
                if (!Regex.IsMatch(hex, "^[0-9a-fA-F]{3}$"))
                {
                    return false;
                }

                hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
            }
            else if (hex.Length == 6)
            {
                if (!Regex.IsMatch(hex, "^[0-9a-fA-F]{6}$"))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            normalized = "#" + hex.ToLowerInvariant();
            return true;
        }

        private static string Darken(string hex, int percent)
        {
            var (r, g, b) = ParseHex(hex);
            r = AdjustDarken(r, percent);
            g = AdjustDarken(g, percent);
            b = AdjustDarken(b, percent);
            return ToHex(r, g, b);
        }

        private static string Lighten(string hex, int percent)
        {
            var (r, g, b) = ParseHex(hex);
            r = AdjustLighten(r, percent);
            g = AdjustLighten(g, percent);
            b = AdjustLighten(b, percent);
            return ToHex(r, g, b);
        }

        private static (int r, int g, int b) ParseHex(string hex)
        {
            var clean = hex.TrimStart('#');
            var r = Convert.ToInt32(clean.Substring(0, 2), 16);
            var g = Convert.ToInt32(clean.Substring(2, 2), 16);
            var b = Convert.ToInt32(clean.Substring(4, 2), 16);
            return (r, g, b);
        }

        private static int AdjustDarken(int value, int percent)
        {
            var result = value * (100 - percent) / 100;
            return Clamp(result);
        }

        private static int AdjustLighten(int value, int percent)
        {
            var result = value + (255 - value) * percent / 100;
            return Clamp(result);
        }

        private static int Clamp(int value)
        {
            if (value < 0)
            {
                return 0;
            }

            if (value > 255)
            {
                return 255;
            }

            return value;
        }

        private static string ToHex(int r, int g, int b)
        {
            return $"#{r:X2}{g:X2}{b:X2}".ToLowerInvariant();
        }
    }
}
