using crmHuman.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using System.Linq;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Item;

namespace crmHuman.Pages
{
    [Authorize]
    public class ScheduleInterviewModel : BaseModel2
    {
        private readonly IScheduleInterviewBussiness _scheduleInterviewBussiness;
        private readonly ICandidateBusiness _candidateBusiness;
        private readonly IEmpBusiness _empBusiness;
        private readonly IOnboardMemberBusiness _onboardMemberBusiness;
        private readonly INotificationBusiness _notificationBusiness;
        public BaseList DataAll { get; set; }
        public BaseList CandidateList { get; set; }
        public BaseList InterviewerList { get; set; }
        public ScheduleInterviewRquest RequestSearch { get; set; }

        public ScheduleInterviewModel(
            IScheduleInterviewBussiness scheduleInterviewBussiness,
            ICandidateBusiness candidateBusiness,
            IEmpBusiness empBusiness,
            IOnboardMemberBusiness onboardMemberBusiness,
            INotificationBusiness notificationBusiness)
        {
            _scheduleInterviewBussiness = scheduleInterviewBussiness;
            _candidateBusiness = candidateBusiness;
            _empBusiness = empBusiness;
            _onboardMemberBusiness = onboardMemberBusiness;
            _notificationBusiness = notificationBusiness;
            TitlePage = "Danh sach phong van";
            KeyPage = "ScheduleInterview";
            DataAll = new BaseList();
            CandidateList = new BaseList();
            InterviewerList = new BaseList();
            RequestSearch = new ScheduleInterviewRquest();
        }

        public async Task<IActionResult> OnPostAddSchedule(CandidateScheduleAdd request)
        {
            GetInfoUser();
            var errors = new List<object>();
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            var canEdit = (Permision != null && (Permision.Add == true || Permision.Edit == true)) || CanManageRecruitmentData();
            if (!canEdit)
            {
                return ApiResponseHelper.Error("Không có quyền thực hiện thao tác này", StatusCodes.Status403Forbidden);
            }

            var userId = UserData?.UserId ?? 0;
            var roleCode = UserData?.RoleCode;
            if (request.RelId <= 0 || !await _candidateBusiness.HasManageAccess(request.RelId, userId, roleCode))
            {
                return ApiResponseHelper.Error("Khong co quyen tao hoac cap nhat lich cho ung vien nay", StatusCodes.Status403Forbidden);
            }

            if (request.Id > 0)
            {
                var existingSchedule = await _scheduleInterviewBussiness.GetById(request.Id);
                if (existingSchedule == null || existingSchedule.Id <= 0)
                {
                    return ApiResponseHelper.Error("Khong tim thay lich phong van", StatusCodes.Status404NotFound);
                }

                if (!await _candidateBusiness.HasManageAccess(existingSchedule.RelId, userId, roleCode))
                {
                    return ApiResponseHelper.Error("Khong co quyen cap nhat lich phong van nay", StatusCodes.Status403Forbidden);
                }

                request.RelId = existingSchedule.RelId;
            }

            var itemInsert = new ScheduleInterviewAdd()
            {
                Id = request.Id,
                RelId = request.RelId,
                RelCode = request.RelCode,
                Type = request.Type,
                ScheduleDate = request.ScheduleDate,
                AddressInfo = request.AddressInfo,
                Noted = request.Noted,
                Status = request.Status,
                InterviewerId = request.InterviewerId,
                InterviewMode = request.InterviewMode,
                InterviewResult = request.InterviewResult,
                SendEmail = request.SendEmail,
                CreatedBy = userId,
                UpdatedBy = userId
            };

            var result = await _scheduleInterviewBussiness.SaveInterviewSchedule(itemInsert, userId);
            if (_notificationBusiness != null && result.Success && request.RelId > 0 && string.IsNullOrWhiteSpace("disabled"))
            {
                var scheduleText = request.ScheduleDate?.ToString("HH:mm dd/MM/yyyy") ?? "";
                if (request.Id <= 0)
                {
                    await _notificationBusiness.CreateNotification(
                        request.RelId,
                        $"Bạn có lịch phỏng vấn mới vào {scheduleText}.",
                        "/Candidate/Dashboard",
                        "InterviewScheduled"
                    );
                }
                else
                {
                    await _notificationBusiness.CreateNotification(
                        request.RelId,
                        $"Lịch phỏng vấn của bạn vào {scheduleText} đã được cập nhật.",
                        "/Candidate/Dashboard",
                        "InterviewUpdated"
                    );
                }
            }
            return ApiResponseHelper.SuccessResponse(new { success = result.Success, message = result.Message });
        }

