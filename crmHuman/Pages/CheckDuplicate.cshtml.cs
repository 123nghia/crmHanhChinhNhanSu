using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Item;

namespace crmHuman.Pages
{
    [Authorize]
    public class CheckDuplicateModel : BaseModel2
    {
        private readonly ILogger<CheckDuplicateModel> _logger;
        private readonly IOrderBussiness _business;
        public CheckDuplicateRequest RequestSearch { get; set; }
        public BaseList DataAll { get; set; }
        public int TotalRecord
        {
            get
            {
                return DataAll.Total;

            }
        }
        public CheckDuplicateModel(ILogger<CheckDuplicateModel> logger, IOrderBussiness business)
        {
            TitlePage = "Danh sách ứng cử viên";
            KeyPage = "candidate";
            _logger = logger;
            _business = business;
            TableColumnText = new List<string>()
             {
                    "STT",
                    "Tên ứng cử viên",
                    "Mã",
                    "Số điện thoại",
                    "Email",
                    "Trạng thái",
                      "Người tạo",
                     "Ngày tạo",
                    "Cập nhật gần nhất",

                    "Thao tác"
            };
        }
        public async Task<ActionResult> OnGet([FromQuery] CheckDuplicateRequest request)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }
            GetInfoUser();
            RequestSearch = request;


            DataAll = await _business.GetALlHistory(new OrderRequest()
            {
                Email = request.KeyCheck,
                Phone = request.KeyCheck

            });
            return Page();
        }

    }
}
