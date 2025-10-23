using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Item;

namespace crmHuman.Pages
{
    [Authorize]
    public class OrderListDataModel : BaseModel2
    {
        private readonly ILogger<OrderListDataModel> _logger;
        private readonly IOrderBussiness _orderBussiness;
        private readonly IJobItemBusiness _jobItemBusiness;
        private readonly ICandidateBusiness _candidateBusiness;
        private readonly ImasterDataBussiness _masterDataBussiness;
        private readonly IPartnerBusiness _partnerBusiness;
        public OrderReportRequest RequestSearch { get; set; }
        public BaseList DataAll { get; set; }
        public BaseList JobList { get; set; }



        public int TotalRecord
        {

            get
            {
                return DataAll.Total;

            }
        }
        public OrderListDataModel(ILogger<OrderListDataModel> logger,
            IOrderBussiness empBusiness,
            IJobItemBusiness jobItemBusiness,
            ICandidateBusiness candidateBusiness,
            IPartnerBusiness partnerBusiness,
            ImasterDataBussiness masterDataBussiness
            )
        {
            _logger = logger;
            _orderBussiness = empBusiness;
            _jobItemBusiness = jobItemBusiness;
            TitlePage = "Danh sách đơn hàng";
            KeyPage = "Order";
            _candidateBusiness = candidateBusiness;
            _partnerBusiness = partnerBusiness;
            _masterDataBussiness = masterDataBussiness;

            TableColumnText = new List<string>()
            {
                "STT","Mã","Ứng cử viên","Vị trí",
                "Đối tác/dự án","Trạng thái","Người tạo",
                "Xử lý bởi",
                "Ngày nhận",
                "Cập nhật gần nhất","Thao tác"
            };
        }













        public async Task<ActionResult> GetAll(OrderReportRequest request2)
        {
            request2.UserId = UserData.UserId;
            request2.Marketting = 0;
            request2.RoleCode = UserData.RoleCode;
            if (request2.RoleCode == "4")
            {
                request2.Tracking = true;
                this.Permision.SearchGrop = false;
            }
            if (UserData.RoleCode == "2" || UserData.RoleCode == "4")
            {
                this.Permision.SearchGrop = false;
            }



            if (UserData.RoleCode == "3")
            {

                if (UserData.UserId == 37)
                {
                    request2.GroupId = 6;


                }
                else if (UserData.UserId == 38)
                {
                    request2.GroupId = 7;

                }
            }

            RequestSearch = request2;

            JobList = await _jobItemBusiness.GetAll(new JobRequest());
            DataAll = await _orderBussiness.GetAllOrderReport(request2);
            return Page();
        }

        public async Task<ActionResult> OnGet([FromQuery] OrderReportRequest request)
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


    }
}
