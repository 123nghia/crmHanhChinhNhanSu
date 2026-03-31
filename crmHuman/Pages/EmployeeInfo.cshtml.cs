using crmHuman.DisplayModel;
using crmHuman.Helpers;
using crmHuman.Model;
using DocumentFormat.OpenXml.Office2016.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Security.Cryptography;
using System.Text;
using VS.Human.Business;
using VS.Human.Business.Helpers;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep.Model;

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
        private readonly IWebHostEnvironment _hostingEnvironment;

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
            IEmployeeExtraBusiness employeeExtraBusiness,
            IWebHostEnvironment hostingEnvironment
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
            _hostingEnvironment = hostingEnvironment;



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
            Code ="9", Name ="HCNS"
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

        private IActionResult? RejectIfTcRole()
        {
            GetInfoUser();
            if (UserData?.RoleCode == "2")
            {
                return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
            }

            return null;
        }

        private bool IsEmployeeSelfView()
        {
            return UserData?.RoleCode == "2";
        }

        private bool CanRequestDocumentSignature()
        {
            return UserData?.RoleCode == "1" || UserData?.RoleCode == "8" || UserData?.RoleCode == "9";
        }

        private bool CanEmployeeAccessDocument(DocumentData? document)
        {
            return document != null
                && document.Id > 0
                && document.dataType == 2
                && document.RelId == UserData?.UserId;
        }

        public async Task<IActionResult> OnPostAddSchedule(CandidateScheduleAdd request)
        {
            var reject = RejectIfTcRole();
            if (reject != null)
            {
                return reject;
            }
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
                SendEmail = request.SendEmail,
                CreatedBy = userId,
                UpdatedBy = userId
            };

            var result = await _scheduleInterviewBussiness.SaveInterviewSchedule(itemInsert, userId);
            return ApiResponseHelper.SuccessResponse(new { success = result.Success, message = result.Message });
        }

        public async Task<IActionResult> OnPostAddRelationItem(RelationItemAdd request)
        {
            var reject = RejectIfTcRole();
            if (reject != null)
            {
                return reject;
            }
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
            var reject = RejectIfTcRole();
            if (reject != null)
            {
                return reject;
            }
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
            var reject = RejectIfTcRole();
            if (reject != null)
            {
                return reject;
            }
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
            var reject = RejectIfTcRole();
            if (reject != null)
            {
                return reject;
            }
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

                if (request.EmployeeId < 1)
                {
                    request.EmployeeId = UserData.UserId;
                }

                if (request.EmployeeId != UserData.UserId)
                {
                    return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
                }
            }

            await _employeeExtraBusiness.UpdateEmployeeInfother(request);
            return ApiResponseHelper.Success(true);
        }


        public async Task<IActionResult> OnPostUpdate(EmployeeDetailUpdate request)
        {
            var reject = RejectIfTcRole();
            if (reject != null)
            {
                return reject;
            }
            var isSelfView = UserData?.RoleCode == "2";
            var errors = new List<object>();
            Employee? existingEmployee = null;
            var requestId = request.Id;

            if (isSelfView)
            {
                if (UserData == null || UserData.UserId < 1)
                {
                    return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
                }

                if (requestId < 1)
                {
                    requestId = UserData.UserId;
                }

                if (requestId != UserData.UserId)
                {
                    return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
                }

                request.Id = UserData.UserId;
                existingEmployee = await _empBusiness.GetById(UserData.UserId);
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
            else if (requestId > 0)
            {
                existingEmployee = await _empBusiness.GetById(requestId);
                if (existingEmployee == null || existingEmployee.Id < 1)
                {
                    return ApiResponseHelper.NotFound("Employee not found");
                }
            }

            if (string.IsNullOrWhiteSpace(request.Phone) && !string.IsNullOrWhiteSpace(existingEmployee?.Phone))
            {
                request.Phone = existingEmployee.Phone;
            }

            if (request.Id < 1)
            {
                ValidationHelper.ValidatePhone(request.Phone, errors);
            }
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            // Log DocumentCheck to help debug
            _logger.LogInformation("Updating employee {Id}. DocumentCheck value: {DocumentCheck}", request.Id, request.DocumentCheck);

            var bodyRequest = EmployeeMapper.MapToEmployeeInfoAdd(request);
            var result = await _empBusiness.Update(bodyRequest);
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }

        public async Task<IActionResult> OnPostChangePassword(PasswordAdd request)
        {
            var reject = RejectIfTcRole();
            if (reject != null)
            {
                return reject;
            }
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
                request.NewPassword = "Vietstar@2026";
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
            var reject = RejectIfTcRole();
            if (reject != null)
            {
                return reject;
            }
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

                if (request.RelId < 1)
                {
                    request.RelId = UserData.UserId;
                }

                if (request.RelId != UserData.UserId)
                {
                    return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
                }
            }

            request.UserId = UserData.UserId;

            var result = await _documentDataBussiness.AddOrUpdate(request);
            if (!result)
            {
                return ApiResponseHelper.Error("Tài liệu đang chờ ký hoặc đã ký, không thể cập nhật.", StatusCodes.Status409Conflict);
            }

            return ApiResponseHelper.SuccessResponse(new { success = result });
        }

        public async Task<IActionResult> OnPostRequestDocumentSign(int id)
        {
            GetInfoUser();
            if (!CanRequestDocumentSignature())
            {
                return ApiResponseHelper.Error("Access denied", StatusCodes.Status403Forbidden);
            }

            if (id < 1)
            {
                return ApiResponseHelper.BadRequest("txtDocumentSignPassword", "Thiếu tài liệu cần yêu cầu ký");
            }

            var document = await _documentDataBussiness.GetById(id);
            if (document == null || document.Id < 1 || document.dataType != 2 || document.IsFolder)
            {
                return ApiResponseHelper.NotFound("Document not found");
            }

            if (string.IsNullOrWhiteSpace(document.ValueFile))
            {
                return ApiResponseHelper.Error("Tài liệu chưa có file để yêu cầu ký", StatusCodes.Status400BadRequest);
            }

            if (document.IsSignedInternal)
            {
                return ApiResponseHelper.Error("Tài liệu đã được ký nội bộ", StatusCodes.Status409Conflict);
            }

            if (document.IsSignatureRequested)
            {
                return ApiResponseHelper.Error("Tài liệu đã được yêu cầu ký", StatusCodes.Status409Conflict);
            }

            var requestedAt = DateTime.Now;
            var result = await _documentDataBussiness.RequestInternalSign(id, UserData.UserId, requestedAt);
            if (!result)
            {
                return ApiResponseHelper.Error("Không thể yêu cầu ký tài liệu này", StatusCodes.Status409Conflict);
            }

            return ApiResponseHelper.SuccessResponse(new
            {
                success = true,
                requestedAt = requestedAt.ToString("dd/MM/yyyy HH:mm"),
                requestedBy = UserData.UserName
            });
        }

        public async Task<IActionResult> OnGetDocumentSignForm(int id)
        {
            GetInfoUser();
            if (!IsEmployeeSelfView())
            {
                return new JsonResult(new { success = false, message = "Bạn không có quyền ký tài liệu này" })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            var document = await _documentDataBussiness.GetById(id);
            if (document == null || document.Id < 1)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy tài liệu" })
                {
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            if (!CanEmployeeAccessDocument(document))
            {
                return new JsonResult(new { success = false, message = "Bạn không có quyền ký tài liệu này" })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            var viewModel = new
            {
                Document = document,
                CanSign = document.IsSignatureRequested && !document.IsSignedInternal,
                TermsTitle = "Điều khoản ký tài liệu nội bộ",
                TermsContent = "Tôi xác nhận đã đọc kỹ nội dung tài liệu, đồng ý ký nội bộ cho tài liệu này và hiểu rằng tài liệu sau khi ký sẽ được khóa chỉnh sửa, lưu dấu vết hash và thời gian ký trên hệ thống."
            };

            return Partial("ControlForm/SignEmployeeDocument", viewModel);
        }

        public async Task<IActionResult> OnPostSignDocumentInternal([FromForm] DocumentSignRequest request)
        {
            GetInfoUser();

            var listError = new List<object>();
            if (!IsEmployeeSelfView())
            {
                return new JsonResult(new { success = false, message = "Bạn không có quyền ký tài liệu này" })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            if (request.Id < 1)
            {
                listError.Add(new { name = "txtDocumentSignPassword", Content = "Thiếu tài liệu cần ký" });
            }
            if (string.IsNullOrWhiteSpace(request.PasswordConfirm))
            {
                listError.Add(new { name = "txtDocumentSignPassword", Content = "Nhập mật khẩu xác nhận" });
            }
            if (!request.AcceptTerms)
            {
                listError.Add(new { name = "cbDocumentAcceptTerms", Content = "Bạn cần chấp nhận điều khoản trước khi ký" });
            }
            if (string.IsNullOrWhiteSpace(request.SignatureDataUrl))
            {
                listError.Add(new { name = "employeeSignatureCanvas", Content = "Nhân viên cần vẽ chữ ký trước khi ký tài liệu" });
            }
            if (listError.Count > 0)
            {
                return new JsonResult(listError) { StatusCode = StatusCodes.Status400BadRequest };
            }

            var document = await _documentDataBussiness.GetById(request.Id);
            if (document == null || document.Id < 1)
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy tài liệu" })
                {
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            if (!CanEmployeeAccessDocument(document))
            {
                return new JsonResult(new { success = false, message = "Bạn không có quyền ký tài liệu này" })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            if (document.IsSignedInternal)
            {
                return new JsonResult(new { success = false, message = "Tài liệu đã được ký nội bộ" })
                {
                    StatusCode = StatusCodes.Status409Conflict
                };
            }

            if (!document.IsSignatureRequested)
            {
                return new JsonResult(new { success = false, message = "Tài liệu này chưa được yêu cầu ký" })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            var documentFilePath = ResolveDocumentFilePath(document.ValueFile);
            if (string.IsNullOrWhiteSpace(documentFilePath) || !global::System.IO.File.Exists(documentFilePath))
            {
                return new JsonResult(new { success = false, message = "Không tìm thấy file tài liệu để ký" })
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            var authEmployee = await _empBusiness.Login(UserData.UserName, request.PasswordConfirm ?? string.Empty);
            if (authEmployee == null || authEmployee.Id != UserData.UserId)
            {
                return new JsonResult(new[] { new { name = "txtDocumentSignPassword", Content = "Mật khẩu xác nhận không đúng" } })
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            var fileHash = await ComputeFileSha256Async(documentFilePath);
            var signedAt = DateTime.Now;
            var signatureImageBytes = DecodeSignatureDataUrl(request.SignatureDataUrl);
            if (signatureImageBytes == null || signatureImageBytes.Length == 0)
            {
                return new JsonResult(new[] { new { name = "employeeSignatureCanvas", Content = "Chữ ký vẽ tay không hợp lệ" } })
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            var signedIpAddress = GetRequestIpAddress();
            var signedUserAgent = GetRequestUserAgent();
            var signatureImagePath = await SaveSignatureImageAsync(document, signatureImageBytes, signedAt, fileHash);
            var signatureImageHash = ComputeSha256(signatureImageBytes);
            var signedFileArchivePath = await ArchiveSignedDocumentAsync(document, documentFilePath, signatureImageBytes, signedAt, fileHash);
            var signatureIntentText = BuildDocumentSignatureIntentText();
            var signatureHash = ComputeDocumentSignatureHash(document, UserData.UserId, UserData.UserName, UserData.FullName, signedIpAddress, signedUserAgent, signedFileArchivePath, signatureImageHash, signedAt, fileHash, signatureIntentText, request.SignatureCode, request.SignNote);
            var signNote = BuildDocumentSignNote(signatureIntentText, request.SignatureCode, request.SignNote);

            var signed = await _documentDataBussiness.SignInternal(
                document.Id,
                UserData.UserId,
                signedAt,
                signatureHash,
                fileHash,
                signNote,
                "EMPLOYEE_DOCUMENT_INTERNAL_SHA256",
                UserData.UserName,
                UserData.FullName,
                signedIpAddress,
                signedUserAgent,
                signedFileArchivePath,
                signatureImagePath,
                signatureIntentText,
                signedAt);

            if (!signed)
            {
                DeleteArchivedFileIfExists(signedFileArchivePath);
                DeleteArchivedFileIfExists(signatureImagePath);
                return new JsonResult(new { success = false, message = "Không thể ký tài liệu. Tài liệu có thể đã được cập nhật bởi người khác." })
                {
                    StatusCode = StatusCodes.Status409Conflict
                };
            }

            return new JsonResult(new
            {
                success = true,
                signedAt = signedAt.ToString("dd/MM/yyyy HH:mm"),
                signedBy = UserData.UserName
            });
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
                UserId = UserData.UserId,
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
            var reject = RejectIfTcRole();
            if (reject != null)
            {
                return reject;
            }
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
            var reject = RejectIfTcRole();
            if (reject != null)
            {
                return reject;
            }
            var errors = new List<object>();
            ValidationHelper.ValidateIdForDelete(Id, errors);
            
            if (ValidationHelper.HasErrors(errors))
            {
                return ApiResponseHelper.BadRequest(errors);
            }

            var result = await _empBusiness.Delete(Id, true);
            return ApiResponseHelper.SuccessResponse(new { success = result });
        }

        private string? ResolveDocumentFilePath(string? fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
            {
                return null;
            }

            var normalizedRelativePath = fileUrl
                .Replace('/', global::System.IO.Path.DirectorySeparatorChar)
                .TrimStart(global::System.IO.Path.DirectorySeparatorChar);

            var webRoot = global::System.IO.Path.GetFullPath(_hostingEnvironment.WebRootPath);
            var fullPath = global::System.IO.Path.GetFullPath(global::System.IO.Path.Combine(webRoot, normalizedRelativePath));
            if (!fullPath.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return fullPath;
        }

        private static async Task<string> ComputeFileSha256Async(string filePath)
        {
            using var stream = new global::System.IO.FileStream(filePath, global::System.IO.FileMode.Open, global::System.IO.FileAccess.Read, global::System.IO.FileShare.Read);
            using var sha = SHA256.Create();
            var hashBytes = await sha.ComputeHashAsync(stream);
            return Convert.ToHexString(hashBytes);
        }

        private static string ComputeDocumentSignatureHash(DocumentData document, int signedBy, string? signedByUserName, string? signedByFullName, string? signedIpAddress, string? signedUserAgent, string? signedFileArchivePath, string? signatureImageHash, DateTime signedAt, string fileHash, string signatureIntentText, string? signatureCode, string? signNote)
        {
            var payload = string.Join("|", new[]
            {
                document.Id.ToString(),
                (document.RelId ?? 0).ToString(),
                signedBy.ToString(),
                (signedByUserName ?? string.Empty).Trim(),
                (signedByFullName ?? string.Empty).Trim(),
            (signedIpAddress ?? string.Empty).Trim(),
            (signedUserAgent ?? string.Empty).Trim(),
            (signedFileArchivePath ?? string.Empty).Trim(),
                (signatureImageHash ?? string.Empty).Trim(),
                signedAt.ToString("O"),
                fileHash,
                signatureIntentText.Trim(),
                (signatureCode ?? string.Empty).Trim(),
                (signNote ?? string.Empty).Trim()
            });

            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hashBytes);
        }

        private static string BuildDocumentSignatureIntentText()
        {
            return "Toi xac nhan chinh toi la nguoi ky, da doc, hieu, dong y voi noi dung tai lieu va dong y ky dien tu noi bo cho tai lieu nay.";
        }

        private static string? BuildDocumentSignNote(string signatureIntentText, string? signatureCode, string? signNote)
        {
            var code = (signatureCode ?? string.Empty).Trim();
            var note = (signNote ?? string.Empty).Trim();

            var normalized = $"INTENT:{signatureIntentText.Trim()}";
            if (!string.IsNullOrWhiteSpace(code))
            {
                normalized = $"{normalized}; CODE:{code}";
            }
            if (!string.IsNullOrWhiteSpace(note))
            {
                normalized = $"{normalized}; NOTE:{note}";
            }

            if (normalized.Length > 500)
            {
                return normalized.Substring(0, 500);
            }

            return normalized;
        }

        private string? GetRequestIpAddress()
        {
            return HttpContext?.Connection?.RemoteIpAddress?.ToString();
        }

        private string? GetRequestUserAgent()
        {
            var value = HttpContext?.Request?.Headers["User-Agent"].ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Length > 500 ? value.Substring(0, 500) : value;
        }

        private static byte[]? DecodeSignatureDataUrl(string? signatureDataUrl)
        {
            if (string.IsNullOrWhiteSpace(signatureDataUrl))
            {
                return null;
            }

            var commaIndex = signatureDataUrl.IndexOf(',');
            if (commaIndex < 0 || commaIndex >= signatureDataUrl.Length - 1)
            {
                return null;
            }

            try
            {
                return Convert.FromBase64String(signatureDataUrl.Substring(commaIndex + 1));
            }
            catch
            {
                return null;
            }
        }

        private static string ComputeSha256(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(bytes));
        }

        private async Task<string?> SaveSignatureImageAsync(DocumentData document, byte[] signatureImageBytes, DateTime signedAt, string fileHash)
        {
            var relativePath = $"/signed-archive/employee-signatures/{signedAt:yyyy}/{signedAt:MM}/sign-{document.Id}-emp-{document.RelId ?? 0}-{signedAt:yyyyMMddHHmmss}-{fileHash.Substring(0, Math.Min(12, fileHash.Length))}.png";
            var fullPath = ResolveArchiveAbsolutePath(relativePath);
            var directory = global::System.IO.Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await global::System.IO.File.WriteAllBytesAsync(fullPath, signatureImageBytes);
            var fileInfo = new global::System.IO.FileInfo(fullPath);
            fileInfo.IsReadOnly = true;
            return relativePath;
        }

        private async Task<string?> ArchiveSignedDocumentAsync(DocumentData document, string sourceFilePath, byte[] signatureImageBytes, DateTime signedAt, string fileHash)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !global::System.IO.File.Exists(sourceFilePath))
            {
                return null;
            }

            var extension = global::System.IO.Path.GetExtension(sourceFilePath);
            var archiveRelativePath = $"/signed-archive/employee-documents/{signedAt:yyyy}/{signedAt:MM}/doc-{document.Id}-emp-{document.RelId ?? 0}-{signedAt:yyyyMMddHHmmss}-{fileHash.Substring(0, Math.Min(12, fileHash.Length))}{extension}";
            var archiveFullPath = ResolveArchiveAbsolutePath(archiveRelativePath);
            var archiveDirectory = global::System.IO.Path.GetDirectoryName(archiveFullPath);
            if (!string.IsNullOrWhiteSpace(archiveDirectory))
            {
                Directory.CreateDirectory(archiveDirectory);
            }

            var normalizedExtension = extension.ToLowerInvariant();
            if (normalizedExtension == ".pdf")
            {
                await StampPdfAsync(sourceFilePath, archiveFullPath, signatureImageBytes);
            }
            else if (normalizedExtension == ".png" || normalizedExtension == ".jpg" || normalizedExtension == ".jpeg" || normalizedExtension == ".bmp" || normalizedExtension == ".gif" || normalizedExtension == ".webp")
            {
                await StampImageAsync(sourceFilePath, archiveFullPath, signatureImageBytes);
            }
            else
            {
                await using (var sourceStream = new global::System.IO.FileStream(sourceFilePath, global::System.IO.FileMode.Open, global::System.IO.FileAccess.Read, global::System.IO.FileShare.Read))
                await using (var destinationStream = new global::System.IO.FileStream(archiveFullPath, global::System.IO.FileMode.CreateNew, global::System.IO.FileAccess.Write, global::System.IO.FileShare.None))
                {
                    await sourceStream.CopyToAsync(destinationStream);
                }
            }

            var fileInfo = new global::System.IO.FileInfo(archiveFullPath);
            fileInfo.IsReadOnly = true;

            return archiveRelativePath;
        }

        private async Task StampImageAsync(string sourceFilePath, string destinationFilePath, byte[] signatureImageBytes)
        {
            using var baseImage = await Image.LoadAsync(sourceFilePath);
            await using var signatureImageStream = new MemoryStream(signatureImageBytes);
            using var signatureImage = await Image.LoadAsync(signatureImageStream);

            var targetWidth = Math.Max(140, baseImage.Width / 4);
            signatureImage.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new SixLabors.ImageSharp.Size(targetWidth, 0)
            }));

            var padding = 24;
            var posX = Math.Max(padding, baseImage.Width - signatureImage.Width - padding);
            var posY = Math.Max(padding, baseImage.Height - signatureImage.Height - padding);
            baseImage.Mutate(ctx => ctx.DrawImage(signatureImage, new SixLabors.ImageSharp.Point(posX, posY), 1f));
            await baseImage.SaveAsync(destinationFilePath);
        }

        private Task StampPdfAsync(string sourceFilePath, string destinationFilePath, byte[] signatureImageBytes)
        {
            using var sourceStream = new global::System.IO.FileStream(sourceFilePath, global::System.IO.FileMode.Open, global::System.IO.FileAccess.Read, global::System.IO.FileShare.Read);
            using var document = PdfReader.Open(sourceStream, PdfDocumentOpenMode.Modify);
            var page = document.Pages[document.PageCount - 1];
            using var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
            using var signatureImage = XImage.FromStream(() => new MemoryStream(signatureImageBytes));

            var maxWidth = Math.Min(180, page.Width / 3);
            var scale = signatureImage.PixelWidth > 0 ? maxWidth / signatureImage.PixelWidth : 1;
            var width = signatureImage.PixelWidth * scale;
            var height = signatureImage.PixelHeight * scale;
            var x = page.Width - width - 30;
            var y = page.Height - height - 40;
            graphics.DrawImage(signatureImage, x, y, width, height);
            document.Save(destinationFilePath);
            return Task.CompletedTask;
        }

        private string ResolveArchiveAbsolutePath(string archiveRelativePath)
        {
            var normalized = archiveRelativePath.TrimStart('/').Replace('/', global::System.IO.Path.DirectorySeparatorChar);
            return global::System.IO.Path.Combine(_hostingEnvironment.WebRootPath, normalized);
        }

        private void DeleteArchivedFileIfExists(string? archiveRelativePath)
        {
            if (string.IsNullOrWhiteSpace(archiveRelativePath))
            {
                return;
            }

            var archiveFullPath = ResolveArchiveAbsolutePath(archiveRelativePath);

            if (global::System.IO.File.Exists(archiveFullPath))
            {
                var fileInfo = new global::System.IO.FileInfo(archiveFullPath);
                if (fileInfo.IsReadOnly)
                {
                    fileInfo.IsReadOnly = false;
                }

                global::System.IO.File.Delete(archiveFullPath);
            }
        }



    }
}

