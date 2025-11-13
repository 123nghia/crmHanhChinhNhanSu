using Microsoft.AspNetCore.Http;
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

        public async Task<bool> Add(EmployeeInfoAdd itemAdd)
        {
            var item = new Employee();
            item.FullName = itemAdd.FullName;
            item.Onboard = itemAdd.Onboard;
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
            item.Noted = itemAdd.Noted;
            item.NationalId = itemAdd.NationalId;
            item.NationalDate = itemAdd.NationalDate;
            item.NationalPlace = itemAdd.NationalPlace;
            item.Phone = itemAdd.Phone;
            var passNew = getMD5(itemAdd.Pass);
            item.Pass = passNew;
            item.CreateAt = DateTime.Now;
            item.CreatedBy = GetUserId();
            return await _unitOfWork.EmployeeRep.AddOrUpdate(item);
        }


        public async Task<bool> ChangePassword(string password, int id)
        {
            var passwordNew = getMD5(password);

            return await _unitOfWork.EmployeeRep.ChangePassword(passwordNew, id);
        }
        public async Task<bool> Update(EmployeeInfoAdd itemUpdate)
        {
            var item = new Employee();
            item.FullName = itemUpdate.FullName;
            item.Id = itemUpdate.Id;
            item.RoleCode = itemUpdate.RoleCode;
            item.Dob = itemUpdate.Dob;
            item.ManagerId = itemUpdate.ManagerId;
            item.DepartmentCode = itemUpdate.DepartmentCode;
            item.PositionCode = itemUpdate.PositionCode;
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
            item.UpdatedBy = GetUserId();
            item.UpdateAt = DateTime.Now;
            
            // Khi thêm mới nhân viên (Id < 0), cần set UserName và Pass
            if (itemUpdate.Id < 0)
            {
                // Generate UserName từ email hoặc phone nếu chưa có
                if (string.IsNullOrEmpty(itemUpdate.UserName))
                {
                    if (!string.IsNullOrEmpty(itemUpdate.Email))
                    {
                        // Lấy phần trước @ của email làm UserName
                        item.UserName = itemUpdate.Email.Split('@')[0];
                    }
                    else if (!string.IsNullOrEmpty(itemUpdate.Phone))
                    {
                        // Dùng phone làm UserName nếu không có email
                        item.UserName = itemUpdate.Phone;
                    }
                    else
                    {
                        // Nếu không có cả email và phone, dùng FullName (loại bỏ dấu cách)
                        item.UserName = itemUpdate.FullName?.Replace(" ", "").ToLower() ?? "user" + DateTime.Now.Ticks;
                    }
                }
                else
                {
                    item.UserName = itemUpdate.UserName;
                }
                
                // Set mật khẩu mặc định nếu chưa có
                if (string.IsNullOrEmpty(itemUpdate.Pass))
                {
                    item.Pass = getMD5("Vietstar@2024"); // Mật khẩu mặc định
                }
                else
                {
                    item.Pass = getMD5(itemUpdate.Pass);
                }
                
                item.CreateAt = DateTime.Now;
                item.CreatedBy = GetUserId();
            }
            else
            {
                // Khi update, lấy thông tin từ database để giữ nguyên UserName, Pass, CreatedBy, CreateAt
                var existingEmployee = await _unitOfWork.EmployeeRep.GetById(itemUpdate.Id);
                if (existingEmployee != null)
                {
                    item.UserName = existingEmployee.UserName;
                    item.Pass = existingEmployee.Pass; // Giữ nguyên password cũ khi update
                    item.CreatedBy = existingEmployee.CreatedBy; // Giữ nguyên CreatedBy
                    item.CreateAt = existingEmployee.CreateAt; // Giữ nguyên CreateAt
                }
            }
            
            return await _unitOfWork.EmployeeRep.AddOrUpdate(item);
        }
        public Task<bool> Delete(int id, bool reactive = false)
        {
            return _unitOfWork.EmployeeRep.Delete(id, reactive);
        }


        public async Task<BaseList> GetAll(EmployeeRequest request)
        {
            return await _unitOfWork.EmployeeRep.GetAll(request);
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
