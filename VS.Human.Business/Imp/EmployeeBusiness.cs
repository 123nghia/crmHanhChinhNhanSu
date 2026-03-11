using Microsoft.AspNetCore.Http;
using VS.Human.Business.Helpers;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class EmployeeBusiness : BaseBusiness, IEmpBusiness
    {



        public EmployeeBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {

        }

        public async Task<Employee?> Add(EmployeeInfoAdd itemAdd)
        {
            itemAdd.UserName = await BuildUniqueUserName(itemAdd.UserName, itemAdd.Email, itemAdd.Phone, itemAdd.FullName);
            var item = new Employee();
            item.FullName = itemAdd.FullName;
            item.Onboard = itemAdd.Onboard;
            item.ResignationDate = itemAdd.ResignationDate;
            item.LineCode = itemAdd.LineCode;

            item.Phone = itemAdd.Phone;
            item.RoleCode = itemAdd.RoleCode;
            item.UserName = itemAdd.UserName;
            item.CreatedBy = itemAdd.CreatedBy;
            item.ColorCode = itemAdd.ColorCode;
            item.Noted = itemAdd.Noted;
            item.Dob = itemAdd.Dob;
            item.IsActive = itemAdd.IsActive;
            item.Status = itemAdd.Status;

            item.PermanentAddress = itemAdd.PermanentAddress;
            item.TemporaryAddress = itemAdd.TemporaryAddress;
            item.NationalId = itemAdd.NationalId;
            item.NationalDate = itemAdd.NationalDate;
            item.NationalPlace = itemAdd.NationalPlace;
            item.Email = itemAdd.Email;
            item.DepartmentCode = itemAdd.DepartmentCode;
            item.PositionCode = itemAdd.PositionCode;
            item.ManagerId = itemAdd.ManagerId;
            item.StatusWork = itemAdd.StatusWork;
            item.BankAccount = itemAdd.BankAccount;
            item.BankName = itemAdd.BankName;
            item.EducationLevel = itemAdd.EducationLevel;
            item.Maritalstatus = itemAdd.Maritalstatus;
            item.DocumentCheck = itemAdd.DocumentCheck;
            item.DocumentStatus = itemAdd.DocumentStatus;
            item.Gender = itemAdd.Gender;
            item.PlaceOfBirth = itemAdd.PlaceOfBirth;
            item.Ethnicity = itemAdd.Ethnicity;
            item.Religion = itemAdd.Religion;
            item.PersonalEmail = itemAdd.PersonalEmail;
            item.BeneficiaryName = itemAdd.BeneficiaryName;
            item.EmergencyContact = itemAdd.EmergencyContact;
            item.FingerprintCode = itemAdd.FingerprintCode;
            
            var passNew = getMD5(itemAdd.Pass);
            item.Pass = passNew;
            item.CreateAt = DateTime.Now;
            item.CreatedBy = GetUserId();
            var ok = await _unitOfWork.EmployeeRep.AddOrUpdate(item);
            if (!ok) return null;

            // Try to fetch the created employee to get real Id/UserName
            var created = await _unitOfWork.EmployeeRep.GetLastByEmailOrPhone(item.Email ?? string.Empty, item.Phone ?? string.Empty);
            if (created != null && created.Id > 0) return created;
            // Fallback to duplicate check
            created = await _unitOfWork.EmployeeRep.CheckDuplicate(item.Email ?? string.Empty, item.Phone ?? string.Empty);
            if (created != null && created.Id > 0) return created;
            return null;
        }


        public async Task<bool> ChangePassword(string password, int id)
        {
            var passwordNew = getMD5(password);

            return await _unitOfWork.EmployeeRep.ChangePassword(passwordNew, id);
        }

        public async Task<bool> UpdateAvatar(int id, string avatarFile, int updatedBy)
        {
            if (id <= 0 || string.IsNullOrWhiteSpace(avatarFile))
            {
                return false;
            }

            return await _unitOfWork.EmployeeRep.UpdateAvatar(id, avatarFile.Trim(), updatedBy);
        }

        public async Task<bool> Update(EmployeeInfoAdd itemUpdate)
        {
            // Set UpdatedBy và UpdatedAt
            itemUpdate.UpdatedBy = GetUserId();
            itemUpdate.UpdateAt = DateTime.Now;

            // Get existing employee if updating
            Employee? existingEmployee = null;
            if (itemUpdate.Id > 0)
            {
                existingEmployee = await _unitOfWork.EmployeeRep.GetById(itemUpdate.Id);
            }

            // Handle new employee (Id < 0)
            if (itemUpdate.Id < 0)
            {
                itemUpdate.UserName = await BuildUniqueUserName(itemUpdate.UserName, itemUpdate.Email, itemUpdate.Phone, itemUpdate.FullName);

                // Set default password if not provided
                if (string.IsNullOrEmpty(itemUpdate.Pass))
                {
                    itemUpdate.Pass = "Vietstar@2026"; // Default password (will be hashed later)
                }

                itemUpdate.CreatedBy = GetUserId();
                itemUpdate.CreateAt = DateTime.Now;
            }

            // Map to Employee entity
            var item = EmployeeMapper.MapToEmployee(itemUpdate, existingEmployee);

            // Hash password for new employees (existing employees keep their password)
            if (itemUpdate.Id < 0 && !string.IsNullOrEmpty(item.Pass))
            {
                // Hash password for new employee
                item.Pass = getMD5(item.Pass);
            }

            return await _unitOfWork.EmployeeRep.AddOrUpdate(item);
        }

        private async Task<string> BuildUniqueUserName(string? userName, string? email, string? phone, string? fullName)
        {
            var baseUserName = string.IsNullOrWhiteSpace(userName)
                ? EmployeeMapper.GenerateUserName(email, phone, fullName)
                : userName.Trim();

            if (string.IsNullOrWhiteSpace(baseUserName))
            {
                baseUserName = "user" + DateTime.Now.Ticks;
            }

            var finalUserName = baseUserName;
            var counter = 2;

            while (true)
            {
                var existUser = await _unitOfWork.EmployeeRep.GetByUserName(finalUserName);
                if (existUser == null || existUser.Id <= 0)
                {
                    break;
                }

                finalUserName = $"{baseUserName}{counter}";
                counter++;

                if (counter > 100) break;
            }

            return finalUserName;
        }
        public Task<bool> Delete(int id, bool reactive = false)
        {
            return _unitOfWork.EmployeeRep.Delete(id, reactive);
        }


        public async Task<BaseList> GetAll(EmployeeRequest request)
        {
            return await _unitOfWork.EmployeeRep.GetAll(request);
        }

        /// <summary>
        /// Lấy danh sách nhân viên với đầy đủ thông tin cho chế độ chỉnh sửa mở rộng
        /// Sử dụng stored procedure riêng sp_Employee_getAll_Extended
        /// </summary>
        public async Task<BaseList> GetAllExtended(EmployeeRequest request)
        {
            return await _unitOfWork.EmployeeRep.GetAllExtended(request);
        }

        public async Task<List<EmployeeExtendedModel>> Export(EmployeeRequest request)
        {
            return await _unitOfWork.EmployeeRep.ExecuteExport(request);
        }

        public async Task<BaseList> GetAllManager()
        {
            return await _unitOfWork.EmployeeRep.GetAllManager();
        }

        public async Task<Employee> ConvertToEmployeeFromCandidate(int? requestId)
        {
            return new Employee();
        }




        public async Task<Employee> Login(string userName, string password)
        {
            var passwordGen = getMD5(password);
            return await _unitOfWork.EmployeeRep.Login(userName, passwordGen);

        }

        public async Task<Employee> GetById(int id)
        {

            return await _unitOfWork.EmployeeRep.GetById(id);


        }
        public async Task<Employee> CheckDuplicate(string email, string phone)
        {
            return await _unitOfWork.EmployeeRep.CheckDuplicate(email, phone);
        }

    }
}
