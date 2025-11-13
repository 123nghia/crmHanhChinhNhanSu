using crmHuman.DisplayModel;
using crmHuman.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        private readonly IEmpBusiness _empBusiness1;
        public List<string> TableColumnTextAdmin { get; set; }
        public CandidateRequest RequestSearch { get; set; }
        public BaseList DataAll { get; set; }

        public BaseList DataMasterData { get; set; }

        public BaseList DataPostion { get; set; }

        public BaseList DataLead { get; set; }



        public BaseList DataHistory { get; set; }

        public CandidateDisplayEdit ResultModel { get; set; }

        public List<DataMasterItem> DataDepartment { get; set; }


        public BaseList DataFile { get; set; }

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
            _empBusiness1 = empBusiness1;
        }

        public async Task<IActionResult> OnPostAddSchedule(CandidateScheduleAdd request)
        {
            var errors = new List<object>();
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            var itemInsert = new ScheduleInterviewAdd()
            {
                AddressInfo = request.AddressInfo,
                ScheduleDate = request.ScheduleDate,
                Noted = request.Noted,
                RelId = request.RelId,
            };

            var result = await _scheduleInterviewBussiness.AddOrUpdate(itemInsert);
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }

        public async Task<IActionResult> OnPostUpdate(CandidateDetailUpdate request)
        {
            var errors = new List<object>();
            ValidationHelper.ValidateId(request.CandidateId, "txtFullName", "đối tượng Id", errors);
            ValidationHelper.ValidatePhone(request.Phone, errors);
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            request.Id = request.CandidateId;
            var result = await _empBusiness.Update(request);
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }


        public async Task<IActionResult> OnPostAddDocument(DocumentDataAddRequest request)
        {
            var errors = new List<object>();
            ValidationHelper.ValidateId(request.RelId, "txtFullName", "đối tượng Id", errors);
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
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
                ManagerId = candidateInfo.ManagerId

            };
            ResultModel = resultView;
            var dataAllHistory = await _scheduleInterviewBussiness.GetAll(new ScheduleInterviewRquest()
            {
                RelId = idInput,
                Type = 0
            });
            DataHistory = dataAllHistory;

            DataFile = await _documentDataBussiness.GetAll(new DocumentDataRquest()
            {
                DataType = 1,
                RelId = idInput

            });
            DataLead = await _empBusiness1.GetAllManager();

            return Page();
        }
        public virtual async Task<PartialViewResult> OnGetFormEdit(int id)

        {
            GetInfoUser();
            var resultView = new Candidate()
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
