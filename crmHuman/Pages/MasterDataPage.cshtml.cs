using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Item;

namespace crmHuman.Pages
{
    [Authorize]
    public class MasterDataPageModel : BaseModel2
    {
        private readonly ILogger<MasterDataPageModel> _logger;
        private readonly ImasterDataBussiness _business;
        public CommonRequest RequestSearch { get; set; }
        public BaseList DataAll { get; set; }
        public int? TypePage { get; set; }
        public string? TypePageText
        {
            get
            {
                switch (TypePage)
                {
                    case 1:
                        return "loai-hdld";
                    case 2:
                        return "vi-tri-viec-lam";
                    case 3:
                        return "quan-he-nguoi-than";
                    case 5:
                        return "bo-phan";

                    case 7:
                        return "Loai-chung-tu";
                    case 9:
                        return "trang-thai-ung-vien";

                    case 10:
                        return "loai-chung-tu-nhan-vien";
                    case 8:
                        return "trang-thai-chung-tu";
                     case 11:
                        return "trang-thai-lam-viec";
                     case 13:
                        return "trang-thai-hon-nhan";
                     case 14:
                        return "trinh-do-hoc-van";
                    default:
                        return "";
                }
            }
        }

       

        public int TotalRecord
        {

            get
            {
                return DataAll.Total;
            }
        }
        public MasterDataPageModel(ILogger<MasterDataPageModel> logger,
            ImasterDataBussiness empBusiness
            )
        {
            _logger = logger;
            _business = empBusiness;
            TitlePage = "Danh sách";
            KeyPage = "statusPage";
            TableColumnText = new List<string>()
            {
                "STT","Mã","Tên","Trạng thái","Đối tượng", "Ngày tạo","Cập nhật gần nhất","Thao tác"
            };
            NameController = "MasterDataPage";
            TitleList = "Trạng thái";
        }

        public async Task<IActionResult> OnPostAdd
            (MasterData2Add request)
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
            var result = await _business.AddOrUpdate(new MasterDataAdd()
            {
                TypeData = request.TypeData,
                Name = request.Name,
                Id = request.Id,
                ApplyFor = request.ApplyFor,

                IsActive = request.IsActive,
                Noted = request.Noted,

            });
            var dataReponse = new
            {
                success = result,

            };
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK

            };
        }

        public async Task<ActionResult> OnGet([FromQuery] MasterRequestInput request)
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


            var inputRequest = new CommonRequest();
            inputRequest.Token = request.Token;
            inputRequest.Limit = request.Limit;
            inputRequest.Page = request.Page;
            inputRequest.Status = request.Status;
            inputRequest.UserId = request.UserId;
            inputRequest.ApplyFor = request.ApplyFor;
            switch (request.Type)
            {
                case "loai-hdld":
                    inputRequest.Type = 1;
                    break;
                case "vi-tri-viec-lam":
                    inputRequest.Type = 2;
                    break;
                case "quan-he-nguoi-than":
                    inputRequest.Type = 3;
                    break;
                case "bo-phan":
                    inputRequest.Type = 5;
                    break;

                case "trang-thai-ung-vien":
                    inputRequest.Type = 9;
                    break;
                case "trang-thai-chung-tu":
                    inputRequest.Type = 8;
                    break;
                case "Loai-chung-tu":
                    inputRequest.Type = 7;
                    break;
                case "loai-chung-tu-nhan-vien":
                    inputRequest.Type = 10;
                    break;
                case "trang-thai-lam-viec":
                    inputRequest.Type = 11;
                    break;
                 case "trang-thai-hon-nhan":
                    inputRequest.Type = 13;
                    break;
                 case "trinh-do-hoc-van":
                    inputRequest.Type = 14;
                    break;
                default:
                    inputRequest.Type = -1;
                    break;
            }
            return await GetAll(inputRequest);
        }

        public async Task<ActionResult> GetAll(CommonRequest request2)
        {
            RequestSearch = request2;
            request2.UserId = UserData.UserId;
            DataAll = await _business.GetAll(request2);
            TypePage = request2.Type;
            return Page();
        }



        public async Task<ActionResult> OnGetAllData(CommonRequest request2)
        {
            RequestSearch = request2;
            request2.UserId = UserData.UserId;
            request2.Type = 4;
            DataAll = await _business.GetAll(request2);
            return new JsonResult(DataAll) { StatusCode = StatusCodes.Status200OK };
        }
        public virtual async Task<PartialViewResult> OnGetFormEdit(int id = -1, int typeData = -1)

        {
            var resultView = new VS.Human.Rep.Model.MasterData()
            {
                IsActive = 1,
                Id = -1,
                Extra = "red",
                TypeData = typeData
            };
            if (id > 0)
            {
                resultView = await _business.GetById(id);
            }
            return Partial("MasterData/EditMasterData2", resultView);
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

            var result = await _business.Delete(Id);
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
