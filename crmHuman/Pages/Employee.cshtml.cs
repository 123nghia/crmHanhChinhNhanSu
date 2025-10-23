using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace crmHuman.Pages
{
    [Authorize]
    public class EmployeeModel : BaseModel2
    {
        private readonly ILogger<EmployeeModel> _logger;
        private readonly IEmpBusiness _empBusiness;

        private readonly ICandidateBusiness _candidateBusiness;

        public List<string> TableColumnTextAdmin { get; set; }
        public EmployeeRequest RequestSearch { get; set; }
        public BaseList DataAll { get; set; }



        public int TotalRecord
        {

            get
            {
                return DataAll.Total;

            }
        }
        public EmployeeModel(ILogger<EmployeeModel> logger,
            IEmpBusiness empBusiness,
            ICandidateBusiness candidateBusiness
            )
        {
            _logger = logger;
            _empBusiness = empBusiness;
            TitlePage = "Danh sách nhân viên";
            KeyPage = "Employee";

            TableColumnText = new List<string>()
            {
                "STT","UserName","Họ tên","Vai trò", "Vị trí", "Bộ phận","Nhóm", "Trạng thái",
                "Trạng thái chứng từ",
                "Ngày Onboard","Cập nhật gần nhất","Thao tác"
            };

            TableColumnTextAdmin = new List<string>()
            {
                "STT","UserName","Họ tên"
                ,"Vai trò", "Vị trí", "Bộ phận","Nhóm","Trạng thái", "Trạng thái chứng từ", "Ngày Onboard","Cập nhật gần nhất","Thao tác"
            };
            _candidateBusiness = candidateBusiness;

        }

        public async Task<IActionResult> OnPostChangePassword
         (PasswordAdd request)
        {
            var listEror = new List<object>();
            if (string.IsNullOrEmpty(request.NewPassword))
            {
                var itemError = new
                {
                    name = "txtrenewPassword",
                    Content = "Thiếu thông tin họ và tên"
                };
                listEror.Add(itemError);
            }
            if (listEror.Count > 0)
            {
                return new JsonResult(listEror)
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            int IdEmployer = -1;
            if (request.ResetPass == true)
            {
                request.NewPassword = "Vietstar@2024";
            }
            if (request.Id.HasValue && request.Id.Value > 0)
            {
                IdEmployer = request.Id.Value;
            }
            else
            {
                GetInfoUser();
                var userId = UserData.UserId;
                IdEmployer = userId;
            }


            var result = await _empBusiness.ChangePassword(request.NewPassword, IdEmployer);
            var dataReponse = new
            {
                success = result,

            };
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK

            };
        }


        public async Task<IActionResult> OnPostUpdateInfo
         (EmployeeAdd request)
        {
            var listEror = new List<object>();
            if (string.IsNullOrEmpty(request.FullName))
            {
                var itemError = new
                {
                    name = "txtUpdateName",
                    Content = "Thiếu thông tin họ và tên"
                };
                listEror.Add(itemError);
            }
            if (listEror.Count > 0)
            {
                return new JsonResult(listEror)
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
            GetInfoUser();
            var userId = UserData.UserId;
            var employee = await _empBusiness.GetById(userId);
            employee.FullName = request.FullName;
            employee.Noted = request.Noted;
            var itemUpdate = new EmployeeInfoAdd()
            {
                Status = employee.Status,
                Phone = employee.Phone,
                UserName = employee.UserName,
                Onboard = employee.Onboard,
                LineCode = employee.LineCode,
                Dob = employee.Dob,
                AvatarFile = employee.AvatarFile,
                CreateAt = employee.CreateAt,
                Deleted = employee.Deleted,
                Noted = request.Noted,
                Email = employee.Email,
                CreatedBy = employee.CreatedBy,
                FullName = request.FullName,
                IsActive = employee.IsActive,
                Id = employee.Id,
                RoleCode = employee.RoleCode,
                UpdateAt = employee.UpdateAt,
                UpdatedBy = employee.UpdatedBy,
                Pass = employee.Pass
            };
            if (UserData.RoleCode == "1")
            {
                itemUpdate.IsActive = request.IsActive;
            }
            itemUpdate.UpdatedBy = userId;
            var result = await _empBusiness.Update(itemUpdate);
            var dataReponse = new
            {
                success = result,

            };
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK

            };
        }

        public async Task<IActionResult> OnPostConvertToEmployee
          (ConvertToEmployeeAdd request)
        {
            var listEror = new List<object>();
            if (request.Id.Value < 1)
            {
                var itemError = new
                {

                    Content = "Thiếu đối tượng ID"
                };
                listEror.Add(itemError);
            }
            if (listEror.Count > 0)
            {
                return new JsonResult(listEror)
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
            // GetInfoUser();
            // var userId = UserData.UserId;


            var result = await _empBusiness.ConvertToEmployeeFromCandidate(request.Id.Value);
            var dataReponse = new
            {
                success = result,

            };
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK

            };
        }

        public async Task<ActionResult> OnGet([FromQuery] EmployeeRequest request)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }
            GetInfoUser();
            if (UserData.RoleCode == "2")
            {
                return Redirect("/");
            }
            return await GetAll(request);
        }

        public async Task<ActionResult> GetAll(EmployeeRequest request2)
        {
            RequestSearch = request2;
            request2.UserId = UserData.UserId;
            if (UserData.RoleCode == "1")
            {
                request2.IsDeleted = true;
            }
            else
            {
                request2.IsDeleted = false;
            }

            DataAll = await _empBusiness.GetAll(request2);

            if (UserData.RoleCode == "6")
            {
                TableColumnText = new List<string>()
                    {
                        "STT","Họ tên","Tài khoản","Vai trò","Nhóm", "Ngày Onboard","Cập nhật gần nhất","Thao tác"
                    };

                TableColumnTextAdmin = new List<string>()
                    {
                        "STT","Họ tên","Tài khoản","Vai trò","Nhóm","Trạng thái", "Ngày Onboard","Cập nhật gần nhất","Thao tác"
                    };
            }
            return Page();
        }

        public virtual async Task<PartialViewResult> OnGetFormEdit(int id)
        {
            GetInfoUser();
            var resultView = new Employee()
            {
                Id = id,
                UserName = "",
                FullName = "",
                IsActive = 1,
                Phone = "",
                Status = 1,
                RoleCode = UserData.RoleCode,
                Deleted = false,
                Noted = ""
            };
            if (id > 0)
            {
                resultView = await _empBusiness.GetById(id);
            }
            return Partial("editOrUpdateEmployee", resultView);
        }

        public virtual async Task<PartialViewResult> OnGetFormChangePassword(int id)

        {

            var resultView = new
            {
                Id = id
            };
            return Partial("formChangePassword", resultView);
        }

        public async Task<IActionResult> OnPostDelete
      (int Id = -1)
        {

            var listEror = new List<object>();

            if (Id < 0)
            {
                var itemError = new
                {
                    name = "id",
                    Content = "Thiếu thông tin cần xoá"
                };
                listEror.Add(itemError);

            }
            if (listEror.Count > 0)
            {
                return new JsonResult(listEror)
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            var result = true;

            result = await _empBusiness.Delete(Id);
            var dataReponse = new
            {
                success = result,

            };
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK

            };
        }


        public async Task<IActionResult> OnPostReactive
     (int Id = -1)
        {

            var listEror = new List<object>();

            if (Id < 0)
            {
                var itemError = new
                {
                    name = "id",
                    Content = "Thiếu thông tin cần xoá"
                };
                listEror.Add(itemError);

            }
            if (listEror.Count > 0)
            {
                return new JsonResult(listEror)
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            var result = true;

            result = await _empBusiness.Delete(Id, true);
            var dataReponse = new
            {
                success = result,

            };
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK

            };
        }



    }
}
