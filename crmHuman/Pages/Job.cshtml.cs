using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace crmHuman.Pages
{
    [Authorize]
    public class JobModel : BaseModel2
    {
        private readonly ILogger<JobModel> _logger;
        private readonly IJobItemBusiness _empBusiness;
        private readonly ImasterDataBussiness _imasterDataBussiness;

        private readonly IPartnerBusiness _partnerBusiness;
        private readonly IParrentChildBussiness _parrentChildBussiness;
        public JobRequest RequestSearch { get; set; }
        public BaseList DataAll { get; set; }



        public int TotalRecord
        {

            get
            {
                return DataAll.Total;

            }
        }


        public JobModel(ILogger<JobModel> logger,
            IJobItemBusiness empBusiness,
            ImasterDataBussiness imasterDataBussiness,
            IPartnerBusiness partnerBusiness,
            IParrentChildBussiness parrentChildBussiness

            )
        {
            _logger = logger;
            _empBusiness = empBusiness;
            _imasterDataBussiness = imasterDataBussiness;
            _parrentChildBussiness = parrentChildBussiness;
            TitlePage = "Danh sách việc làm";
            KeyPage = "job";
            _partnerBusiness = partnerBusiness;

            TableColumnText = new List<string>()
            {
                "STT","Tiêu đề","Mã","Lĩnh vực","Số ngày BH", "Trạng thái","Ngày tạo","Cập nhật gần nhất","Thao tác"
            };


        }

        public async Task<IActionResult> OnPostAdd
            (JobItemAdd request)
        {
            var listEror = new List<object>();

            if (string.IsNullOrEmpty(request.Name))
            {
                var itemError = new
                {
                    name = "txtName",
                    Content = "Thiếu thông tin tiêu đề"
                };
                listEror.Add(itemError);

            }

            if (request.Field < 1)
            {
                var itemError = new
                {
                    name = "cbField",
                    Content = "Thiếu thông tin lĩnh vực"
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
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK

            };
        }

        public async Task<ActionResult> OnGet([FromQuery] JobRequest request)
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

        public async Task<ActionResult> GetAll(JobRequest request2)
        {
            RequestSearch = request2;
            request2.UserId = UserData.UserId;
            DataAll = await _empBusiness.GetAll(request2);
            return Page();
        }


        public async Task<ActionResult> OnGetAllJob(JobRequest2 request2)
        {

            request2.UserId = UserData.UserId;
            DataAll = await _empBusiness.GetAll2(request2);

            return new JsonResult(DataAll)
            {
                StatusCode = StatusCodes.Status200OK

            };
        }

        public async Task<ActionResult> OnGetAllData(JobRequest request)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }
            GetInfoUser();
            DataAll = await _empBusiness.GetAll(request);
            return new JsonResult(DataAll) { StatusCode = StatusCodes.Status200OK };
        }

        public virtual async Task<PartialViewResult> OnGetFormEdit(int id)

        {
            var resultView = new JobItem()
            {
                Id = id,
                Name = "",
                IsActive = 1,
                Status = 1,
                Deleted = false,
                Noted = ""
            };
            if (id > 0)
            {
                resultView = await _empBusiness.GetById(id);
            }


            var allFiled = await _imasterDataBussiness.GetAll(new CommonRequest()
            {
                Type = 2
            });

            var allCarrer = await _imasterDataBussiness.GetAll(new CommonRequest()
            {
                Type = 1
            });

            var allPartner = await _partnerBusiness.GetAll(new CommonRequest());

            var result = new { allCarrer, allFiled, resultView, allPartner };
            return Partial("EditOrUpdateJob", result);
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



        public virtual async Task<ActionResult> OnGetFullText(int id)

        {
            // var resultView = new JobItem()
            // {
            //     Id = id,
            //     Name = "",
            //     IsActive = 1,
            //     Status = 1,
            //     Deleted = false,
            //     Noted = ""
            // };
            // if (id > 0)
            // {
            //     resultView = await _empBusiness.GetById(id);
            // }

            var jobItem = await _empBusiness.GetById(id);

            var allFiled = await _imasterDataBussiness.GetAll(new CommonRequest()
            {
                Type = 2
            });

            var allCarrer = await _imasterDataBussiness.GetAll(new CommonRequest()
            {
                Type = 1
            });

            var allPartner = await _partnerBusiness.GetAll(new CommonRequest());

            var patnerText = "";
            foreach (var item in allPartner.Data)
            {
                var itemNeed = item as dynamic;
                if (itemNeed.Id == jobItem.PartnerId)
                {
                    patnerText = itemNeed.ShortName;
                    break;
                }
            }

            // var result = new { allCarrer, allFiled, resultView, allPartner };
            var allProject = await _parrentChildBussiness.GetAll(jobItem.PartnerId, 2);
            var projectText = "";
            foreach (var item in allProject.Data)
            {
                var itemNeed = item as dynamic;
                if (itemNeed.Id == jobItem.ProjectId)
                {
                    projectText = itemNeed.Text;
                    break;
                }
            }



            var linhvucText = "";
            foreach (var item in allFiled.Data)
            {
                var itemNeed = item as dynamic;
                if (itemNeed.Id == jobItem.Field)
                {
                    linhvucText = itemNeed.Name;
                    break;
                }
            }
            var pathFullText = "";

            if (!string.IsNullOrEmpty(linhvucText))
            {
                pathFullText += linhvucText + "_";
            }
            if (!string.IsNullOrEmpty(patnerText))
            {
                pathFullText += patnerText;
            }
            if (!string.IsNullOrEmpty(projectText))
            {
                pathFullText += "_" + projectText;
            }
            var partnerId = jobItem.PartnerId;



            var reponse = new
            {
                pathFullText,
                partnerId
            };


            return new JsonResult(reponse)
            {
                StatusCode = StatusCodes.Status200OK

            };


        }




    }
}
