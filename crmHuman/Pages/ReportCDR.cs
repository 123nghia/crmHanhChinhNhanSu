using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace crmHuman.Pages
{
    [Authorize]
    public class ReportCDRModel : BaseModel2
    {
        private readonly ILogger<ReportCDRModel> _logger;
        private readonly IOrderBussiness _empBusiness;
        private readonly IReoportBussiness _reoportBussiness;
        private readonly IJobItemBusiness _jobItemBusiness;
        private readonly ICandidateBusiness _candidateBusiness;
        private readonly ImasterDataBussiness _masterDataBussiness;
        private readonly IPartnerBusiness _partnerBusiness;

        private readonly ImasterDataBussiness _imasterDataBussiness;

        public ReportCDInputRequest RequestSearch { get; set; }
        public BaseList DataAll { get; set; }

        public BaseList DataStatus { get; set; }


        public int TotalRecord
        {

            get
            {
                return DataAll.Total;

            }
        }
        public ReportCDRModel(ILogger<ReportCDRModel> logger,
            IOrderBussiness empBusiness,
            IJobItemBusiness jobItemBusiness,
            ICandidateBusiness candidateBusiness,
            IPartnerBusiness partnerBusiness,
            ImasterDataBussiness masterDataBussiness,
            IReoportBussiness reoportBussiness,
            ImasterDataBussiness imasterDataBussiness


            )
        {
            _logger = logger;
            _empBusiness = empBusiness;
            _jobItemBusiness = jobItemBusiness;
            TitlePage = "Danh sách file ghi âm";
            KeyPage = "ReportRecording";
            _candidateBusiness = candidateBusiness;
            _partnerBusiness = partnerBusiness;
            _masterDataBussiness = masterDataBussiness;
            _reoportBussiness = reoportBussiness;
            TableColumnText = new List<string>()
            {
                "STT","Mã đơn","Ngày","Giờ bắt đầu",
                "Giờ kết thúc","Talking-Time", "File ghi âm",
                "Line gọi","tên đăng nhập",
                "Ngày tạo"
            };
            _imasterDataBussiness = imasterDataBussiness;
        }


        public async Task<ActionResult> OnGet([FromQuery] ReportCDInputRequest request)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }
            GetInfoUser();
            RequestSearch = request;
            if (UserData.RoleCode == "2" || UserData.RoleCode == "4")
            {
                this.Permision.SearchGrop = false;
            }
            return await GetAll(request);
        }

        public async Task<ActionResult> GetAll(ReportCDInputRequest request2)
        {
            request2.UserId = UserData.UserId;
            var inputRequest = new ReportCDRequest();
            inputRequest.UserId = request2.UserId.ToString();
            inputRequest.From = request2.From;
            inputRequest.To = request2.To;
            inputRequest.Limit = request2.Limit;
            inputRequest.GroupId = request2.GroupId;
            inputRequest.MemberId = request2.MemberId;
            DataAll = await _reoportBussiness.GetAllRecordingFile(inputRequest);
            DataStatus = await _masterDataBussiness.GetAll(new CommonRequest()
            {
                Type = 4,
                UserId = UserData.UserId
            });
            return Page();
        }

        public virtual async Task<PartialViewResult> OnGetFormEdit(int id)

        {
            var resultView = new Order()
            {
                Id = id
            };
            if (id > 0)
            {
                resultView = await _empBusiness.GetById(id);
            }
            var allJob = await _jobItemBusiness.GetAll(new JobRequest()
            {
            });
            //var allCandidate = await _candidateBusiness.GetAll(new CandidateRequest()
            //{
            //});

            var AllStatus = await _masterDataBussiness.GetAll(new CommonRequest()
            {
                Type = 4
            });

            var allLinhvuc = await _masterDataBussiness.GetAll(new CommonRequest()
            {
                Type = 2
            });

            var allPartner = await _partnerBusiness.GetAll(new CommonRequest());
            var resultObject = new
            {
                allJob,
                //allCandidate,
                resultView,
                AllStatus,
                allLinhvuc,
                allPartner
            };
            return Partial("EditOrUpdateOrder", resultObject);
        }

        public async Task<IActionResult> OnPostDelete(int Id = -1)
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




    }
}
