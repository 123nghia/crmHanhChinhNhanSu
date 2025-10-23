using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace crmHuman.Pages
{
    [Authorize]
    public class OrderOnboardModel : BaseModel2
    {
        private readonly ILogger<OrderOnboardModel> _logger;
        private readonly IOrderBussiness _empBusiness;

        private readonly IOnboardMemberBusiness _onboardMemberBusiness;

        private readonly IJobItemBusiness _jobItemBusiness;
        private readonly ICandidateBusiness _candidateBusiness;
        private readonly ImasterDataBussiness _masterDataBussiness;
        private readonly IPartnerBusiness _partnerBusiness;
        public OrderRequest RequestSearch { get; set; }
        public BaseList DataAll { get; set; }
        public BaseList JobList { get; set; }

        public int TotalRecord
        {
            get
            {
                return DataAll.Total;

            }
        }
        public OrderOnboardModel(ILogger<OrderOnboardModel> logger,
            IOrderBussiness empBusiness,
            IJobItemBusiness jobItemBusiness,
            ICandidateBusiness candidateBusiness,
            IPartnerBusiness partnerBusiness,
            ImasterDataBussiness masterDataBussiness,
            IOnboardMemberBusiness onboardMemberBusiness
            )
        {
            _logger = logger;
            _empBusiness = empBusiness;
            _jobItemBusiness = jobItemBusiness;
            TitlePage = "Danh sách onboard";
            KeyPage = "Order";
            _candidateBusiness = candidateBusiness;
            _partnerBusiness = partnerBusiness;
            _masterDataBussiness = masterDataBussiness;

            TableColumnText = new List<string>()
            {
                "STT",
                "Code",
                "Họ tên","Onboard","Vị trí",
                 "Trạng thái",
                "hệ thống",
                "Ngày tạo",
                "DPD",
                "Hết hạn BH",

                "Xử lý bởi",
                 "Kết quả"
            };
            _onboardMemberBusiness = onboardMemberBusiness;
        }

        public async Task<ActionResult> OnGet([FromQuery] OrderRequest request)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }
            GetInfoUser();
            if (UserData.RoleCode == "2")
            {
                this.Permision.SearchGrop = false;
            }
            return await GetAll(request);
        }





        public async Task<ActionResult> GetAll(OrderRequest request2)
        {


            RequestSearch = request2;
            request2.UserId = UserData.UserId;
            request2.Marketting = 0;
            request2.RoleCode = UserData.RoleCode;
            if (request2.RoleCode == "4")
            {
                request2.Tracking = true;
            }
            JobList = await _jobItemBusiness.GetAll(new JobRequest());
            DataAll = await _onboardMemberBusiness.GetAll(request2);
            return Page();
        }


        public virtual async Task<PartialViewResult> OnGetFormEdit(int id)

        {
            var resultView = new OnboardMember()
            {
                Id = id
            };
            resultView = await _onboardMemberBusiness.GetById(id);
            return Partial("EditOrUpdateOnboard", resultView);
        }

        public async Task<IActionResult> OnPostUpdate(OnboardMemberUpdate request)
        {
            var listEror = new List<object>();
            if (request.OrderId < 1)
            {
                var itemError = new
                {

                    name = "cbcandidateId",
                    Content = "Thiếu thông tin ứng cử viên"
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
            var requestUpdate = new
            {
                CreatedBy = -1,
                id = request.OrderId,
                result = request.Result,
                OnboardDate = request.DateOnboard
            };

            var result =
                await _onboardMemberBusiness.UpdateOnboard(requestUpdate);
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
