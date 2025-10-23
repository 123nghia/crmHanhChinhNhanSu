using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Item;

namespace crmHuman.Pages
{
    [Authorize]
    public class LinhvucModel : BaseModel2
    {
        private readonly ILogger<LinhvucModel> _logger;
        private readonly ImasterDataBussiness _empBusiness;
        public CommonRequest RequestSearch { get; set; }
        public BaseList DataAll { get; set; }



        public int TotalRecord
        {

            get
            {
                return DataAll.Total;

            }
        }
        public LinhvucModel(ILogger<LinhvucModel> logger,
            ImasterDataBussiness empBusiness
            )
        {
            _logger = logger;
            _empBusiness = empBusiness;
            TitlePage = "Danh sách lĩnh vực";
            KeyPage = "linhvuc";
            TableColumnText = new List<string>()
            {
                "STT","Mã","Tên","Trạng thái","Ngày tạo","Cập nhật gần nhất","Thao tác"
            };
            NameController = "Linhvuc";
            TitleList = "Lĩnh vực";


        }

        public async Task<IActionResult> OnPostAdd
            (MasterDataAdd request)
        {
            var listEror = new List<object>();

            if (string.IsNullOrEmpty(request.Name))
            {
                var itemError = new
                {
                    name = "txtFullName",
                    Content = "Thiếu thông tin họ và tên"
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
            request.TypeData = 2;

            var result = await _empBusiness.AddOrUpdate(request);
            var dataReponse = new
            {
                success = result,

            };
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK

            };
        }

        public async Task<ActionResult> OnGet([FromQuery] CommonRequest request)
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

        public async Task<ActionResult> GetAll(CommonRequest request2)
        {

            RequestSearch = request2;
            request2.UserId = UserData.UserId;
            request2.Type = 2;
            DataAll = await _empBusiness.GetAll(request2);
            return Page();
        }

        public virtual async Task<PartialViewResult> OnGetFormEdit(int id = -1)

        {
            var resultView = new VS.Human.Rep.Model.MasterData()
            {
                IsActive = 1,
                Id = -1
            };
            if (id > 0)
            {
                resultView = await _empBusiness.GetById(id);

            }
            return Partial("MasterData/EditMasterData", resultView);
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

            var result = await _empBusiness.Delete(Id);
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
