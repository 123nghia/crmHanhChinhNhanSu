using crmHuman.DisplayModel;
using crmHuman.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace crmHuman.Pages
{
    [Authorize]
    public class CandidateModel : BaseModel2
    {
        private readonly ILogger<CandidateModel> _logger;
        private readonly ICandidateBusiness _empBusiness;

        private readonly IEmpBusiness _iempl;
        public List<string> TableColumnTextAdmin { get; set; }
        public CandidateRequest RequestSearch { get; set; }
        public BaseList DataAll { get; set; }
        public List<DataMasterItem> DataMasterData { get; set; }
        private readonly ImasterDataBussiness _masterDataBussiness;
        private readonly INotificationBusiness _notificationBusiness;
        public BaseList DataManager { get; set; }
        public int TotalRecord
        {

            get
            {
                return DataAll.Total;

            }
        }
        public CandidateModel(ILogger<CandidateModel> logger,
            ICandidateBusiness empBusiness,
            IEmpBusiness empBusiness1,
            ImasterDataBussiness imasterDataBussiness,
            INotificationBusiness notificationBusiness
            )
        {
            _logger = logger;
            _empBusiness = empBusiness;
            _masterDataBussiness = imasterDataBussiness;
            _notificationBusiness = notificationBusiness;
            TitlePage = "Danh sách ứng viên";
            KeyPage = "Candidate";
            _iempl = empBusiness1;
            TableColumnText = new List<string>()
            {
                "STT","UserName","Họ tên", "Vị trí", "Phòng ban",
                "Người quản lý",
                "Trạng thái",
                "Trạng thái chứng từ"
               ,"Cập nhật gần nhất", "Người tạo","Thao tác"
            };

            TableColumnTextAdmin = new List<string>()
            {
                "STT","UserName","Họ tên", "Vị trí", "Phòng ban",
                 "Người quản lý","Trạng thái", "Trạng thái chứng từ",
                "Cập nhật gần nhất","Người tạo","Thao tác"
            };
            DataMasterData = new List<DataMasterItem>();

        }

        public async Task<IActionResult> OnPostAdd
            (CandidateAdd request)
        {
            GetInfoUser();
            var canEdit = (Permision != null && (Permision.Add == true || Permision.Edit == true)) || CanManageRecruitmentData();
            if (!canEdit)
            {
                return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
            }

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

            if (string.IsNullOrEmpty(request.Phone))
            {
                var itemError = new
                {
                    name = "txtPhone",
                    Content = "Thiếu thông tin số điện thoại"
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

            if (request.Id < 0)
            {
                var duplicateCandidate = await _empBusiness.FindDuplicateForCreate(request);
                if (duplicateCandidate != null && duplicateCandidate.Id > 0)
                {
                    return ApiResponseHelper.BadRequest("txtPhone", "Ứng viên đã tồn tại trong hệ thống.");
                }
            }

            bool result = false;
            if (request.Id < 0)
            {
                request.Status = 91;
                result = await _empBusiness.Add(request);
                if (!result)
                {
                    return ApiResponseHelper.BadRequest("txtPhone", "Không thể lưu ứng viên. Vui lòng thử lại.");
                }
                if (result)
                {
                    // Get the newly created candidate to find their ID
                    var candidates = await _empBusiness.GetAll(new CandidateRequest
                    {
                        Token = request.Phone,
                        UserId = UserData.UserId,
                        RoleCode = UserData.RoleCode,
                        Page = 1,
                        Limit = 1
                    });
                    if (candidates?.Data != null)
                    {
                        foreach (var c in candidates.Data)
                        {
                            var cand = c as dynamic;
                            if (cand != null && cand.Id > 0)
                            {
                                await _notificationBusiness.CreateNotification(
                                    cand.Id,
                                    "Chào mừng bạn! Tài khoản ứng viên của bạn đã được tạo thành công.",
                                    "/Candidate/Dashboard",
                                    "CandidateWelcome"
                                );
                                break;
                            }
                        }
                    }
                }

            }
            else
            {
                //result = await _empBusiness.Update(request);
            }
            var dataReponse = new
            {
                success = result,
            };
            return ApiResponseHelper.SuccessResponse(dataReponse);
        }



        public async Task<ActionResult> OnGet([FromQuery] CandidateRequest request)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }
            GetInfoUser();
            if (UserData.RoleCode == "CANDIDATE")
            {
                return Redirect("/Candidate/Dashboard");
            }
            var temp = await _masterDataBussiness.GetAll(new CommonRequest()
            {

            });
            foreach (var item in temp.Data)
            {

                var tempItem = item as dynamic;

                var itemInsert = new DataMasterItem()
                {
                    Name = tempItem.Name,
                    TypeData = tempItem.TypeData,
                    Code = tempItem.Code,
                    ApplyFor = tempItem.ApplyFor,
                    IsActive = tempItem.IsActive
                };
                DataMasterData.Add(itemInsert);
            }
            DataManager = await _iempl.GetAllManager();
            return await GetAll(request);
        }

        public async Task<ActionResult> GetAll(CandidateRequest request2)
        {
            RequestSearch = request2;
            request2.UserId = UserData.UserId;
            request2.RoleCode = UserData.RoleCode;
            if (UserData.RoleCode == "1")
            {
                request2.IsDeleted = true;
            }
            else
            {

            }
            {
                request2.IsDeleted = false;
            }

            DataAll = await _empBusiness.GetAll(request2);

            if (UserData.RoleCode == "6")
            {
                TableColumnText = new List<string>()
                    {
                        "STT","Họ tên","Tài khoản","Vai trò","Nhóm", "Ngày Onboard","Cập nhật gần nhất","Thao tác"
                    };

                TableColumnTextAdmin = new List<string>()
                    {
                        "STT","Họ tên","Tài khoản","Vai trò","Nhóm","Trạng thái", "Ngày Onboard","Cập nhật gần nhất","Thao tác"
                    };
            }
            return Page();
        }

        public virtual async Task<PartialViewResult> OnGetFormEdit(int id)

        {
            GetInfoUser();
            var resultView = new VS.Human.Rep.Model.Candidate()
            {
                Id = id,

                IsActive = 1,
                Phone = "",
                Status = 1,
                CVLink = "",

                Deleted = false,
                Noted = ""

            };

            var temp = await _masterDataBussiness.GetAll(new CommonRequest()
            {

            });

            foreach (var item in temp.Data)
            {

                var tempItem = item as dynamic;

                var itemInsert = new DataMasterItem()
                {
                    Name = tempItem.Name,
                    TypeData = tempItem.TypeData,
                    Code = tempItem.Code,
                    IsActive = tempItem.IsActive
                };
                DataMasterData.Add(itemInsert);
            }

            DataManager = await _iempl.GetAllManager();

            var resultModel = new
            {
                DataManager,
                resultView,
                DataMasterData
            };


            if (id < 1)
            {
                return Partial("EditOrUpdateCandidate", resultModel);
            }
            if (id > 0)
            {
                resultView = await _empBusiness.GetById(id);
            }
            var religionOptions = await _masterDataBussiness.GetallByTypeData(20);
            ViewData["ReligionOptions"] = religionOptions;

            return Partial("editOrUpdateEmployee", resultView);
        }

        public virtual async Task<PartialViewResult> OnGetFormChangePassword(int id)

        {

            var resultView = new
            {
                Id = id
            };
            return Partial("formChangePassword", resultView);
        }

        public virtual async Task<PartialViewResult> OnGetFormImportCandidate()

        {
            GetInfoUser();
            var dataCandidate = await _empBusiness.GetAll(new CandidateRequest()
            {
                UserId = UserData.UserId,
                RoleCode = UserData.RoleCode,
                IsEmployee = true
            });
            var resultView = new
            {
                dataAll = dataCandidate.Data
            };
            return Partial("FormImportCandidate", resultView);
        }

        public async Task<IActionResult> OnPostDelete
      (int Id = -1)
        {
            GetInfoUser();

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

            if (!await _empBusiness.HasManageAccess(Id, UserData.UserId, UserData.RoleCode))
            {
                return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
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


        public async Task<IActionResult> OnPostReactive
     (int Id = -1)
        {
            GetInfoUser();

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

            if (!await _empBusiness.HasManageAccess(Id, UserData.UserId, UserData.RoleCode))
            {
                return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
            }

            var result = true;

            result = await _empBusiness.Delete(Id, true);
            var dataReponse = new
            {
                success = result,

            };
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK

            };
        }




        public async Task<IActionResult> OnPostApprovePass(int Id)
        {
            GetInfoUser();
            if (!await _empBusiness.HasManageAccess(Id, UserData.UserId, UserData.RoleCode))
            {
                return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
            }
            if (Id < 0) return new JsonResult(new { success = false }) { StatusCode = StatusCodes.Status400BadRequest };
            var result = await _empBusiness.ApprovePassInterview(Id);
            return new JsonResult(new { success = result }) { StatusCode = StatusCodes.Status200OK };
        }

        public async Task<IActionResult> OnPostApprovePending(int Id)
        {
            GetInfoUser();
            if (!await _empBusiness.HasManageAccess(Id, UserData.UserId, UserData.RoleCode))
            {
                return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
            }
            if (Id < 0) return new JsonResult(new { success = false }) { StatusCode = StatusCodes.Status400BadRequest };
            var result = await _empBusiness.ApprovePendingEmployee(Id);
            return new JsonResult(new { success = result }) { StatusCode = StatusCodes.Status200OK };
        }

        public async Task<IActionResult> OnPostOnboard(int Id)
        {
            GetInfoUser();
            if (!await _empBusiness.HasManageAccess(Id, UserData.UserId, UserData.RoleCode))
            {
                return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
            }
            if (Id < 0) return new JsonResult(new { success = false }) { StatusCode = StatusCodes.Status400BadRequest };
            var emp = await _empBusiness.Onboard(Id);
            var result = emp != null;
            return new JsonResult(new { success = result }) { StatusCode = StatusCodes.Status200OK };
        }

        private bool CanManageRecruitmentData()
        {
            var roleCode = UserData?.RoleCode ?? string.Empty;
            return roleCode == "1" || roleCode == "3" || roleCode == "6" || roleCode == "8" || roleCode == "9";
        }

    }
}
