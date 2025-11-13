namespace crmHuman.Helpers
{
    /// <summary>
    /// Helper class for validation operations
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Validates if a value is required (not null or empty)
        /// </summary>
        public static void ValidateRequired(string? value, string fieldName, string displayName, List<object> errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(new { name = fieldName, Content = $"Thiếu thông tin {displayName}" });
            }
        }

        /// <summary>
        /// Validates if an ID is valid (greater than 0)
        /// </summary>
        public static void ValidateId(int id, string fieldName, string displayName, List<object> errors)
        {
            if (id < 1)
            {
                errors.Add(new { name = fieldName, Content = $"Thiếu thông tin {displayName}" });
            }
        }

        /// <summary>
        /// Validates if an ID is valid for deletion (greater than or equal to 0)
        /// </summary>
        public static void ValidateIdForDelete(int id, List<object> errors)
        {
            if (id < 0)
            {
                errors.Add(new { name = "id", Content = "Thiếu thông tin cần xoá" });
            }
        }

        /// <summary>
        /// Validates phone number
        /// </summary>
        public static void ValidatePhone(string? phone, List<object> errors)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                errors.Add(new { name = "txtPhone", Content = "Thiếu thông tin số điện thoại" });
            }
        }

        /// <summary>
        /// Validates email
        /// </summary>
        public static void ValidateEmail(string? email, List<object> errors)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                errors.Add(new { name = "txtEmail", Content = "Thiếu thông tin email" });
            }
            else if (!email.Contains("@"))
            {
                errors.Add(new { name = "txtEmail", Content = "Email không hợp lệ" });
            }
        }

        /// <summary>
        /// Validates password for new employee
        /// </summary>
        public static void ValidatePasswordForNewEmployee(string? password, int employeeId, List<object> errors)
        {
            if (employeeId < 0 && string.IsNullOrWhiteSpace(password))
            {
                errors.Add(new { name = "txtPass", Content = "Yêu cầu nhập mật khẩu" });
            }
        }

        /// <summary>
        /// Validates full name
        /// </summary>
        public static void ValidateFullName(string? fullName, string fieldName, List<object> errors)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                errors.Add(new { name = fieldName, Content = "Yêu cầu nhập họ và tên" });
            }
        }

        /// <summary>
        /// Checks if there are any errors and returns true if errors exist
        /// </summary>
        public static bool HasErrors(List<object> errors)
        {
            return errors.Count > 0;
        }
    }
}