        public async Task<IActionResult> OnPostDeleteSchedule(int Id)
        {
            GetInfoUser();
            if (Id <= 0)
            {
                return ApiResponseHelper.Error("Thiếu thông tin lịch phỏng vấn");
            }

            var userId = UserData?.UserId ?? 0;
            if (!await _scheduleInterviewBussiness.HasManageAccess(Id, userId, UserData?.RoleCode))
            {
                return ApiResponseHelper.Error("Không có quyền xóa lịch phỏng vấn", StatusCodes.Status403Forbidden);
            }

            var result = await _scheduleInterviewBussiness.Delete(Id);
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }

        public async Task<IActionResult> OnPostUpdateResult(int Id, int? Type, int? InterviewerId, DateTime? ScheduleDate, int? InterviewMode, int? InterviewResult, int? Status, string AddressInfo, string Noted)
        {
            GetInfoUser();
            var errors = new List<object>();
            if (Id <= 0)
            {
                errors.Add(new { Content = "Thiếu lịch phỏng vấn" });
            }
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            var scheduleItem = await _scheduleInterviewBussiness.GetById(Id);
            if (scheduleItem == null || scheduleItem.Id <= 0)
            {
                return ApiResponseHelper.Error("Không tìm thấy lịch phỏng vấn");
            }

            var userId = UserData?.UserId ?? 0;
            var canManage = await _scheduleInterviewBussiness.HasManageAccess(Id, userId, UserData?.RoleCode);
            var isInterviewer = UserData != null && UserData.UserId == scheduleItem.InterviewerId;
            if (!canManage && !isInterviewer)
            {
                return ApiResponseHelper.Error("Không có quyền cập nhật kết quả", StatusCodes.Status403Forbidden);
            }

            var updateItem = new ScheduleInterviewAdd()
            {
                Id = scheduleItem.Id,
                RelId = scheduleItem.RelId,
                RelCode = scheduleItem.RelCode,
                Type = Type ?? scheduleItem.Type,
                ScheduleDate = ScheduleDate ?? scheduleItem.ScheduleDate,
                AddressInfo = AddressInfo ?? scheduleItem.AddressInfo,
                Noted = Noted ?? scheduleItem.Noted,
                Status = Status ?? scheduleItem.Status,
                InterviewerId = InterviewerId ?? scheduleItem.InterviewerId,
                InterviewMode = InterviewMode ?? scheduleItem.InterviewMode,
                InterviewResult = InterviewResult ?? scheduleItem.InterviewResult,
                UpdatedBy = userId
            };

            var result = await _scheduleInterviewBussiness.AddOrUpdate(updateItem);
            if (result && InterviewResult.HasValue && scheduleItem.RelId > 0)
            {
                var resultText = InterviewResult.Value switch
                {
                    1 => "Đạt",
                    2 => "Không đạt",
                    3 => "Chờ quyết định",
                    _ => "Đã cập nhật"
                };
                await _notificationBusiness.CreateNotification(
                    scheduleItem.RelId,
                    $"Kết quả phỏng vấn của bạn: {resultText}.",
                    "/Candidate/Dashboard",
                    "InterviewResult"
                );
            }
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }

