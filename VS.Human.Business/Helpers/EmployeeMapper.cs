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
        /// Generates username from email, phone, or full name
        /// </summary>
        public static string GenerateUserName(string? email, string? phone, string? fullName)
        {
            if (!string.IsNullOrWhiteSpace(email))
            {
                return email.Split('@')[0];
            }
            else if (!string.IsNullOrWhiteSpace(phone))
            {
                return phone;
            }
            else if (!string.IsNullOrWhiteSpace(fullName))
            {
                // Format: [Tên][Viết tắt Họ Đệm]
                // Ví dụ: Nguyễn Văn Nghĩa -> nghianv
                return GetShortUserName(fullName);
            }
            else
            {
                return "user" + DateTime.Now.Ticks;
            }
        }

        private static string GetShortUserName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return "";

            // Bỏ dấu và lowercase
            string unSignName = VS.Human.Utility.Utils.ConvertToUnSign(fullName).ToLower();
            
            var parts = unSignName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "";
            if (parts.Length == 1) return parts[0];

            var firstName = parts[parts.Length - 1]; // Tên
            var lastNameInitials = "";

            // Lấy chữ cái đầu của họ và đệm
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (parts[i].Length > 0)
                {
                    lastNameInitials += parts[i][0];
                }
            }

            return firstName + lastNameInitials;
        }
    }
}

