using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep.Model;
using ContractEntity = VS.Human.Rep.Model.Contract;

namespace crmHuman.Pages
{
    [Authorize]
    public class ContractModel : BaseModel2
    {
        private const string InternalSignMethod = "INTERNAL_SHA256";
        private readonly ILogger<ContractModel> _logger;
        private readonly IContractBusiness _contractBusiness;
        private readonly IEmpBusiness _empBusiness;
        private readonly ImasterDataBussiness _masterDataBussiness;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public ContractRequest RequestSearch { get; set; }
        public BaseList DataAll { get; set; }
        public BaseList EmployeeList { get; set; }
        public List<VS.Human.Rep.Model.MasterData> ContractTypes { get; set; }
        public List<string> StatusSuggestions { get; set; }

        public int TotalRecord
        {
            get
            {
                return DataAll.Total;
            }
        }

        public ContractModel(
            ILogger<ContractModel> logger,
            IContractBusiness contractBusiness,
            IEmpBusiness empBusiness,
            ImasterDataBussiness masterDataBussiness,
            IWebHostEnvironment hostingEnvironment)
        {
            _logger = logger;
            _contractBusiness = contractBusiness;
            _empBusiness = empBusiness;
            _masterDataBussiness = masterDataBussiness;
            _hostingEnvironment = hostingEnvironment;

            TitlePage = "Quan ly hop dong";
            KeyPage = "Contract";
            TableColumnText = new List<string>
            {
                "STT",
                "Nhan vien",
                "Loai hop dong",
                "Ngay bat dau",
                "Ngay ket thuc",
                "Trang thai",
                "Chu ky noi bo",
                "File",
                "Cap nhat",
                "Thao tac"
            };

            DataAll = new BaseList();
            RequestSearch = new ContractRequest();
            EmployeeList = new BaseList();
            ContractTypes = new List<VS.Human.Rep.Model.MasterData>();
            StatusSuggestions = new List<string>
            {
                "Active",
                "Expiring",
                "Expired",
                "Terminated"
            };
        }

        public async Task<ActionResult> OnGet([FromQuery] ContractRequest request)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();

            if (!Request.Query.ContainsKey("from"))
            {
                request.From = null;
            }
            if (!Request.Query.ContainsKey("to"))
            {
                request.To = null;
            }

            if (IsEmployeeSelfService())
            {
                request.EmployeeId = UserData.UserId;
                request.Token = string.Empty;
            }

            await LoadReferenceData(includeEmployees: !IsEmployeeSelfService());
            return await GetAll(request);
        }

        private async Task LoadReferenceData(bool includeEmployees = true)
        {
            ContractTypes = await _masterDataBussiness.GetallByTypeData(1);
            if (includeEmployees)
            {
                EmployeeList = await _empBusiness.GetAll(new EmployeeRequest
                {
                    Limit = 1000,
                    Page = 1
                });
            }
            else
            {
                EmployeeList = new BaseList { Data = new List<object>(), Total = 0 };
            }
        }

        public async Task<ActionResult> GetAll(ContractRequest request2)
        {
            RequestSearch = request2;
            DataAll = await _contractBusiness.GetAll(request2);
            return Page();
        }

