using System;
using System.Linq;
using VS.Human.Business.Model;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Helpers
{
    /// <summary>
    /// Helper class for mapping Employee objects
    /// </summary>
    public static class EmployeeMapper
    {
        /// <summary>
        /// Maps EmployeeDetailUpdate to EmployeeInfoAdd
        /// </summary>
        public static EmployeeInfoAdd MapToEmployeeInfoAdd(EmployeeDetailUpdate request)
        {
            return new EmployeeInfoAdd()
            {
                Id = request.Id,
                RoleCode = request.RoleCode,
                FullName = request.FullName,
                NationalDate = request.NationalDate,
                NationalId = request.NationalId,
                NationalPlace = request.NationalPlace,
                Dob = request.Dob,
                Onboard = request.Onboard,
                ResignationDate = request.ResignationDate,
                Phone = request.Phone,
                ManagerId = request.ManagerId,
                DepartmentCode = request.DepartmentCode,
                PositionCode = request.PositionCode,
                GroupId = request.GroupId,
                Email = request.Email,
                Noted = request.Noted,
                StatusWork = request.StatusWork,
                PermanentAddress = request.PermanentAddress,
                TemporaryAddress = request.TemporaryAddress,
                DocumentStatus = request.DocumentStatus,
                Status = request.Status,
                CVLink = request.CVLink,
                BankAccount = request.BankAccount,
                BankName = request.BankName,
                EducationLevel = request.EducationLevel,
                Maritalstatus = request.Maritalstatus,
                DocumentCheck = request.DocumentCheck,
                Gender = request.Gender,
                PlaceOfBirth = request.PlaceOfBirth,
                Ethnicity = request.Ethnicity,
                Religion = request.Religion,
                PersonalEmail = request.PersonalEmail,
                BeneficiaryName = request.BeneficiaryName,
                EmergencyContact = request.EmergencyContact,
                FingerprintCode = request.FingerprintCode
            };
        }

        public static EmployeeInfoAdd MapToEmployeeInfoAdd(Employee employee)
        {
            return new EmployeeInfoAdd()
            {
                Id = employee.Id,
                RoleCode = employee.RoleCode,
                FullName = employee.FullName,
                NationalDate = employee.NationalDate,
                NationalId = employee.NationalId,
                NationalPlace = employee.NationalPlace,
                Dob = employee.Dob,
                Onboard = employee.Onboard,
                ResignationDate = employee.ResignationDate,
                Phone = employee.Phone,
                ManagerId = employee.ManagerId,
                DepartmentCode = employee.DepartmentCode,
                PositionCode = employee.PositionCode,
                GroupId = employee.GroupId,
                Email = employee.Email,
                Noted = employee.Noted,
                StatusWork = employee.StatusWork,
                PermanentAddress = employee.PermanentAddress,
                TemporaryAddress = employee.TemporaryAddress,
                DocumentStatus = employee.DocumentStatus,
                Status = employee.Status,
                CVLink = employee.CVLink,
                BankAccount = employee.BankAccount,
                BankName = employee.BankName,
                EducationLevel = employee.EducationLevel,
                Maritalstatus = employee.Maritalstatus,
                DocumentCheck = employee.DocumentCheck,
                UserName = employee.UserName,
                Pass = employee.Pass,
                CreatedBy = employee.CreatedBy,
                UpdatedBy = employee.UpdatedBy,
                CreateAt = employee.CreateAt,
                UpdateAt = employee.UpdateAt,
                IsActive = employee.IsActive,
                Gender = employee.Gender,
                PlaceOfBirth = employee.PlaceOfBirth,
                Ethnicity = employee.Ethnicity,
                Religion = employee.Religion,
                PersonalEmail = employee.PersonalEmail,
                BeneficiaryName = employee.BeneficiaryName,
                EmergencyContact = employee.EmergencyContact,
                FingerprintCode = employee.FingerprintCode
            };
        }

       
        public static Employee MapToEmployee(EmployeeInfoAdd itemUpdate, Employee? existingEmployee = null)
        {
            var item = new Employee();
            
            // Basic information
            item.FullName = itemUpdate.FullName;
            item.Id = itemUpdate.Id;
            item.RoleCode = itemUpdate.RoleCode;
            item.Dob = itemUpdate.Dob;
            item.ManagerId = itemUpdate.ManagerId;
            item.DepartmentCode = itemUpdate.DepartmentCode;
            item.PositionCode = itemUpdate.PositionCode;
            item.GroupId = itemUpdate.GroupId;
            item.Email = itemUpdate.Email;
            item.CVLink = itemUpdate.CVLink;
            item.Phone = itemUpdate.Phone;
            item.IsActive = itemUpdate.IsActive;
            item.Noted = itemUpdate.Noted;
            item.Onboard = itemUpdate.Onboard;
            item.ResignationDate = itemUpdate.ResignationDate;
            item.PermanentAddress = itemUpdate.PermanentAddress;
            item.TemporaryAddress = itemUpdate.TemporaryAddress;
            item.NationalId = itemUpdate.NationalId;
            item.NationalDate = itemUpdate.NationalDate;
            item.NationalPlace = itemUpdate.NationalPlace;
            item.DocumentStatus = itemUpdate.DocumentStatus;
            item.Status = itemUpdate.Status;
            item.BankAccount = itemUpdate.BankAccount;
            item.BankName = itemUpdate.BankName;
            item.EducationLevel = itemUpdate.EducationLevel;
            item.Maritalstatus = itemUpdate.Maritalstatus;
            item.DocumentCheck = itemUpdate.DocumentCheck;
            item.StatusWork = itemUpdate.StatusWork;
            
            // Các trường mới
            item.Gender = itemUpdate.Gender;
            item.PlaceOfBirth = itemUpdate.PlaceOfBirth;
            item.Ethnicity = itemUpdate.Ethnicity;
            item.Religion = itemUpdate.Religion;
            item.PersonalEmail = itemUpdate.PersonalEmail;
            item.BeneficiaryName = itemUpdate.BeneficiaryName;
            item.EmergencyContact = itemUpdate.EmergencyContact;
            item.FingerprintCode = itemUpdate.FingerprintCode;

            // Handle CreatedBy, CreateAt, UserName, and Pass
            if (existingEmployee != null)
            {
                // Update existing employee - preserve CreatedBy, CreateAt, UserName, and Pass
                item.CreatedBy = existingEmployee.CreatedBy;
                item.CreateAt = existingEmployee.CreateAt;
                item.UserName = existingEmployee.UserName;
                item.Pass = existingEmployee.Pass; // Keep existing password
            }
            else
            {
                // New employee
                item.CreatedBy = itemUpdate.CreatedBy;
                item.CreateAt = itemUpdate.CreateAt;
                item.UserName = itemUpdate.UserName;
                item.Pass = itemUpdate.Pass; // Will be hashed in business layer
            }

            item.UpdatedBy = itemUpdate.UpdatedBy;
            item.UpdateAt = itemUpdate.UpdateAt;

            return item;
        }

        /// <summary>
        /// Generates username from full name (family.given), fallback to email/phone.
        /// </summary>
        public static string GenerateUserName(string? email, string? phone, string? fullName)
        {
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                var fromName = GenerateFamilyGivenUserName(fullName);
                if (!string.IsNullOrWhiteSpace(fromName))
                {
                    return fromName;
                }
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                return email.Split('@')[0];
            }

            if (!string.IsNullOrWhiteSpace(phone))
            {
                return phone;
            }

            return "user" + DateTime.Now.Ticks;
        }

        public static string GenerateFamilyGivenUserName(string? fullName)
        {
            var parts = GetNormalizedNameParts(fullName);
            if (parts.Length == 0) return string.Empty;
            if (parts.Length == 1) return parts[0];

            var familyName = parts[0];
            var givenName = parts[parts.Length - 1];
            return $"{familyName}.{givenName}";
        }

        public static string GenerateLegacyGivenFamilyUserName(string? fullName)
        {
            var parts = GetNormalizedNameParts(fullName);
            if (parts.Length == 0) return string.Empty;
            if (parts.Length == 1) return parts[0];

            var familyName = parts[0];
            var givenName = parts[parts.Length - 1];
            return $"{givenName}{familyName}";
        }

        public static bool IsLegacyAutoGeneratedUserName(string? userName, string? fullName)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(fullName))
            {
                return false;
            }

            var normalizedUserName = userName.Trim().ToLowerInvariant();
            var legacyBase = GenerateLegacyGivenFamilyUserName(fullName);
            if (string.IsNullOrWhiteSpace(legacyBase))
            {
                return false;
            }

            if (string.Equals(normalizedUserName, legacyBase, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!normalizedUserName.StartsWith(legacyBase, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var suffix = normalizedUserName.Substring(legacyBase.Length);
            return suffix.Length > 0 && suffix.All(char.IsDigit);
        }

        private static string[] GetNormalizedNameParts(string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return Array.Empty<string>();

            var unSignName = VS.Human.Utility.Utils.ConvertToUnSign(fullName).ToLowerInvariant();
            var normalizedChars = new char[unSignName.Length];

            for (int i = 0; i < unSignName.Length; i++)
            {
                var ch = unSignName[i];
                normalizedChars[i] = char.IsLetterOrDigit(ch) ? ch : ' ';
            }

            return new string(normalizedChars).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }
    }
}

