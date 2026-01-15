using crmHuman.DisplayModel;
using crmHuman.Helpers;
using crmHuman.Model;
using DocumentFormat.OpenXml.Office2016.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Business.Helpers;
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

        public bool IsSelfView { get; set; }



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
            Code ="8", Name ="BGĐ"
            }
        };


        }

        public async Task<IActionResult> OnPostAddSchedule(CandidateScheduleAdd request)
        {
            GetInfoUser();
            var errors = new List<object>();
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            var userId = UserData?.UserId ?? 0;
            var itemInsert = new ScheduleInterviewAdd()
            {
                Id = request.Id,
                AddressInfo = request.AddressInfo,
                ScheduleDate = request.ScheduleDate,
                Noted = request.Noted,
                RelId = request.RelId,
                Type = request.Type,
                Status = request.Status,
                InterviewerId = request.InterviewerId,
                InterviewMode = request.InterviewMode,
                InterviewResult = request.InterviewResult,
                CreatedBy = userId,
                UpdatedBy = userId
            };

            var result = await _scheduleInterviewBussiness.AddOrUpdate(itemInsert);
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }

        public async Task<IActionResult> OnPostAddRelationItem(RelationItemAdd request)
        {
            GetInfoUser();
            var isSelfView = UserData?.RoleCode == "2";
            var errors = new List<object>();
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            if (isSelfView)
            {
                if (UserData == null || UserData.UserId < 1)
                {
                    return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
                }

                request.UserName = UserData.UserName;
            }

            var result = await _employeeExtraBusiness.UpdateRelation(request);
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }


        public async Task<IActionResult> OnPostAddHDLDItem(HDLDItemAdd request)
        {
            GetInfoUser();
            var isSelfView = UserData?.RoleCode == "2";
            var errors = new List<object>();
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            if (isSelfView)
            {
                if (UserData == null || UserData.UserId < 1)
                {
                    return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
                }

                request.UserId = UserData.UserId.ToString();
            }

            var result = await _employeeExtraBusiness.UpdateHDLDItem(request);
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }


        public async Task<IActionResult> OnPostAddEmployee(EmployeeInfoAdd request)
        {
            var errors = new List<object>();
            ValidationHelper.ValidatePhone(request.Phone, errors);
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            var result = await _empBusiness.Update(request);
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }

        public async Task<IActionResult> OnPostAddOtherInfomation(EmployeeInfoOther request)
        {
            GetInfoUser();
            var isSelfView = UserData?.RoleCode == "2";
            var errors = new List<object>();
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            if (isSelfView)
            {
                if (UserData == null || UserData.UserId < 1)
                {
                    return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
                }

                if (request.EmployeeId != UserData.UserId)
                {
                    return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
                }

                request.EmployeeId = UserData.UserId;
            }

            await _employeeExtraBusiness.UpdateEmployeeInfother(request);
            return ApiResponseHelper.Success(true);
        }


        public async Task<IActionResult> OnPostUpdate(EmployeeDetailUpdate request)
        {
            GetInfoUser();
            var isSelfView = UserData?.RoleCode == "2";
            var errors = new List<object>();
            ValidationHelper.ValidatePhone(request.Phone, errors);
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            if (isSelfView)
            {
                if (UserData == null || UserData.UserId < 1)
                {
                    return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
                }

                if (request.Id != UserData.UserId)
                {
                    return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
                }

                var existingEmployee = await _empBusiness.GetById(UserData.UserId);
                if (existingEmployee == null || existingEmployee.Id < 1)
                {
                    return ApiResponseHelper.NotFound("Employee not found");
                }

                request.Id = existingEmployee.Id;
                request.RoleCode = existingEmployee.RoleCode;
                request.StatusWork = existingEmployee.StatusWork;
                request.Status = existingEmployee.Status;
                request.DocumentStatus = existingEmployee.DocumentStatus;
                request.FingerprintCode = existingEmployee.FingerprintCode;
                request.Onboard = existingEmployee.Onboard;
                request.ResignationDate = existingEmployee.ResignationDate;
                request.ManagerId = existingEmployee.ManagerId;
                request.DepartmentCode = existingEmployee.DepartmentCode;
                request.PositionCode = existingEmployee.PositionCode;
            }

            // Log DocumentCheck to help debug
            _logger.LogInformation("Updating employee {Id}. DocumentCheck value: {DocumentCheck}", request.Id, request.DocumentCheck);

            var bodyRequest = EmployeeMapper.MapToEmployeeInfoAdd(request);
            var result = await _empBusiness.Update(bodyRequest);
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }

        public async Task<IActionResult> OnPostChangePassword(PasswordAdd request)
        {
            GetInfoUser();
            var isSelfView = UserData?.RoleCode == "2";
            var errors = new List<object>();
            ValidationHelper.ValidateRequired(request.NewPassword, "txtrenewPassword", "mật khẩu mới", errors);
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            // Reset password if requested
            if (request.ResetPass == true)
            {
                request.NewPassword = "Vietstar@2024";
            }

            // Determine employee ID
            int employeeId = -1;
            if (request.Id.HasValue && request.Id.Value > 0)
            {
                employeeId = request.Id.Value;
            }
            else
            {
                employeeId = UserData.UserId;
            }

            if (isSelfView)
            {
                if (UserData == null || UserData.UserId < 1)
                {
                    return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
                }

                if (request.Id.HasValue && request.Id.Value != UserData.UserId)
                {
                    return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
                }

                employeeId = UserData.UserId;
                request.ResetPass = false;
            }

            var result = await _empBusiness.ChangePassword(request.NewPassword, employeeId);
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }

        public async Task<IActionResult> OnPostAddDocument([FromBody] DocumentDataAddRequest request)
        {
            GetInfoUser();
            var isSelfView = UserData?.RoleCode == "2";
            var errors = new List<object>();
            ValidationHelper.ValidateId(request.RelId, "txtFullName", "đối tượng Id", errors);
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            if (isSelfView)
            {
                if (UserData == null || UserData.UserId < 1)
                {
                    return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
                }

                if (request.RelId != UserData.UserId)
                {
                    return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
                }

                request.RelId = UserData.UserId;
            }

            request.UserId = UserData.UserId;

            var result = await _documentDataBussiness.AddOrUpdate(request);
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }

        public async Task<ActionResult> OnGet([FromQuery] CandidateEditRequest request)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }
            GetInfoUser();
            IsSelfView = UserData?.RoleCode == "2";
            var idInput = request.Id ?? -1;
            if (IsSelfView)
            {
                if (UserData == null || UserData.UserId < 1)
                {
                    return Redirect("/Login");
                }

                idInput = UserData.UserId;
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
            if (idInput < 1)
            {
                TitlePage = "Thêm mới nhân viên";
            }

            var itemInfo = await _empBusiness.GetById(idInput);
            
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
                ResignationDate = itemInfo.ResignationDate,
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
                StatusWork = itemInfo.StatusWork,
                Gender = itemInfo.Gender,
                PlaceOfBirth = itemInfo.PlaceOfBirth,
                Ethnicity = itemInfo.Ethnicity,
                Religion = itemInfo.Religion,
                PersonalEmail = itemInfo.PersonalEmail,
                BeneficiaryName = itemInfo.BeneficiaryName,
                EmergencyContact = itemInfo.EmergencyContact,
                FingerprintCode = itemInfo.FingerprintCode

            };


            ResultModel = resultView;
            var dataAllHistory = await _scheduleInterviewBussiness.GetAll(new ScheduleInterviewRquest()
            {
                RelId = idInput,
                Type = -1,
                From = null,
                To = null
            });
            DataHistory = dataAllHistory;
            DataFile = await _documentDataBussiness.GetAll(new DocumentDataRquest()
            {
                DataType = 2,
                RelId = idInput,
                CurrentUserId = UserData.UserId

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

        public async Task<IActionResult> OnPostDelete(int Id = -1)
        {
            var errors = new List<object>();
            ValidationHelper.ValidateIdForDelete(Id, errors);
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            var result = await _empBusiness.Delete(Id);
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }


        public async Task<IActionResult> OnPostReactive(int Id = -1)
        {
            var errors = new List<object>();
            ValidationHelper.ValidateIdForDelete(Id, errors);
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            var result = await _empBusiness.Delete(Id, true);
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }



    }
}
