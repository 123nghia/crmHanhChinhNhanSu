using crmHuman.DisplayModel;
using crmHuman.Model;
using DocumentFormat.OpenXml.Office2016.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Item;

namespace crmHuman.Pages
{
    [Authorize]
    public class EmployeeInfoModel : BaseModel2
    {
        private readonly ILogger<CandidateModel> _logger;
        private readonly IEmpBusiness _empBusiness;
        private readonly ImasterDataBussiness _masterDataBussiness;
        private readonly IScheduleInterviewBussiness _scheduleInterviewBussiness;
        private readonly IDocumentDataBussiness _documentDataBussiness;
        public readonly IEmployeeExtraBusiness _employeeExtraBusiness;

        public List<SelectDisplay> ArrayRol { get; set; }

        public List<string> TableColumnTextAdmin { get; set; }
        public CandidateRequest RequestSearch { get; set; }
        public BaseList DataAll { get; set; }

        public BaseList DataMasterData { get; set; }

        public BaseList DataPostion { get; set; }

        public BaseList DataLead { get; set; }

        public BaseList DataHistory { get; set; }

        public EmployeeDisplayEdit ResultModel { get; set; }

        public List<DataMasterItem> DataDepartment { get; set; }


        public BaseList DataFile { get; set; }



        public int TotalRecord
        {

            get
            {
                return DataAll.Total;

            }
        }
        public EmployeeInfoModel(ILogger<CandidateModel> logger,
            IEmpBusiness empBusiness,
            ImasterDataBussiness masterDataBussiness,
            IScheduleInterviewBussiness scheduleInterviewBussiness,
            IDocumentDataBussiness documentDataBussiness,
            IEmpBusiness empBusiness1,
            IEmployeeExtraBusiness employeeExtraBusiness
            )
        {
            _logger = logger;
            _empBusiness = empBusiness;
            TitlePage = "Thông tin nhân viên";
            KeyPage = "CandidateDetail";
            _masterDataBussiness = masterDataBussiness;
            DataPostion = new BaseList();
            DataDepartment = new List<DataMasterItem>();
            _scheduleInterviewBussiness = scheduleInterviewBussiness;
            _documentDataBussiness = documentDataBussiness;
            _employeeExtraBusiness = employeeExtraBusiness;



            ArrayRol = new List<SelectDisplay>()
        {
            new Model.SelectDisplay()
            {
                Code ="1", Name ="Admin"
            },
            new Model.SelectDisplay()
            {
            Code ="2", Name ="TC"
            },
            new Model.SelectDisplay()
            {
            Code ="3", Name ="TL"
            },
            new Model.SelectDisplay()
            {
            Code ="4", Name ="Marketing"
            },
            new Model.SelectDisplay()
            {
            Code ="6", Name ="Trưởng CTV"
            },
            new Model.SelectDisplay()
            {
            Code ="7", Name ="CTV"
            }
        };


        }

