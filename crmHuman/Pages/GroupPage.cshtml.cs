using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace crmHuman.Pages
{
    [Authorize]
    public class GroupPageModel : BaseModel2
    {
        private readonly ILogger<GroupPageModel> _logger;
        private readonly IGroupBusiness _business;
        public GroupRequest RequestSearch { get; set; }
        public BaseList DataAll { get; set; }

        public BaseList ManagerList { get; set; }



        public int TotalRecord
        {

            get
            {
                return DataAll.Total;

            }
        }
        public GroupPageModel(ILogger<GroupPageModel> logger,
            IGroupBusiness business
            )
        {
            _logger = logger;
            _business = business;
            TitlePage = "Danh sách nhóm";
            KeyPage = "GroupPage";
            TableColumnText = new List<string>()
            {
                "STT","Mã nhóm","Tên nhóm","Tên người quản lý","Trạng thái","Ngày tạo","Cập nhật gần nhất","Thao tác"
            };


        }

        public async Task<IActionResult> OnPostAdd
            (GroupAdd request)
        {
            var listEror = new List<object>();

            if (string.IsNullOrEmpty(request.Name))
            {
                var itemError = new
                {
                    name = "txtName",
                    Content = "Thiếu thông tin tên nhóm"
                };
                listEror.Add(itemError);

            }

            if (string.IsNullOrEmpty(request.ManagerId))
            {
                var itemError = new
                {
                    name = "txtPhone",
                    Content = "Thiếu thông tin trưởng nhóm"
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

            var itemgroup = new GroupItem()
            {

                ManagerId = request.ManagerId,
                Name = request.Name,
                CreateAt = DateTime.Now,
                Code = request.Code,
                CreatedBy = UserData.UserId,
                IsActive = request.IsActive,
                Status = request.IsActive,
                Id = request.Id,
                UpdatedBy = request.UpdatedBy
            };
            var result = await _business.AddOrUpdate(itemgroup);

            var dataReponse = new
            {
                success = result,

            };
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK

            };
        }


        public async Task<IActionResult> OnPostAddMember
           (GroupMemberAdd request)
        {
            var listEror = new List<object>();

            if (!request.GroupId.HasValue)
            {
                var itemError = new
                {
                    name = "txtName",
                    Content = "Thiếu thông tin nhóm"
                };
                listEror.Add(itemError);

            }

            if (!request.MemberId.HasValue)
            {
                var itemError = new
                {
                    name = "cbmemberId",
                    Content = "Thiếu thông tin thành viên"
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

            var itemMember = new GroupMember()
            {
                MemberId = request.MemberId,
                GroupId = request.GroupId,
                CreatedBy = UserData.UserId,
                Id = request.Id
            };
            var result = await _business.AddOrUpdateMember(itemMember);

            var dataReponse = new
            {
                success = result,

            };
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK

            };
        }


        public async Task<ActionResult> OnGet([FromQuery] GroupRequest request)
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
            ManagerList = await _business.GetAllManager();


            return await GetAll(request);

        }


        public async Task<JsonResult> OnGetAllGroup([FromQuery] GroupRequest request)
        {

            var dataGroup = await _business.GetAll(request);
            request.UserId = UserData.UserId;
            return new JsonResult(dataGroup);


        }
        public async Task<JsonResult> OnGetListMember(int groupId)

        {
            var allMemeber = await _business.GetAllMember(groupId);
            return new JsonResult(allMemeber);
        }
        public async Task<ActionResult> GetAll(GroupRequest request2)
        {
            RequestSearch = request2;
            request2.UserId = UserData.UserId;
            DataAll = await _business.GetAll(request2);
            return Page();
        }

        public virtual async Task<PartialViewResult> OnGetFormEdit(int id)

        {
            var listManager = new BaseList();
            var resultView = new GroupItem()
            {
                Id = id,


            };
            if (id > 0)
            {
                resultView = await _business.GetById(id);
                var leadOfGroup = -1;
                if (!string.IsNullOrEmpty(resultView.ManagerId))
                {
                    leadOfGroup = int.Parse(resultView.ManagerId);
                }
                listManager = await _business.GetAllManager(leadOfGroup);
            }
            else
            {
                listManager = await _business.GetAllManager();
            }

            resultView.ManagerList = listManager.Data;

            return Partial("editOrUpdateGroup", resultView);
        }


        public virtual async Task<PartialViewResult> OnGetFormMember(int id)

        {
            var listManager = new BaseList();
            var resultView = new GroupItem()
            {
                Id = id,


            };
            resultView = await _business.GetById(id);
            var allMemeber = await _business.GetAllMember(resultView.Id);
            resultView.ListMember = allMemeber.Data;
            var allMemberNotGroup = await _business.GetAllMemberNotGroup();
            resultView.ListMeberNotGroup = allMemberNotGroup.Data;
            return Partial("EditOrUpdateMember", resultView);
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

            result = await _business.Delete(Id);
            var dataReponse = new
            {
                success = result,

            };
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK

            };
        }

        public async Task<IActionResult> OnPostDeleteMember
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

            result = await _business.DeleteMember(Id);
            var dataReponse = new
            {
                success = result,

            };
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK

            };
        }


        public virtual async Task<PartialViewResult> OnGetListDataMember(int groupId)

        {
            var listManager = new BaseList();
            var resultView = new GroupItem()
            {
                Id = -1,


            };
            var allMemeber = await _business.GetAllMember(groupId);
            return Partial("DispalyDataMember", allMemeber.Data);
        }


    }
}