        public async Task<IActionResult> OnGetFormEdit(int id = -1)
        {
            GetInfoUser();
            if (IsEmployeeSelfService())
            {
                return new JsonResult(new { success = false, message = "Access denied" })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            await LoadReferenceData();

            var contract = new ContractEntity
            {
                Id = id,
                Status = "Active"
            };

            if (id > 0)
            {
                var result = await _contractBusiness.GetById(id);
                if (result != null)
                {
                    contract = result;
                }
            }

            var viewModel = new
            {
                Contract = contract,
                ContractTypes,
                Employees = EmployeeList.Data,
                StatusSuggestions
            };

            return Partial("Contract/EditContract", viewModel);
        }

        public async Task<IActionResult> OnPostAdd([FromForm] ContractAdd request)
        {
            GetInfoUser();
            if (UserData.RoleCode == "2")
            {
                return new JsonResult(new { success = false, message = "Access denied" })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            var listError = new List<object>();
            if (request.EmployeeId <= 0)
            {
                listError.Add(new { name = "cbEmployeeId", Content = "Thieu thong tin nhan vien" });
            }
            if (string.IsNullOrWhiteSpace(request.ContractTypeCode))
            {
                listError.Add(new { name = "cbContractType", Content = "Thieu thong tin loai hop dong" });
            }
            if (request.Id < 1 && request.ContractFile == null)
            {
                listError.Add(new { name = "fileContract", Content = "Thieu file hop dong" });
            }
            if (request.Id > 0 && request.ContractFile != null)
            {
                listError.Add(new { name = "fileContract", Content = "File hop dong khong duoc thay doi" });
            }

            if (listError.Count > 0)
            {
                return new JsonResult(listError) { StatusCode = StatusCodes.Status400BadRequest };
            }

            string? fileUrl = null;
            if (request.Id < 1 && request.ContractFile != null)
            {
                var assetsPath = Path.Combine(_hostingEnvironment.WebRootPath, "assets", "contracts");
                if (!Directory.Exists(assetsPath))
                {
                    Directory.CreateDirectory(assetsPath);
                }

                var extension = Path.GetExtension(request.ContractFile.FileName);
                var fileName = $"contract_{request.EmployeeId}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}{extension}";
                var fullPath = Path.Combine(assetsPath, fileName);

                using (FileStream stream = new FileStream(fullPath, FileMode.Create))
                {
                    await request.ContractFile.CopyToAsync(stream);
                }

                fileUrl = $"/assets/contracts/{fileName}";
            }

            var contract = new ContractEntity
            {
                Id = request.Id,
                EmployeeId = request.EmployeeId,
                ContractTypeCode = request.ContractTypeCode,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = request.Status,
                Note = request.Note,
                FileUrl = fileUrl
            };

            var result = request.Id < 1
                ? await _contractBusiness.Add(contract, UserData.UserId)
                : await _contractBusiness.Update(contract, UserData.UserId);

            return new JsonResult(new { success = result }) { StatusCode = StatusCodes.Status200OK };
        }

        public async Task<IActionResult> OnGetSignForm(int id = -1)
        {
            GetInfoUser();
            var contract = await _contractBusiness.GetById(id) ?? new ContractEntity { Id = -1 };
            if (contract.Id < 1)
            {
                return new JsonResult(new { success = false, message = "Khong tim thay hop dong" })
                {
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            if (IsEmployeeSelfService() && !CanEmployeeAccessContract(contract))
            {
                return new JsonResult(new { success = false, message = "Ban khong co quyen xem hop dong nay" })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            var canSignHr = CanSignContractByHr(contract);
            var canSignEmployee = CanSignContractByEmployee(contract);

            var viewModel = new
            {
                Contract = contract,
                CanSignHr = canSignHr,
                CanSignEmployee = canSignEmployee
            };

            return Partial("Contract/SignContract", viewModel);
        }

        public async Task<IActionResult> OnPostSignInternal([FromForm] ContractSignRequest request)
        {
            GetInfoUser();

            var listError = new List<object>();
            if (request.Id < 1)
            {
                listError.Add(new { name = "txtSignPassword", Content = "Thieu hop dong can ky" });
            }
            if (string.IsNullOrWhiteSpace(request.PasswordConfirm))
            {
                listError.Add(new { name = "txtSignPassword", Content = "Nhap mat khau xac nhan" });
            }
            if (listError.Count > 0)
            {
                return new JsonResult(listError) { StatusCode = StatusCodes.Status400BadRequest };
            }

            var contract = await _contractBusiness.GetById(request.Id);
            if (contract == null || contract.Id < 1)
            {
                return new JsonResult(new { success = false, message = "Khong tim thay hop dong" })
                {
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            if (IsEmployeeSelfService() && !CanEmployeeAccessContract(contract))
            {
                return new JsonResult(new { success = false, message = "Ban khong co quyen ky hop dong nay" })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            if (contract.IsSignedInternal)
            {
                return new JsonResult(new { success = false, message = "Hop dong da duoc ky noi bo" })
                {
                    StatusCode = StatusCodes.Status409Conflict
                };
            }

            var contractFilePath = ResolveContractFilePath(contract.FileUrl);
            if (string.IsNullOrWhiteSpace(contractFilePath) || !global::System.IO.File.Exists(contractFilePath))
            {
                return new JsonResult(new { success = false, message = "Khong tim thay file hop dong de ky" })
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            var fileHash = await ComputeFileSha256Async(contractFilePath);
            var signedAt = DateTime.Now;
            var signatureHash = ComputeSignatureHash(contract, UserData.UserId, signedAt, fileHash, request.SignatureCode, request.SignNote);
            var signNote = BuildSignNote(request.SignatureCode, request.SignNote);

            if (IsPrivilegedSigner())
            {
                if (!CanSignContractByHr(contract))
                {
                    return new JsonResult(new { success = false, message = "Ban khong co quyen ky HR hop dong nay" })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                }

                var authUser = await _empBusiness.Login(UserData.UserName, request.PasswordConfirm ?? string.Empty);
                if (authUser == null || authUser.Id != UserData.UserId)
                {
                    return new JsonResult(new[] { new { name = "txtSignPassword", Content = "Mat khau xac nhan khong dung" } })
                    {
                        StatusCode = StatusCodes.Status400BadRequest
                    };
                }

                var signed = await _contractBusiness.SignInternalHr(
                    contract.Id,
                    UserData.UserId,
                    signedAt,
                    signatureHash,
                    fileHash,
                    signNote,
                    InternalSignMethod);

                if (!signed)
                {
                    return new JsonResult(new { success = false, message = "Khong the ky HR hop dong. Hop dong co the da duoc ky boi nguoi khac." })
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

            if (!CanSignContractByEmployee(contract))
            {
                var message = contract.IsHrSigned ? "Ban khong co quyen ky hop dong nay" : "Hop dong chua duoc HR ky";
                return new JsonResult(new { success = false, message })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            var authEmployee = await _empBusiness.Login(UserData.UserName, request.PasswordConfirm ?? string.Empty);
            if (authEmployee == null || authEmployee.Id != UserData.UserId)
            {
                return new JsonResult(new[] { new { name = "txtSignPassword", Content = "Mat khau xac nhan khong dung" } })
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            var signedEmployee = await _contractBusiness.SignInternal(
                contract.Id,
                UserData.UserId,
                signedAt,
                signatureHash,
                fileHash,
                signNote,
                InternalSignMethod);

            if (!signedEmployee)
            {
                return new JsonResult(new { success = false, message = "Khong the ky hop dong. Hop dong co the da duoc ky boi nguoi khac." })
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

        public async Task<IActionResult> OnPostDelete(int Id = -1)
        {
            GetInfoUser();
            if (UserData.RoleCode == "2")
            {
                return new JsonResult(new { success = false, message = "Access denied" })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            var listError = new List<object>();
            if (Id < 1)
            {
                listError.Add(new { name = "id", Content = "Thieu thong tin can xoa" });
            }

            if (listError.Count > 0)
            {
                return new JsonResult(listError) { StatusCode = StatusCodes.Status400BadRequest };
            }

            var result = await _contractBusiness.Delete(Id, UserData.UserId);
            return new JsonResult(new { success = result }) { StatusCode = StatusCodes.Status200OK };
        }

        public async Task<IActionResult> OnGetHistory(int contractId)
        {
            GetInfoUser();
            var contract = await _contractBusiness.GetById(contractId);
            if (contract == null || contract.Id < 1)
            {
                return new JsonResult(new { success = false, message = "Khong tim thay hop dong" })
                {
                    StatusCode = StatusCodes.Status404NotFound
                };
            }

            if (IsEmployeeSelfService() && !CanEmployeeAccessContract(contract))
            {
                return new JsonResult(new { success = false, message = "Ban khong co quyen xem lich su hop dong nay" })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            var history = await _contractBusiness.GetHistory(contractId);
            return Partial("Contract/History", history);
        }

        private bool IsEmployeeSelfService()
        {
            return UserData.RoleCode == "2";
        }

        private bool CanEmployeeAccessContract(ContractEntity contract)
        {
            return contract != null && contract.Id > 0 && contract.EmployeeId == UserData.UserId;
        }

        private bool CanSignContractByHr(ContractEntity contract)
        {
            if (contract == null || contract.Id < 1)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(contract.FileUrl))
            {
                return false;
            }

            if (contract.IsHrSigned)
            {
                return false;
            }

            return IsPrivilegedSigner();
        }

        private bool CanSignContractByEmployee(ContractEntity contract)
        {
            if (contract == null || contract.Id < 1)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(contract.FileUrl))
            {
                return false;
            }

            if (!contract.IsHrSigned || contract.IsSignedInternal)
            {
                return false;
            }

            return contract.EmployeeId == UserData.UserId;
        }

        private bool IsPrivilegedSigner()
        {
            return UserData.RoleCode == "1" || UserData.RoleCode == "9";
        }

        private string? ResolveContractFilePath(string? fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
            {
                return null;
            }

            var normalizedRelativePath = fileUrl
                .Replace('/', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);

            var webRoot = Path.GetFullPath(_hostingEnvironment.WebRootPath);
            var fullPath = Path.GetFullPath(Path.Combine(webRoot, normalizedRelativePath));
            if (!fullPath.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return fullPath;
        }

        private static async Task<string> ComputeFileSha256Async(string filePath)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var sha = SHA256.Create();
            var hashBytes = await sha.ComputeHashAsync(stream);
            return Convert.ToHexString(hashBytes);
        }

        private static string ComputeSignatureHash(ContractEntity contract, int signedBy, DateTime signedAt, string fileHash, string? signatureCode, string? signNote)
        {
            var payload = string.Join("|", new[]
            {
                contract.Id.ToString(),
                contract.EmployeeId.ToString(),
                signedBy.ToString(),
                signedAt.ToString("O"),
                fileHash,
                (signatureCode ?? string.Empty).Trim(),
                (signNote ?? string.Empty).Trim()
            });

            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hashBytes);
        }

        private static string? BuildSignNote(string? signatureCode, string? signNote)
        {
            var code = (signatureCode ?? string.Empty).Trim();
            var note = (signNote ?? string.Empty).Trim();

            var normalized = string.Empty;
            if (!string.IsNullOrWhiteSpace(code))
            {
                normalized = $"CODE:{code}";
            }
            if (!string.IsNullOrWhiteSpace(note))
            {
                normalized = string.IsNullOrEmpty(normalized)
                    ? note
                    : $"{normalized}; NOTE:{note}";
            }

            if (string.IsNullOrEmpty(normalized))
            {
                return null;
            }

            if (normalized.Length > 500)
            {
                return normalized.Substring(0, 500);
            }

            return normalized;
        }
    }
}
