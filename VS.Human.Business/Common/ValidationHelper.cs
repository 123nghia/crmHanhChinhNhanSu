namespace VS.Human.Business.Common
{
    /// <summary>
    /// Helper class for common validation operations
    /// </summary>
    public static class ValidationHelper
    {
        public static Result ValidateRequired(string? value, string fieldName, string displayName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Result.Failure($"Thiếu thông tin {displayName}");
            }
            return Result.Success();
        }

        public static Result ValidateEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Result.Failure("Email is required");

            if (!email.Contains("@"))
                return Result.Failure("Email không hợp lệ");

            return Result.Success();
        }

        public static Result ValidatePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return Result.Failure("Số điện thoại là bắt buộc");

            if (phone.Length < 10)
                return Result.Failure("Số điện thoại phải có ít nhất 10 số");

            return Result.Success();
        }

        public static Result ValidateId(int id, string entityName = "đối tượng")
        {
            if (id <= 0)
                return Result.Failure($"ID {entityName} không hợp lệ");

            return Result.Success();
        }

        public static List<object> CreateErrorList(string fieldName, string message)
        {
            var errors = new List<object>
            {
                new { name = fieldName, Content = message }
            };
            return errors;
        }
    }
}

