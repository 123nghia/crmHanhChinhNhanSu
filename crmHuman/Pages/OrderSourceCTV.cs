using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace crmHuman.Pages
{
    [Authorize]
    public class OrderSourceCTVModel : BaseModel2
    {
        private readonly ILogger<OrderSourceCTVModel> _logger;
        private readonly IOrderBussiness _empBusiness;

        private readonly IEmpBusiness _empBusiness2;
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
        public OrderSourceCTVModel(ILogger<OrderSourceCTVModel> logger,
            IOrderBussiness empBusiness,
            IJobItemBusiness jobItemBusiness,
            ICandidateBusiness candidateBusiness,
            IPartnerBusiness partnerBusiness,
            ImasterDataBussiness masterDataBussiness,
            IEmpBusiness empBusiness2
            )
        {
            _logger = logger;
            _empBusiness = empBusiness;
            _jobItemBusiness = jobItemBusiness;
            TitlePage = "Danh sách đơn hàng";
            KeyPage = "Order";
            _candidateBusiness = candidateBusiness;
            _partnerBusiness = partnerBusiness;
            _masterDataBussiness = masterDataBussiness;
            TableColumnText = new List<string>()
            {
                "STT","Mã","Ứng  cử viên","Vị trí",
                "Đối tác/dự án","Trạng thái","Nhân viên xử lý ", "Ngày nhận","Người tạo","Ngày tạo",
                "Cập nhật gần nhất","Thao tác"
            };
            _empBusiness2 = empBusiness2;


        }

        public async Task<IActionResult> OnPostAssingee(OrderAssingeeAdd request)
        {
            var listEror = new List<object>();
            if (string.IsNullOrEmpty(request.Assignee))
            {
                var itemError = new
                {
                    name = "cbAssingeeId",
                    Content = "Thiếu thông tin TC"
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
            var result = await _empBusiness.Assignee(request);
            var dataReponse = new
            {
                success = result,

            };
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK

            };
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
                return Redirect("/");
            }

            if (UserData.RoleCode != "4")
            {
                this.Permision.Assignee = true;
            }
            return await GetAll(request);
        }

        public async Task<ActionResult> GetAll(OrderRequest request2)
        {
            RequestSearch = request2;
            request2.UserId = UserData.UserId;
            request2.Marketting = 1;
            request2.Marketting = 0;
            request2.CTV = true;
            request2.RoleCode = UserData.RoleCode;
            DataAll = await _empBusiness.GetAll(request2);
            JobList = await _jobItemBusiness.GetAll(new JobRequest());
            return Page();
        }

        public virtual async Task<PartialViewResult> OnGetOpenForm(int id)

        {
            GetInfoUser();
            var useridReuest = UserData.UserId;
            var RoleCode = UserData.RoleCode;
            var resultView = new Order()
            {
                Id = id
            };
            if (id > 0)
            {
                resultView = await _empBusiness.GetById(id);
            }
            var allEmployers = await _empBusiness2.GetAll(new EmployeeRequest()
            {
                UserId = useridReuest
            });
            var resultObject = new
            {
                allEmployers,
                resultView

            };
            return Partial("OpenAssignee", resultObject);
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




    }
}
