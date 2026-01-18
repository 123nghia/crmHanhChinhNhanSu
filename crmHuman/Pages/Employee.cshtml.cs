using crmHuman.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep.Model;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using crmHuman.Model;
using OfficeOpenXml;

namespace crmHuman.Pages
{
    [Authorize]
    public class EmployeeModel : BaseModel2
    {
        private readonly ILogger<EmployeeModel> _logger;
        private readonly IEmpBusiness _empBusiness;
        private readonly IEmployeeImportBusiness _employeeImportBusiness;
        private readonly IWebHostEnvironment _environment;

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
            IWebHostEnvironment environment,
            ICandidateBusiness candidateBusiness,
            ImasterDataBussiness masterDataBussiness
            )
        {
            _logger = logger;
            _empBusiness = empBusiness;
            _employeeImportBusiness = employeeImportBusiness;
            _environment = environment;
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
                request.NewPassword = "Vietstar@2026";
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
                FingerprintCode = employee.FingerprintCode,
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

            await ApplyDefaultStatusWork(request2);

            // Sử dụng GetAllExtended để lấy đầy đủ dữ liệu cho chế độ chỉnh sửa mở rộng
            DataAll = await _empBusiness.GetAllExtended(request2);

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

        private async Task ApplyDefaultStatusWork(EmployeeRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.StatusWork))
            {
                return;
            }

            var statusOptions = await _masterDataBussiness.GetallByTypeData(11);
            if (statusOptions == null || statusOptions.Count == 0)
            {
                return;
            }

            var defaultStatus = statusOptions.FirstOrDefault(option =>
                NormalizeText(option.Name).Contains("dang lam viec"));

            if (defaultStatus == null)
            {
                defaultStatus = statusOptions.FirstOrDefault(option =>
                    NormalizeText(option.Name).Contains("dang lam"));
            }

            if (defaultStatus == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(defaultStatus.Code))
            {
                request.StatusWork = defaultStatus.Code;
                return;
            }

