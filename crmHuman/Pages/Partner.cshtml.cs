using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Item;

namespace crmHuman.Pages
{
    [Authorize]
    public class PartnerModel : BaseModel2
    {
        private readonly ILogger<PartnerModel> _logger;
        private readonly IPartnerBusiness _empBusiness;

        private readonly IParrentChildBussiness _parrentChild;
        public CommonRequest RequestSearch { get; set; }
        public BaseList DataAll { get; set; }



        public int TotalRecord
        {

            get
            {
                return DataAll.Total;

            }
        }
        public PartnerModel(ILogger<PartnerModel> logger,
            IPartnerBusiness empBusiness,
            IParrentChildBussiness parrentChild
            )
        {
            _logger = logger;
            _empBusiness = empBusiness;
            TitlePage = "Danh sách đối tác";
            KeyPage = "Partner";
            TableColumnText = new List<string>()
            {
                "STT","Tên","Mã","Trạng thái","Ngày tạo","Cập nhật gần nhất","Thao tác"
            };
            NameController = "Partner";
            TitleList = "Đối tác";
            _parrentChild = parrentChild;


        }

        public async Task<IActionResult> OnPostAdd
            (PartnerAdd request)
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

            var result = await _empBusiness.AddOrUpdate(request);
            var dataReponse = new
            {
                success = result,

            };

            if (request.Id > 0)
            {
                var itemAddress = request.AddressList;
                var itemProject = request.ProjectList;

                if (itemAddress == null)
                {
                    itemAddress = new List<ParrentChildAdd>();
                }
                if (itemProject == null)
                {
                    itemProject = new List<ParrentChildAdd>();
                }
                foreach (var item in itemAddress)
                {
                    var itemInsert = new VS.Human.Rep.Model.ParrentChild()
                    {
                        RelId = request.Id,
                        Type = 1,
                        Text = item.Text,
                        Id = item.Id
                    };
                    await _parrentChild.AddOrUpdate(itemInsert);
                }
                foreach (var item in itemProject)
                {
                    var itemInsert = new VS.Human.Rep.Model.ParrentChild()
                    {
                        RelId = request.Id,
                        Type = 2,
                        Text = item.Text,
                        Id = item.Id
                    };
                    await _parrentChild.AddOrUpdate(itemInsert);
                }

            }
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

        public async Task<ActionResult> OnGetAllData([FromQuery] CommonRequest request)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }
            GetInfoUser();
            DataAll = await _empBusiness.GetAll(request);
            return new JsonResult(DataAll) { StatusCode = StatusCodes.Status200OK };
        }

        public async Task<ActionResult> GetAll(CommonRequest request2)
        {

            RequestSearch = request2;
            request2.UserId = UserData.UserId;
            DataAll = await _empBusiness.GetAll(request2);
            return Page();
        }

        public virtual async Task<PartialViewResult> OnGetFormEdit(int id = -1)

        {
            var resultView = new VS.Human.Rep.Model.Partner()
            {
                IsActive = 1,
                Id = -1
            };
            var listAddress = new BaseList();
            var listProject = new BaseList();
            if (id > 0)
            {
                resultView = await _empBusiness.GetById(id);
                listAddress = await _parrentChild.GetAll(resultView.Id, 1);

                listProject = await _parrentChild.GetAll(resultView.Id, 2);

            }
            var infoMation = new
            {
                info = resultView,
                listAddress,
                listProject

            };
            return Partial("Partner/EditOrUpdatePartner", infoMation);
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