        public async Task<IActionResult> OnPostExport(ScheduleInterviewRquest request)
        {
            try
            {
                if (!HttpContext.User.Identity.IsAuthenticated)
                {
                    return Redirect("/Login");
                }

                GetInfoUser();
                RequestSearch = request ?? new ScheduleInterviewRquest();

                if (RequestSearch.Type == null)
                {
                    RequestSearch.Type = -1;
                }
                if (RequestSearch.Status == null)
                {
                    RequestSearch.Status = -1;
                }
                if (RequestSearch.InterviewerId == null)
                {
                    RequestSearch.InterviewerId = -1;
                }
                if (RequestSearch.InterviewMode == null)
                {
                    RequestSearch.InterviewMode = -1;
                }

                var hasFrom = Request.Form.ContainsKey("From") && !string.IsNullOrWhiteSpace(Request.Form["From"]);
                var hasTo = Request.Form.ContainsKey("To") && !string.IsNullOrWhiteSpace(Request.Form["To"]);
                if (!hasFrom)
                {
                    RequestSearch.From = null;
                }
                if (!hasTo)
                {
                    RequestSearch.To = null;
                }

                ApplyInterviewScope(RequestSearch);

                RequestSearch.Page = 1;
                RequestSearch.Limit = 1000;

                var data = await _scheduleInterviewBussiness.GetAll(RequestSearch);

                var candidateList = await _candidateBusiness.GetAll(new CandidateRequest()
                {
                    UserId = UserData?.UserId,
                    RoleCode = UserData?.RoleCode,
                    LoadAll = 1,
                    Page = 1,
                    Limit = 1000
                });

                var candidateLookup = new Dictionary<int, CandidateIndexModel>();
                if (candidateList?.Data != null)
                {
                    foreach (var item in candidateList.Data)
                    {
                        if (item is CandidateIndexModel candidateInfo)
                        {
                            candidateLookup[candidateInfo.Id] = candidateInfo;
                        }
                    }
                }

                var onboardLookup = new Dictionary<int, DateTime?>();
                var onboardRequest = new OrderRequest
                {
                    Page = 1,
                    Limit = 1000,
                    Status = -1,
                    Job = -1,
                    GroupId = -1,
                    MemberId = -1,
                    RoleCode = UserData?.RoleCode,
                    UserId = 0
                };
                onboardRequest.From = null;
                onboardRequest.To = null;

                var onboardList = await _onboardMemberBusiness.GetAll(onboardRequest);
                if (onboardList?.Data != null)
                {
                    foreach (var item in onboardList.Data)
                    {
                        if (item is OnboardMemberIndexModel onboardInfo && onboardInfo.CandidateId.HasValue)
                        {
                            onboardLookup[onboardInfo.CandidateId.Value] = onboardInfo.OnboardDate;
                        }
                    }
                }

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("PHỎNG VẤN");
                    var headers = new[]
                    {
                        "STT",
                        "HỌ VÀ TÊN",
                        "VỊ TRÍ ỨNG TUYỂN",
                        "QUẢN LÝ TRỰC TIẾP",
                        "NGÀY SINH",
                        "CCCD",
                        "ĐỊA CHỈ",
                        "SDT",
                        "EMAIL",
                        "LỊCH PV",
                        "KẾT QUẢ PV",
                        "NGÀY NHẬN VIỆC",
                        "GHI CHÚ"
                    };

                    const int headerRow = 2;
                    for (int col = 1; col <= headers.Length; col++)
                    {
                        worksheet.Cells[headerRow, col].Value = headers[col - 1];
                        worksheet.Cells[headerRow, col].Style.Font.Bold = true;
                    }

                    int row = headerRow + 1;
                    int stt = 1;
                    if (data?.Data != null)
                    {
                        foreach (var item in data.Data)
                        {
                            if (item is not ScheduleInterviewIndexModel scheduleItem)
                            {
                                continue;
                            }

                            CandidateIndexModel? candidateInfo = null;
                            if (scheduleItem.RelId.HasValue && candidateLookup.TryGetValue(scheduleItem.RelId.Value, out var candidateFound))
                            {
                                candidateInfo = candidateFound;
                            }

                            var candidateName = candidateInfo?.Name ?? scheduleItem.CandidateFullName ?? string.Empty;
                            var candidatePosition = candidateInfo?.PostionName ?? scheduleItem.PositionText ?? string.Empty;

                            var scheduleText = scheduleItem.ScheduleDate.HasValue
                                ? scheduleItem.ScheduleDate.Value.ToString("HH'h' ngày dd/MM/yyyy")
                                : string.Empty;

                            var resultText = scheduleItem.InterviewResult switch
                            {
                                1 => "Pass",
                                2 => "Fail",
                                3 => "Hold",
                                _ => string.Empty
                            };

                            string onboardText = string.Empty;
                            if (candidateInfo?.ExpectedOnboardDate != null)
                            {
                                onboardText = candidateInfo.ExpectedOnboardDate.Value.ToString("dd/MM/yyyy");
                            }
                            else if (scheduleItem.RelId.HasValue && onboardLookup.TryGetValue(scheduleItem.RelId.Value, out var onboardDate) && onboardDate.HasValue)
                            {
                                onboardText = onboardDate.Value.ToString("dd/MM/yyyy");
                            }

                            int col = 1;
                            worksheet.Cells[row, col++].Value = stt++;
                            worksheet.Cells[row, col++].Value = candidateName;
                            worksheet.Cells[row, col++].Value = candidatePosition;
                            worksheet.Cells[row, col++].Value = candidateInfo?.ManagerName ?? string.Empty;
                            worksheet.Cells[row, col++].Value = candidateInfo?.Dob?.ToString("dd/MM/yyyy") ?? string.Empty;
                            worksheet.Cells[row, col++].Value = candidateInfo?.NationalId ?? string.Empty;
                            worksheet.Cells[row, col++].Value = candidateInfo?.Address ?? string.Empty;
                            worksheet.Cells[row, col++].Value = candidateInfo?.Phone ?? string.Empty;
                            worksheet.Cells[row, col++].Value = candidateInfo?.Email ?? string.Empty;
                            worksheet.Cells[row, col++].Value = scheduleText;
                            worksheet.Cells[row, col++].Value = resultText;
                            worksheet.Cells[row, col++].Value = onboardText;
                            worksheet.Cells[row, col++].Value = scheduleItem.Noted ?? string.Empty;
                            row++;
                        }
                    }

                    worksheet.Cells[headerRow, 1, Math.Max(row - 1, headerRow), headers.Length].AutoFitColumns();

                    var fileContents = package.GetAsByteArray();
                    var fileName = $"InterviewList-{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                var errorBytes = global::System.Text.Encoding.UTF8.GetBytes($"Loi xuat file: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return File(errorBytes, "text/plain", "Error_Log.txt");
            }
        }