            request.StatusWork = defaultStatus.Id > 0 ? defaultStatus.Id.ToString() : request.StatusWork;
        }

        private static string NormalizeText(string? value)
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

            return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
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
            var educationOptions = await _masterDataBussiness.GetallByTypeData(14);
            var maritalOptions = await _masterDataBussiness.GetallByTypeData(13);
            ViewData["ReligionOptions"] = religionOptions;
            ViewData["EducationOptions"] = educationOptions;
            ViewData["MaritalOptions"] = maritalOptions;
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



        /// <summary>
        /// Quick update - Cập nhật nhanh một hoặc nhiều trường từ editable grid
        /// Hỗ trợ cả các trường cơ bản và mở rộng
        /// </summary>
        public async Task<IActionResult> OnPostQuickUpdate([FromForm] EmployeeQuickUpdate request)
        {
            var errors = new List<object>();
            
            if (!request.Id.HasValue || request.Id.Value <= 0)
            {
                errors.Add(new { name = "Id", content = "Id không hợp lệ" });
                return ApiResponseHelper.BadRequest(errors);
            }

            try
            {
                GetInfoUser();
                var employee = await _empBusiness.GetById(request.Id.Value);
                if (employee == null)
                {
                    errors.Add(new { name = "Id", content = "Không tìm thấy nhân viên" });
                    return ApiResponseHelper.BadRequest(errors);
                }

                var itemUpdate = new EmployeeInfoAdd
                {
                    Id = employee.Id,
                    
                    // Các trường cơ bản
                    FullName = !string.IsNullOrEmpty(request.FullName) ? request.FullName : employee.FullName,
                    RoleCode = !string.IsNullOrEmpty(request.RoleCode) ? request.RoleCode : employee.RoleCode,
                    PositionCode = !string.IsNullOrEmpty(request.PositionCode) ? request.PositionCode : employee.PositionCode,
                    DepartmentCode = !string.IsNullOrEmpty(request.DepartmentCode) ? request.DepartmentCode : employee.DepartmentCode,
                    GroupId = request.GroupId ?? employee.GroupId,
                    Status = request.Status ?? employee.Status,
                    StatusWork = !string.IsNullOrEmpty(request.StatusWork) ? request.StatusWork : employee.StatusWork,
                    DocumentStatus = !string.IsNullOrEmpty(request.DocumentStatus) ? request.DocumentStatus : employee.DocumentStatus,
                    Onboard = request.Onboard ?? employee.Onboard,
                    
                    // Các trường mở rộng - Thông tin cá nhân
                    Dob = request.Dob ?? employee.Dob,
                    Gender = !string.IsNullOrEmpty(request.Gender) ? request.Gender : employee.Gender,
                    PlaceOfBirth = !string.IsNullOrEmpty(request.PlaceOfBirth) ? request.PlaceOfBirth : employee.PlaceOfBirth,
                    
                    // CCCD
                    NationalId = !string.IsNullOrEmpty(request.NationalId) ? request.NationalId : employee.NationalId,
                    NationalDate = request.NationalDate ?? employee.NationalDate,
                    NationalPlace = !string.IsNullOrEmpty(request.NationalPlace) ? request.NationalPlace : employee.NationalPlace,
                    
                    // Liên hệ
                    Phone = !string.IsNullOrEmpty(request.Phone) ? request.Phone : employee.Phone,
                    FingerprintCode = employee.FingerprintCode,
                    EmergencyContact = !string.IsNullOrEmpty(request.EmergencyContact) ? request.EmergencyContact : employee.EmergencyContact,
                    Email = !string.IsNullOrEmpty(request.Email) ? request.Email : employee.Email,
                    PersonalEmail = !string.IsNullOrEmpty(request.PersonalEmail) ? request.PersonalEmail : employee.PersonalEmail,
                    
                    // Địa chỉ
                    PermanentAddress = !string.IsNullOrEmpty(request.PermanentAddress) ? request.PermanentAddress : employee.PermanentAddress,
                    TemporaryAddress = !string.IsNullOrEmpty(request.TemporaryAddress) ? request.TemporaryAddress : employee.TemporaryAddress,
                    
                    // Học vấn, tình trạng
                    EducationLevel = !string.IsNullOrEmpty(request.EducationLevel) ? request.EducationLevel : employee.EducationLevel,
                    Religion = !string.IsNullOrEmpty(request.Religion) ? request.Religion : employee.Religion,
                    Maritalstatus = !string.IsNullOrEmpty(request.Maritalstatus) ? request.Maritalstatus : employee.Maritalstatus,
                    
                    // Ngân hàng
                    BankAccount = !string.IsNullOrEmpty(request.BankAccount) ? request.BankAccount : employee.BankAccount,
                    BankName = !string.IsNullOrEmpty(request.BankName) ? request.BankName : employee.BankName,
                    BeneficiaryName = !string.IsNullOrEmpty(request.BeneficiaryName) ? request.BeneficiaryName : employee.BeneficiaryName,
                    
                    // Các trường không thay đổi
                    UserName = employee.UserName,
                    Pass = employee.Pass,
                    LineCode = employee.LineCode,
                    AvatarFile = employee.AvatarFile,
                    CreateAt = employee.CreateAt,
                    Deleted = employee.Deleted,
                    Noted = employee.Noted,
                    CreatedBy = employee.CreatedBy,
                    IsActive = employee.IsActive,
                    UpdatedBy = UserData.UserId,
                    UpdateAt = DateTime.Now,
                    ColorCode = employee.ColorCode,
                    ManagerId = employee.ManagerId,
                    DocumentCheck = employee.DocumentCheck
                };

                var result = await _empBusiness.Update(itemUpdate);
                return ApiResponseHelper.SuccessResponse(new { success = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in QuickUpdate for employee {Id}", request.Id);
                return ApiResponseHelper.Error("Lỗi khi cập nhật. Vui lòng thử lại.");
            }
        }

        /// <summary>
        /// Quick add - Tạo nhanh nhân viên mới từ editable grid
        /// </summary>
        public async Task<IActionResult> OnPostQuickAdd([FromForm] EmployeeQuickAdd request)
        {
            var errors = new List<object>();

            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                errors.Add(new { name = "FullName", content = "Họ tên là bắt buộc" });
                return ApiResponseHelper.BadRequest(errors);
            }

            try
            {
                GetInfoUser();
                var newEmployee = new EmployeeInfoAdd
                {
                    FullName = request.FullName,
                    RoleCode = request.RoleCode ?? "2",
                    PositionCode = request.PositionCode,
                    DepartmentCode = request.DepartmentCode,
                    FingerprintCode = request.FingerprintCode,
                    GroupId = request.GroupId,
                    Status = request.Status ?? 1,
                    StatusWork = request.StatusWork ?? "1",
                    DocumentStatus = request.DocumentStatus ?? "1",
                    Onboard = request.Onboard ?? DateTime.Now,
                    IsActive = 1,
                    Pass = "Vietstar@2026",
                    CreatedBy = UserData.UserId,
                    CreateAt = DateTime.Now
                };

                var result = await _empBusiness.Add(newEmployee);
                if (result != null && result.Id > 0)
                {
                    return ApiResponseHelper.SuccessResponse(new { success = true, id = result.Id, userName = result.UserName });
                }
                
                return ApiResponseHelper.Error("Không thể tạo nhân viên mới");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in QuickAdd");
                return ApiResponseHelper.Error("Lỗi khi tạo nhân viên. Vui lòng thử lại.");
            }
        }

        /// <summary>
        /// Copy row - Sao chép nhân viên để tạo bản mới
        /// </summary>
        public async Task<IActionResult> OnPostCopyRow([FromForm] int sourceId)
        {
            var errors = new List<object>();

            if (sourceId <= 0)
            {
                errors.Add(new { name = "sourceId", content = "Id nguồn không hợp lệ" });
                return ApiResponseHelper.BadRequest(errors);
            }

            try
            {
                GetInfoUser();
                var sourceEmployee = await _empBusiness.GetById(sourceId);
                if (sourceEmployee == null)
                {
                    errors.Add(new { name = "sourceId", content = "Không tìm thấy nhân viên nguồn" });
                    return ApiResponseHelper.BadRequest(errors);
                }

                var copyEmployee = new EmployeeInfoAdd
                {
                    FullName = sourceEmployee.FullName + " (Copy)",
                    RoleCode = sourceEmployee.RoleCode,
                    PositionCode = sourceEmployee.PositionCode,
                    DepartmentCode = sourceEmployee.DepartmentCode,
                    Status = sourceEmployee.Status,
                    StatusWork = sourceEmployee.StatusWork,
                    DocumentStatus = sourceEmployee.DocumentStatus,
                    Onboard = sourceEmployee.Onboard,
                    Phone = "",
                    Email = "",
                    Pass = "Vietstar@2026",
                    IsActive = 1,
                    CreatedBy = UserData.UserId,
                    CreateAt = DateTime.Now,
                    LineCode = sourceEmployee.LineCode,
                    ColorCode = sourceEmployee.ColorCode,
                    Dob = sourceEmployee.Dob,
                    Gender = sourceEmployee.Gender,
                    PlaceOfBirth = sourceEmployee.PlaceOfBirth,
                    Religion = sourceEmployee.Religion,
                    EducationLevel = sourceEmployee.EducationLevel,
                    Maritalstatus = sourceEmployee.Maritalstatus
                };

                var result = await _empBusiness.Add(copyEmployee);
                if (result != null && result.Id > 0)
                {
                    return ApiResponseHelper.SuccessResponse(new { success = true, id = result.Id, userName = result.UserName });
                }

                return ApiResponseHelper.Error("Không thể sao chép nhân viên");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CopyRow for source {SourceId}", sourceId);
                return ApiResponseHelper.Error("Lỗi khi sao chép. Vui lòng thử lại.");
            }
        }

        public async Task<IActionResult> OnPostExport(EmployeeRequest RequestSearch)
        {
            try
            {
                GetInfoUser();
                RequestSearch.UserId = UserData.UserId;
                await ApplyDefaultStatusWork(RequestSearch);
                
                // Debug Log
                Console.WriteLine($"[Export Debug] UserId: {RequestSearch.UserId}");
                Console.WriteLine($"[Export Debug] Token: '{RequestSearch.Token}'");
                Console.WriteLine($"[Export Debug] GroupId: {RequestSearch.GroupId}");
                Console.WriteLine($"[Export Debug] Status: {RequestSearch.Status}");
                Console.WriteLine($"[Export Debug] From: {RequestSearch.From}, To: {RequestSearch.To}");

                var data = await _empBusiness.Export(RequestSearch);
                
                Console.WriteLine($"[Export Debug] Found {data?.Count ?? 0} records.");

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                var webRoot = _environment.WebRootPath;
                if (string.IsNullOrWhiteSpace(webRoot))
                {
                    webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");
                }

                var templatePath = Path.Combine(webRoot, "export", "exportemployee.xlsx");
                if (!global::System.IO.File.Exists(templatePath))
                {
                    throw new FileNotFoundException($"Template export file not found: {templatePath}", templatePath);
                }

                using (var package = new ExcelPackage(new FileInfo(templatePath)))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    if (worksheet == null)
                    {
                        throw new InvalidOperationException("Template export file does not contain any worksheet.");
                    }

                    const int dataStartRow = 4;
                    const int exportColumnCount = 40;
                    int row = dataStartRow;
                    int stt = 1;

                    var lastRow = worksheet.Dimension?.End.Row ?? 0;
                    if (lastRow >= dataStartRow)
                    {
                        worksheet.Cells[dataStartRow, 1, lastRow, exportColumnCount].Value = null;
                    }


                    static HashSet<string> BuildDocumentSet(string? documentCheck)
                    {
                        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        if (string.IsNullOrWhiteSpace(documentCheck))
                        {
                            return result;
                        }

                        foreach (var doc in documentCheck.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        {
                            var trimmed = doc.Trim();
                            if (trimmed.Length > 0)
                            {
                                result.Add(trimmed);
                            }
                        }

                        return result;
                    }

                    if (data != null && data.Any())
                    {
                        foreach (var item in data)
                        {
                            // Debug log per row (temporary)
                            // Console.WriteLine($"[Export Row] {item.UserName} - TaxMST: '{item.Tax_MST}', PITDate: '{item.Tax_NgayCap}'");

                            var docSet = BuildDocumentSet(item.DocumentCheck);
                            var hasScan = docSet.Contains("BangCap") || docSet.Contains("CamKetNoiQuy") || docSet.Contains("CamKetThue");

                            int col = 1;
                            worksheet.Cells[row, col++].Value = stt++;
                            worksheet.Cells[row, col++].Value = item.FingerprintCode;
                            worksheet.Cells[row, col++].Value = item.FullName;
                            worksheet.Cells[row, col++].Value = item.Onboard?.ToString("dd/MM/yyyy");
                            worksheet.Cells[row, col++].Value = item.PositionText;
                            worksheet.Cells[row, col++].Value = item.ManagerName;
                            worksheet.Cells[row, col++].Value = item.DepartmentText;

                            worksheet.Cells[row, col++].Value = item.Gender;
                            worksheet.Cells[row, col++].Value = item.Dob?.ToString("dd/MM/yyyy");
                            worksheet.Cells[row, col++].Value = item.PlaceOfBirth;
                            worksheet.Cells[row, col++].Value = item.NationalId;
                            worksheet.Cells[row, col++].Value = item.NationalDate?.ToString("dd/MM/yyyy");
                            worksheet.Cells[row, col++].Value = item.PermanentAddress;
                            worksheet.Cells[row, col++].Value = item.TemporaryAddress;
                            var ethnicityValue = !string.IsNullOrWhiteSpace(item.EthnicityText)
                                ? item.EthnicityText
                                : item.Ethnicity;
                            worksheet.Cells[row, col++].Value = ethnicityValue;
                            worksheet.Cells[row, col++].Value = item.ReligionText ?? item.Religion;
                            worksheet.Cells[row, col++].Value = item.MaritalstatusText ?? item.Maritalstatus;
                            worksheet.Cells[row, col++].Value = item.EducationLevelText ?? item.EducationLevel;
                            worksheet.Cells[row, col++].Value = item.Email;
                            worksheet.Cells[row, col++].Value = item.PersonalEmail;
                            worksheet.Cells[row, col++].Value = item.Phone;

                            worksheet.Cells[row, col++].Value = item.RelationName;
                            worksheet.Cells[row, col++].Value = item.RelationText ?? item.RelationCode;
                            worksheet.Cells[row, col++].Value = item.RelationPhone;
                            worksheet.Cells[row, col++].Value = item.RelationAddress;

                            worksheet.Cells[row, col++].Value = docSet.Contains("CCCD") ? "x" : string.Empty;
                            worksheet.Cells[row, col++].Value = docSet.Contains("SYLL") ? "x" : string.Empty;
                            worksheet.Cells[row, col++].Value = docSet.Contains("DonXinViec") ? "x" : string.Empty;
                            worksheet.Cells[row, col++].Value = hasScan ? "x" : string.Empty;

                            worksheet.Cells[row, col++].Value = item.HD_LoaiHD;
                            worksheet.Cells[row, col++].Value = item.HD_SoHD;
                            worksheet.Cells[row, col++].Value = item.HD_NgayBatDau?.ToString("dd/MM/yyyy");
                            worksheet.Cells[row, col++].Value = item.HD_NgayKetThuc?.ToString("dd/MM/yyyy");

                            worksheet.Cells[row, col++].Value = item.BankAccount;
                            worksheet.Cells[row, col++].Value = item.BankName;

                            worksheet.Cells[row, col++].Value = item.Tax_MST;
                            worksheet.Cells[row, col++].Value = item.Tax_NguoiPhuThuoc;

                            worksheet.Cells[row, col++].Value = item.BHXH_SoSo;
                            worksheet.Cells[row, col++].Value = item.BHXH_ThangBatDau?.ToString("MM/yyyy");

                            worksheet.Cells[row, col++].Value = item.ResignationDate?.ToString("dd/MM/yyyy");

                            row++;
                        }
                    }
                    
                    var fileContents = package.GetAsByteArray();
                    string excelName = $"EmployeeList-{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx";
                    return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
                }
            }
            catch (Exception ex)
            {
                 // Log error to console
                 Console.WriteLine($"Export Error: {ex.Message}");
                 // Return error as a text file so user can see what happened
                 var errorBytes = global::System.Text.Encoding.UTF8.GetBytes($"Lỗi xuất file: {ex.Message}\nStackTrace: {ex.StackTrace}");
                 return File(errorBytes, "text/plain", "Error_Log.txt");
            }
        }
    }
}
