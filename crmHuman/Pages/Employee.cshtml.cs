using crmHuman.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep.Model;
using System;
using System.Linq;

namespace crmHuman.Pages
{
    [Authorize]
    public class EmployeeModel : BaseModel2
    {
        private readonly ILogger<EmployeeModel> _logger;
        private readonly IEmpBusiness _empBusiness;
        private readonly IEmployeeImportBusiness _employeeImportBusiness;

        private readonly ICandidateBusiness _candidateBusiness;
        private readonly ImasterDataBussiness _masterDataBussiness;

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
            IEmployeeImportBusiness employeeImportBusiness,
            ICandidateBusiness candidateBusiness,
            ImasterDataBussiness masterDataBussiness
            )
        {
            _logger = logger;
            _empBusiness = empBusiness;
            _employeeImportBusiness = employeeImportBusiness;
            TitlePage = "Danh sách nhân viên";
            KeyPage = "Employee";

            TableColumnText = new List<string>()
            {
                "STT","UserName","Họ tên","Vai trò", "Vị trí", "Bộ phận","Nhóm", "Trạng thái",
                "Trạng thái làm việc", "Trạng thái chứng từ",
                "Ngày Onboard","Cập nhật gần nhất","Thao tác"
            };

            TableColumnTextAdmin = new List<string>()
            {
                "STT","UserName","Họ tên"
                ,"Vai trò", "Vị trí", "Bộ phận","Nhóm","Trạng thái", "Trạng thái làm việc", "Trạng thái chứng từ", "Ngày Onboard","Cập nhật gần nhất","Thao tác"
            };
            _candidateBusiness = candidateBusiness;
            _masterDataBussiness = masterDataBussiness;

        }

        public async Task<IActionResult> OnPostChangePassword(PasswordAdd request)
        {
            var errors = new List<object>();
            ValidationHelper.ValidateRequired(request.NewPassword, "txtrenewPassword", "mật khẩu mới", errors);
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            // Reset password if requested
            if (request.ResetPass == true)
            {
                request.NewPassword = "Vietstar@2024";
            }

            // Determine employee ID
            int employeeId = -1;
            if (request.Id.HasValue && request.Id.Value > 0)
            {
                employeeId = request.Id.Value;
            }
            else
            {
                GetInfoUser();
                employeeId = UserData.UserId;
            }

            var result = await _empBusiness.ChangePassword(request.NewPassword, employeeId);
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }


        public async Task<IActionResult> OnPostUpdateInfo(EmployeeAdd request)
        {
            var errors = new List<object>();
            ValidationHelper.ValidateFullName(request.FullName, "txtUpdateName", errors);
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
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
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }

        public async Task<IActionResult> OnPostConvertToEmployee(ConvertToEmployeeAdd request)
        {
            var errors = new List<object>();
            if (request.Id.HasValue)
            {
                ValidationHelper.ValidateId(request.Id.Value, "Id", "đối tượng ID", errors);
            }
            else
            {
                errors.Add(new { Content = "Thiếu đối tượng ID" });
            }
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            var result = await _empBusiness.ConvertToEmployeeFromCandidate(request.Id.Value);
            return ApiResponseHelper.SuccessResponse(new { success = result });
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
            var religionOptions = await _masterDataBussiness.GetallByTypeData(20);
            ViewData["ReligionOptions"] = religionOptions;
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

        public virtual PartialViewResult OnGetFormImportEmployee()
        {
            return Partial("FormImportEmployee", null);
        }

        [RequestSizeLimit(5242880)]
        public async Task<IActionResult> OnPostImportEmployee([FromForm] ImportSourceFileAdd request)
        {
            if (request?.FileRequest == null || request.FileRequest.Length == 0)
            {
                var listEror = new List<object>();
                listEror.Add(new { content = "File không hợp lệ" });
                return ApiResponseHelper.BadRequest(listEror);
            }

            try
            {
                GetInfoUser();
                var importResult = await _employeeImportBusiness.ImportAsync(request.FileRequest, UserData.UserId);
                var formattedErrors = importResult.Errors
                    .Select(e => (object)new { e.Row, e.Content })
                    .ToList();
                var response = new
                {
                    success = importResult.TotalError == 0,
                    importResult.Total,
                    importResult.TotalSuccess,
                    importResult.TotalError,
                    errors = formattedErrors
                };

                if (importResult.TotalError > 0)
                {
                    var errs = string.Join(" | ", importResult.Errors.Select(e => $"Row {e.Row}: {e.Content}"));
                    _logger.LogWarning("Import employee failed: {Errors}", errs);
                    return ApiResponseHelper.BadRequest(formattedErrors);
                }
                return ApiResponseHelper.SuccessResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while importing employees");
                return ApiResponseHelper.Error("Lỗi hệ thống khi import. Vui lòng thử lại sau.");
            }
        }

        public async Task<IActionResult> OnPostDelete(int Id = -1)
        {
            var errors = new List<object>();
            ValidationHelper.ValidateIdForDelete(Id, errors);
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            var result = await _empBusiness.Delete(Id);
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }


        public async Task<IActionResult> OnPostReactive(int Id = -1)
        {
            var errors = new List<object>();
            ValidationHelper.ValidateIdForDelete(Id, errors);
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            var result = await _empBusiness.Delete(Id, true);
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }



    }
}