        public async Task<IActionResult> OnGet([FromQuery] ScheduleInterviewRquest request)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            RequestSearch = request ?? new ScheduleInterviewRquest();

            if (RequestSearch.Type == null)
            {
                RequestSearch.Type = -1;
            }
            if (RequestSearch.Status == null)
            {
                RequestSearch.Status = -1;
            }
            if (RequestSearch.InterviewerId == null)
            {
                RequestSearch.InterviewerId = -1;
            }
            if (RequestSearch.InterviewMode == null)
            {
                RequestSearch.InterviewMode = -1;
            }

            var hasFrom = Request.Query.ContainsKey("From") && !string.IsNullOrWhiteSpace(Request.Query["From"]);
            var hasTo = Request.Query.ContainsKey("To") && !string.IsNullOrWhiteSpace(Request.Query["To"]);
            if (!hasFrom)
            {
                RequestSearch.From = null;
            }
            if (!hasTo)
            {
                RequestSearch.To = null;
            }

            ApplyInterviewScope(RequestSearch);

            DataAll = await _scheduleInterviewBussiness.GetAll(RequestSearch);

            CandidateList = await _candidateBusiness.GetAll(new CandidateRequest()
            {
                UserId = UserData?.UserId,
                RoleCode = UserData?.RoleCode,
                LoadAll = 1,
                Page = 1,
                Limit = 1000
            });
            InterviewerList = await BuildInterviewerListAsync();

            return Page();
        }

        private void ApplyInterviewScope(ScheduleInterviewRquest request)
        {
            var userId = UserData?.UserId ?? 0;

            if (userId > 0)
            {
                request.UserId = userId;
            }
        }

        private async Task<BaseList> BuildInterviewerListAsync()
        {
            var interviewers = await _empBusiness.GetByRoleCodes(new[] { "1", "3", "6", "8", "9" });
            return new BaseList
            {
                Total = interviewers.Count,
                Data = interviewers
                    .Where(x => x != null && x.Id > 0 && !string.IsNullOrWhiteSpace(x.FullName))
                    .OrderBy(x => x.FullName)
                    .ToList()
            };
        }

        private bool CanManageRecruitmentData()
        {
            var roleCode = UserData?.RoleCode ?? string.Empty;
            return roleCode == "1" || roleCode == "3" || roleCode == "6" || roleCode == "8" || roleCode == "9";
        }
    }
}
