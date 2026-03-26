using System.Globalization;
using System.Text;
using VS.Human.Rep.Model;

namespace crmHuman.Helpers
{
    public static class EmployeeSystemAccessPolicy
    {
        public static bool HasSystemAccess(Employee? employee)
        {
            if (employee == null || employee.Id <= 0)
            {
                return false;
            }

            var statusText = string.IsNullOrWhiteSpace(employee.StatusWorkText)
                ? employee.StatusWork
                : employee.StatusWorkText;

            return !IsResignedStatus(statusText);
        }

        private static bool IsResignedStatus(string? value)
        {
            var normalized = Normalize(value);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return false;
            }

            return normalized.Contains("nghi viec")
                || normalized.Contains("thoi viec")
                || normalized.Contains("ket thuc")
                || normalized.Contains("da nghi");
        }

        private static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var ch in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(ch);
                }
            }

            return builder
                .ToString()
                .Normalize(NormalizationForm.FormC)
                .ToLowerInvariant()
                .Replace('_', ' ')
                .Trim();
        }
    }
}
