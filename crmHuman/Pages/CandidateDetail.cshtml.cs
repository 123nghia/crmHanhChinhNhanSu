using crmHuman.DisplayModel;
using crmHuman.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace crmHuman.Pages
{
    [Authorize]
    public class CandidateDetailModel : BaseModel2
    {
        private readonly ILogger<CandidateModel> _logger;
        private readonly ICandidateBusiness _empBusiness;
        private readonly ImasterDataBussiness _masterDataBussiness;
        private readonly IScheduleInterviewBussiness _scheduleInterviewBussiness;
        private readonly IDocumentDataBussiness _documentDataBussiness;
        private readonly IAuditLogBusiness _auditLogBusiness;

        private readonly IEmpBusiness _empBusiness1;
        public List<string> TableColumnTextAdmin { get; set; }
        public CandidateRequest RequestSearch { get; set; }
        public BaseList DataAll { get; set; }

        public BaseList DataMasterData { get; set; }

        public BaseList DataPostion { get; set; }

        public BaseList DataLead { get; set; }

        public BaseList DataInterviewer { get; set; }


        public BaseList DataHistory { get; set; }

        public CandidateDisplayEdit ResultModel { get; set; }

        public List<DataMasterItem> DataDepartment { get; set; }


        public BaseList DataFile { get; set; }
        public List<AuditLog> CandidateActivities { get; set; }

        public int TotalRecord
        {

            get
            {
                return DataAll.Total;

            }
        }
        public CandidateDetailModel(ILogger<CandidateModel> logger,
            ICandidateBusiness empBusiness,
            ImasterDataBussiness masterDataBussiness,
            IScheduleInterviewBussiness scheduleInterviewBussiness,
            IDocumentDataBussiness documentDataBussiness,
            IAuditLogBusiness auditLogBusiness,
            IEmpBusiness empBusiness1
            )
        {
            _logger = logger;
            _empBusiness = empBusiness;
            TitlePage = "Thông tin ứng viên";
            KeyPage = "CandidateDetail";
            _masterDataBussiness = masterDataBussiness;
            DataPostion = new BaseList();
            DataDepartment = new List<DataMasterItem>();
            _scheduleInterviewBussiness = scheduleInterviewBussiness;
            _documentDataBussiness = documentDataBussiness;
            _auditLogBusiness = auditLogBusiness;
            _empBusiness1 = empBusiness1;
            DataInterviewer = new BaseList();
            CandidateActivities = new List<AuditLog>();
        }

        public async Task<IActionResult> OnPostAddSchedule(CandidateScheduleAdd request)
        {
            GetInfoUser();
            if (UserData.RoleCode == "CANDIDATE")
            {
                return Redirect("/Candidate/Dashboard");
            }

            var errors = new List<object>();
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            var userId = UserData?.UserId ?? 0;
            var roleCode = UserData?.RoleCode;
            if (request.RelId <= 0 || !await _empBusiness.HasManageAccess(request.RelId, userId, roleCode))
            {
                return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
            }

            if (request.Id > 0)
            {
                var existingSchedule = await _scheduleInterviewBussiness.GetById(request.Id);
                if (existingSchedule == null || existingSchedule.Id <= 0)
                {
                    return ApiResponseHelper.Error("Không tìm thấy lịch phỏng vấn", StatusCodes.Status404NotFound);
                }

                if (!await _empBusiness.HasManageAccess(existingSchedule.RelId, userId, roleCode))
                {
                    return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
                }

                request.RelId = existingSchedule.RelId;
            }

            var itemInsert = new ScheduleInterviewAdd()
            {
                Id = request.Id,
                AddressInfo = request.AddressInfo,
                ScheduleDate = request.ScheduleDate,
                Noted = request.Noted,
                RelId = request.RelId,
                Type = request.Type,
                Status = request.Status,
                InterviewerId = request.InterviewerId,
                InterviewMode = request.InterviewMode,
                InterviewResult = request.InterviewResult,
                SendEmail = request.SendEmail,
                CreatedBy = userId,
                UpdatedBy = userId
            };

            var result = await _scheduleInterviewBussiness.SaveInterviewSchedule(itemInsert, userId);
            return ApiResponseHelper.SuccessResponse(new { success = result.Success, message = result.Message });
        }

        public async Task<IActionResult> OnPostDeleteSchedule(int scheduleId)
        {
            GetInfoUser();
            if (UserData.RoleCode == "CANDIDATE")
            {
                return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
            }

            var errors = new List<object>();
            ValidationHelper.ValidateId(scheduleId, "scheduleId", "lịch phỏng vấn", errors);

            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            var schedule = await _scheduleInterviewBussiness.GetById(scheduleId);
            if (schedule == null || schedule.Id <= 0)
            {
                return ApiResponseHelper.Error("Không tìm thấy lịch phỏng vấn", StatusCodes.Status404NotFound);
            }

            var canManageSchedule = await _scheduleInterviewBussiness.HasManageAccess(scheduleId, UserData.UserId, UserData.RoleCode);
            var canManageCandidate = await _empBusiness.HasManageAccess(schedule.RelId, UserData.UserId, UserData.RoleCode);
            if (!canManageSchedule && !canManageCandidate)
            {
                return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
            }

            var result = await _scheduleInterviewBussiness.Delete(scheduleId);
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }

        public async Task<IActionResult> OnGetScheduleById(int scheduleId)
        {
            GetInfoUser();
            if (UserData.RoleCode == "CANDIDATE")
            {
                return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
            }

            var schedule = await _scheduleInterviewBussiness.GetById(scheduleId);
            if (schedule == null || schedule.Id <= 0)
            {
                return ApiResponseHelper.Error("Không tìm thấy lịch phỏng vấn", StatusCodes.Status404NotFound);
            }

            var canViewSchedule = await _scheduleInterviewBussiness.HasViewAccess(scheduleId, UserData.UserId, UserData.RoleCode);
            var canViewCandidate = await _empBusiness.HasViewAccess(schedule.RelId, UserData.UserId, UserData.RoleCode);
            if (!canViewSchedule && !canViewCandidate)
            {
                return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
            }

            return ApiResponseHelper.SuccessResponse(new { 
                success = true, 
                data = new {
                    schedule.Id,
                    schedule.RelId,
                    schedule.Type,
                    schedule.ScheduleDate,
                    schedule.AddressInfo,
                    schedule.Noted,
                    schedule.Status,
                    schedule.InterviewerId,
                    schedule.InterviewMode,
                    schedule.InterviewResult
                }
            });
        }

        public async Task<IActionResult> OnPostUpdate(CandidateDetailUpdate request)
        {
            GetInfoUser();
            var errors = new List<object>();
            ValidationHelper.ValidateId(request.CandidateId, "txtFullName", "đối tượng Id", errors);
            ValidationHelper.ValidatePhone(request.Phone, errors);
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            if (!await _empBusiness.HasManageAccess(request.CandidateId, UserData.UserId, UserData.RoleCode))
            {
                return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
            }

            request.Id = request.CandidateId;
            var result = await _empBusiness.Update(request);
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }

        public async Task<IActionResult> OnPostOnboard(int candidateId)
        {
            GetInfoUser();
            if (UserData.RoleCode == "CANDIDATE")
            {
                return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
            }

            var errors = new List<object>();
            ValidationHelper.ValidateId(candidateId, "candidateId", "d?i tu?ng Id", errors);

            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            if (!await _empBusiness.HasManageAccess(candidateId, UserData.UserId, UserData.RoleCode))
            {
                return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
            }

            var employee = await _empBusiness.Onboard(candidateId);
            if (employee == null || employee.Id <= 0)
            {
                return ApiResponseHelper.SuccessResponse(new { success = false });
            }

            return ApiResponseHelper.SuccessResponse(new { success = true, employeeId = employee.Id });
        }



        public async Task<IActionResult> OnPostAddDocument(DocumentDataAddRequest request)
        {
            GetInfoUser();
            var errors = new List<object>();
            ValidationHelper.ValidateId(request.RelId, "txtFullName", "đối tượng Id", errors);
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            if (!await _empBusiness.HasManageAccess(request.RelId, UserData.UserId, UserData.RoleCode))
            {
                return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
            }

            var result = await _documentDataBussiness.AddOrUpdate(request);
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }

        public async Task<ActionResult> OnGet([FromQuery] CandidateEditRequest request)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }
            GetInfoUser();

            var idInput = request.Id.HasValue == true ? request.Id.Value : -1;
            if (idInput <= 0 || !await _empBusiness.HasViewAccess(idInput, UserData.UserId, UserData.RoleCode))
            {
                return Forbid();
            }

            var dataAllMaster = await _masterDataBussiness.GetAll(new CommonRequest()
            {

            });
            DataMasterData = dataAllMaster;

            foreach (var item in dataAllMaster.Data)
            {

                var tempItem = item as dynamic;

                var itemInsert = new DataMasterItem()
                {
                    Name = tempItem.Name,
                    TypeData = tempItem.TypeData,
                    Code = tempItem.Code,
                    ApplyFor = tempItem.ApplyFor,

                    IsActive = tempItem.IsActive
                };
                DataDepartment.Add(itemInsert);
            }

            DataPostion = dataAllMaster;

            var candidateInfo = await _empBusiness.GetById(idInput);
            var resultView = new CandidateDisplayEdit()
            {
                Id = idInput,
                IsActive = candidateInfo.IsActive,
                Phone = candidateInfo.Phone,
                Status = candidateInfo.Status,
                StatusHuman = candidateInfo.StatusHuman,
                CVLink = candidateInfo.CVLink,
                Name = candidateInfo.Name,
                Dob = candidateInfo.Dob,
                DepartmentId = candidateInfo.DepartmentId,
                Position = candidateInfo.Position,
                Email = candidateInfo.Email,

                Deleted = false,
                CreateAt = candidateInfo.CreateAt,
                UpdateAt = candidateInfo.UpdateAt,
                Referrer = candidateInfo.Referrer,
                Noted = candidateInfo.Noted,
                ManagerId = candidateInfo.ManagerId,
                NationalId = candidateInfo.NationalId,
                Address = candidateInfo.Address,
                UserName = candidateInfo.UserName,
                ExpectedOnboardDate = candidateInfo.ExpectedOnboardDate

            };
            ResultModel = resultView;
            var dataAllHistory = await _scheduleInterviewBussiness.GetAll(new ScheduleInterviewRquest()
            {
                UserId = UserData.UserId,
                RelId = idInput,
                Type = -1,
                From = null,
                To = null
            });
            DataHistory = dataAllHistory;

            DataFile = await _documentDataBussiness.GetAll(new DocumentDataRquest()
            {
                DataType = 1,
                RelId = idInput,
                CurrentUserId = UserData.UserId

            });
            DataLead = await _empBusiness1.GetAllManager();
            DataInterviewer = await BuildInterviewerListAsync();
            CandidateActivities = (await _auditLogBusiness.GetCandidateActivityAsync(idInput)).ToList();

            return Page();
        }
        public virtual async Task<PartialViewResult> OnGetFormEdit(int id)

        {
            GetInfoUser();
            var resultView = new VS.Human.Rep.Model.Candidate()
            {
                Id = id,

                IsActive = 1,
                Phone = "",
                Status = 1,
                CVLink = "",

                Deleted = false,
                Noted = ""

            };

            if (id < 1)
            {
                return Partial("EditOrUpdateCandidate", resultView);
            }
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

        public async Task<IActionResult> OnPostDelete(int Id = -1)
        {
            GetInfoUser();
            var errors = new List<object>();
            ValidationHelper.ValidateIdForDelete(Id, errors);
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            if (!await _empBusiness.HasManageAccess(Id, UserData.UserId, UserData.RoleCode))
            {
                return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
            }

            var result = await _empBusiness.Delete(Id);
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }


        public async Task<IActionResult> OnPostReactive(int Id = -1)
        {
            GetInfoUser();
            var errors = new List<object>();
            ValidationHelper.ValidateIdForDelete(Id, errors);
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            if (!await _empBusiness.HasManageAccess(Id, UserData.UserId, UserData.RoleCode))
            {
                return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
            }

            var result = await _empBusiness.Delete(Id, true);
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }

        private async Task<BaseList> BuildInterviewerListAsync()
        {
            var interviewers = await _empBusiness1.GetByRoleCodes(new[] { "1", "3", "6", "8", "9" });
            return new BaseList
            {
                Total = interviewers.Count,
                Data = interviewers
                    .Where(x => x != null && x.Id > 0 && !string.IsNullOrWhiteSpace(x.FullName))
                    .OrderBy(x => x.FullName)
                    .ToList()
            };
        }



    }
}
