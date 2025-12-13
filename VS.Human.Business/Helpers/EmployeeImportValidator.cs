using System.Text.RegularExpressions;
using VS.Human.Item;
using VS.Human.Business.Model;

namespace VS.Human.Business.Helpers
{
    public static class EmployeeImportValidator
    {
        public static string? ValidateRow(EmployeeInfoAdd employee)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(employee.FullName))
                errors.Add("Ho ten khong duoc de trong");

            if (string.IsNullOrWhiteSpace(employee.Phone))
                errors.Add("So dien thoai bat buoc");

            if (!string.IsNullOrWhiteSpace(employee.Phone) && !IsPhoneNumber(employee.Phone))
                errors.Add("So dien thoai khong hop le");

            if (!string.IsNullOrWhiteSpace(employee.Email) && !IsEmail(employee.Email))
                errors.Add("Email khong hop le");

            if (errors.Any())
                return string.Join(", ", errors);

            return null;
        }

        private static bool IsPhoneNumber(string number)
        {
            return Regex.IsMatch(number, @"^(\+[0-9]{9,15})|([0-9]{9,15})$");
        }

        private static bool IsEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
