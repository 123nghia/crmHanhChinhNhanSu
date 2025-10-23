using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Item;

namespace crmHuman.Pages
{
    [Authorize]
    public class CommonModel : BaseModel2
    {
        private readonly ILogger<CommonModel> _logger;
        private readonly IParrentChildBussiness _empBusiness;
        public CommonRequest RequestSearch { get; set; }
        public BaseList DataAll { get; set; }
        public int TotalRecord
        {

            get
            {
                return DataAll.Total;

            }
        }
        public CommonModel(ILogger<CommonModel> logger,
            IParrentChildBussiness empBusiness
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


        public async Task<ActionResult> OnGetGetAllParrentChild(int? idRel, int? type)
        {
            type = 2;
            DataAll = await _empBusiness.GetAll(idRel, type);
            return new JsonResult(DataAll) { StatusCode = StatusCodes.Status200OK };
        }
        public async Task<ActionResult> OnGetGetAllParrentAddress(int? idRel, int? type)
        {
            type = 1;
            DataAll = await _empBusiness.GetAll(idRel, type);
            return new JsonResult(DataAll) { StatusCode = StatusCodes.Status200OK };
        }






    }
}