        public async Task<IActionResult> OnPostAddSchedule
           (CandidateScheduleAdd request)
        {
            var listEror = new List<object>();

            if (listEror.Count > 0)
            {
                return new JsonResult(listEror)
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
            var result = true;
            var itemInsert = new ScheduleInterviewAdd()
            {
                AddressInfo = request.AddressInfo,
                ScheduleDate = request.ScheduleDate,
                Noted = request.Noted,
                RelId = request.RelId,

            };

            result = await _scheduleInterviewBussiness.AddOrUpdate(itemInsert);
            var dataReponse = new
            {
                success = result,
            };
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<IActionResult> OnPostAddRelationItem
          (RelationItemAdd request)
        {
            var listEror = new List<object>();
            if (listEror.Count > 0)
            {
                return new JsonResult(listEror)
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
            var result = true;
            result = await _employeeExtraBusiness.UpdateRelation(request);
            var dataReponse = new
            {
                success = result,
            };
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK
            };
        }


        public async Task<IActionResult> OnPostAddHDLDItem
        (HDLDItemAdd request)
        {
            var listEror = new List<object>();
            if (listEror.Count > 0)
            {
                return new JsonResult(listEror)
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
            var result = true;
            result = await _employeeExtraBusiness.UpdateHDLDItem(request);
            var dataReponse = new
            {
                success = result,
            };
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK
            };
        }


        public async Task<IActionResult> OnPostAddEmployee
            (EmployeeInfoAdd request)
        {
            var listEror = new List<object>();
            if (request.Id < 1)
            {
                var itemError = new
                {
                    name = "txtFullName",
                    Content = "Thiếu thông tin đối tượng Id"
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
            var result = true;
            request.Id = request.Id;
            if (request.Id < 0)
            {
                result = await _empBusiness.Update(request);
            }
            else
            {
                result = await _empBusiness.Update(request);
            }
            var dataReponse = new
            {
                success = result,
            };
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<IActionResult> OnPostAddOtherInfomation
           (EmployeeInfoOther request)
        {
            var listEror = new List<object>();


            if (listEror.Count > 0)
            {
                return new JsonResult(listEror)
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
            var dataReponse = true;
            await _employeeExtraBusiness.UpdateEmployeeInfother(request);
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK
            };
        }


        public async Task<IActionResult> OnPostUpdate
            (EmployeeDetailUpdate request)
        {
            var listEror = new List<object>();
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
            var result = true;
            var bodyRequest = new EmployeeInfoAdd()
            {
                Id = request.Id,
                RoleCode = request.RoleCode,
                FullName = request.FullName,
                NationalDate = request.NationalDate,
                NationalId = request.NationalId,
                NationalPlace = request.NationalPlace,
                Dob = request.Dob,
                Onboard = request.Onboard,
                Phone = request.Phone,
                ManagerId = request.ManagerId,
                DepartmentCode = request.DepartmentCode,
                PositionCode = request.PositionCode,
                Email = request.Email,
                Noted = request.Noted,
                StatusWork = request.StatusWork,
                PermanentAddress = request.PermanentAddress,
                TemporaryAddress = request.TemporaryAddress,
                DocumentStatus = request.DocumentStatus,
                Status = request.Status,
                CVLink = request.CVLink,
                BankAccount = request.BankAccount,
                BankName = request.BankName,
                EducationLevel = request.EducationLevel,
                Maritalstatus = request.Maritalstatus,
                DocumentCheck = request.DocumentCheck
            };
            if (request.Id < 0)
            {
                result = await _empBusiness.Update(bodyRequest);
            }
            else
            {
                result = await _empBusiness.Update(bodyRequest);
            }
            var dataReponse = new
            {
                success = result,
            };
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<IActionResult> OnPostChangePassword
        (PasswordAdd request)
        {
            var listEror = new List<object>();
            if (request.Id < 1)
            {
                var itemError = new
                {
                    name = "txtrid",
                    Content = "Thiếu thông tin Id"
                };
                listEror.Add(itemError);
            }
            if (string.IsNullOrEmpty(request.NewPassword))
            {
                var itemError = new
                {
                    name = "txtrenewPassword",
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

            int IdEmployer = -1;
            if (request.ResetPass == true)
            {
                request.NewPassword = "Vietstar@2024";
            }
            if (request.Id.HasValue && request.Id.Value > 0)
            {
                IdEmployer = request.Id.Value;
            }
            else
            {
                GetInfoUser();
                var userId = UserData.UserId;
                IdEmployer = userId;
            }
            var result = await _empBusiness.ChangePassword(request.NewPassword, IdEmployer);
            var dataReponse = new
            {
                success = result,

            };
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK

            };
        }

        public async Task<IActionResult> OnPostAddDocument
           (DocumentDataAddRequest request)
        {
            var listEror = new List<object>();
            if (request.RelId < 1)
            {
                var itemError = new
                {
                    name = "txtFullName",
                    Content = "Thiếu thông tin đối tượng Id"
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
            var result = await _documentDataBussiness.AddOrUpdate(request);
            var dataReponse = new
            {
                success = result,
            };

            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<ActionResult> OnGet([FromQuery] CandidateEditRequest request)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }
            GetInfoUser();
            var idInput = request.Id.HasValue == true ? request.Id.Value : -1;
            if(idInput ==-1)
            {
                idInput = -1;
            }
            var dataAllMaster = await _masterDataBussiness.GetAll(new CommonRequest());
            DataMasterData = dataAllMaster ?? new BaseList { Data = new List<object>() };
            if (DataDepartment == null)
                DataDepartment = new List<DataMasterItem>();
            
            if (dataAllMaster?.Data != null)
            {
                foreach (var item in dataAllMaster.Data)
                {
                    var tempItem = item as dynamic;
                    if (tempItem != null)
                    {
                        var itemInsert = new DataMasterItem()
                        {
                            Name = tempItem.Name ?? string.Empty,
                            TypeData = tempItem.TypeData ?? 0,
                            Code = tempItem.Code ?? string.Empty,
                            ApplyFor = tempItem.ApplyFor ?? string.Empty,
                            IsActive = tempItem.IsActive ?? false
                        };
                        DataDepartment.Add(itemInsert);
                    }
                }
            }
            DataPostion = dataAllMaster;
            var itemInfo = await _empBusiness.GetById(idInput);
            if (idInput < 1)
            {
                TitlePage = "Thêm mới nhân viên";
            }
            var dataRelation = await _employeeExtraBusiness.GetInfo(itemInfo.UserName);
            var hdldItem = await _employeeExtraBusiness.GetHDLD(itemInfo.Id.ToString());
            var bhxhItem = await _employeeExtraBusiness.GetBHXH(itemInfo.UserName);
            var taxtItem = await _employeeExtraBusiness.GetTaxItem(itemInfo.UserName);
            var resultView = new EmployeeDisplayEdit()
            {
                Id = idInput,
                UserName = itemInfo.UserName,
                FullName = itemInfo.FullName,
                NationalDate = itemInfo.NationalDate,
                NationalId = itemInfo.NationalId,
                NationalPlace = itemInfo.NationalPlace,
                Phone = itemInfo.Phone,
                CVLink = itemInfo.CVLink,
                Dob = itemInfo.Dob,
                Email = itemInfo.Email,
                Onboard = itemInfo.Onboard,
                Deleted = false,
                CreateAt = itemInfo.CreateAt,
                UpdateAt = itemInfo.UpdateAt,
                Noted = itemInfo.Noted,
                ManagerId = itemInfo.ManagerId,
                DocumentStatus = itemInfo.DocumentStatus,
                PositionCode = itemInfo.PositionCode,
                DepartmentCode = itemInfo.DepartmentCode,
                Status = itemInfo.Status,
                IsActive = itemInfo.IsActive,
                TemporaryAddress = itemInfo.TemporaryAddress,
                PermanentAddress = itemInfo.PermanentAddress,
                RoleCode = itemInfo.RoleCode,
                DataRelation = dataRelation,
                HDLD = hdldItem,
                BHXHItem = bhxhItem,
                TaxItem = taxtItem,
                BankAccount = itemInfo.BankAccount,
                BankName = itemInfo.BankName,
                EducationLevel = itemInfo.EducationLevel,
                Maritalstatus = itemInfo.Maritalstatus,
                DocumentCheck = itemInfo.DocumentCheck,
                DataCheckList = itemInfo.DocumentCheck != null
          ? itemInfo.DocumentCheck
        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        .Select(x => x.Trim())
        .ToList() : new List<string>(),
                StatusWork = itemInfo.StatusWork

            };


            ResultModel = resultView;
            var dataAllHistory = await _scheduleInterviewBussiness.GetAll(new ScheduleInterviewRquest()
            {
                RelId = idInput,
                Type = 0
            });
            DataHistory = dataAllHistory;
            DataFile = await _documentDataBussiness.GetAll(new DocumentDataRquest()
            {
                DataType = 2,
                RelId = idInput

            });
            DataLead = await _empBusiness.GetAllManager();

           
            return Page();
        }


        public virtual async Task<PartialViewResult> OnGetFormChangePassword(int id)

        {

            var resultView = new
            {
                Id = id
            };
            return Partial("formChangePassword", resultView);
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


        public async Task<IActionResult> OnPostReactive
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



    }
}
