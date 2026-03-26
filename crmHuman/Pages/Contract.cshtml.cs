using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
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
        private const string HrInternalSignMethod = "HR_CONTRACT_INTERNAL_SHA256";
        private const string EmployeeInternalSignMethod = "EMPLOYEE_CONTRACT_INTERNAL_SHA256";
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

            if (!IsPrivilegedSigner())
            {
                if (!request.AcceptTerms)
                {
                    listError.Add(new { name = "cbContractAcceptTerms", Content = "Ban can chap nhan dieu khoan truoc khi ky" });
                }
                if (string.IsNullOrWhiteSpace(request.SignatureDataUrl))
                {
                    listError.Add(new { name = "contractSignatureCanvas", Content = "Nhan vien can ve chu ky truoc khi ky hop dong" });
                }
                if (listError.Count > 0)
                {
                    return new JsonResult(listError) { StatusCode = StatusCodes.Status400BadRequest };
                }
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
            var signedIpAddress = GetRequestIpAddress();
            var signedUserAgent = GetRequestUserAgent();

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

                var signatureHash = ComputeSignatureHash(
                    contract,
                    UserData.UserId,
                    UserData.UserName,
                    UserData.FullName,
                    signedIpAddress,
                    signedUserAgent,
                    null,
                    null,
                    signedAt,
                    fileHash,
                    null,
                    request.SignatureCode,
                    request.SignNote);
                var signNote = BuildSignNote(null, request.SignatureCode, request.SignNote);

                var signed = await _contractBusiness.SignInternalHr(
                    contract.Id,
                    UserData.UserId,
                    signedAt,
                    signatureHash,
                    fileHash,
                    signNote,
                    HrInternalSignMethod,
                    UserData.UserName,
                    UserData.FullName,
                    signedIpAddress,
                    signedUserAgent);

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

            var signatureImageBytes = DecodeSignatureDataUrl(request.SignatureDataUrl);
            if (signatureImageBytes == null || signatureImageBytes.Length == 0)
            {
                return new JsonResult(new[] { new { name = "contractSignatureCanvas", Content = "Chu ky ve tay khong hop le" } })
                {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }

            var signatureImagePath = await SaveSignatureImageAsync(contract, signatureImageBytes, signedAt, fileHash);
            var signatureImageHash = ComputeSha256(signatureImageBytes);
            var signedFileArchivePath = await ArchiveSignedContractAsync(contract, contractFilePath, signatureImageBytes, signedAt, fileHash);
            var signatureIntentText = BuildContractSignatureIntentText();
            var employeeSignatureHash = ComputeSignatureHash(
                contract,
                UserData.UserId,
                UserData.UserName,
                UserData.FullName,
                signedIpAddress,
                signedUserAgent,
                signedFileArchivePath,
                signatureImageHash,
                signedAt,
                fileHash,
                signatureIntentText,
                request.SignatureCode,
                request.SignNote);
            var employeeSignNote = BuildSignNote(signatureIntentText, request.SignatureCode, request.SignNote);

            var signedEmployee = await _contractBusiness.SignInternal(
                contract.Id,
                UserData.UserId,
                signedAt,
                employeeSignatureHash,
                fileHash,
                employeeSignNote,
                EmployeeInternalSignMethod,
                UserData.UserName,
                UserData.FullName,
                signedIpAddress,
                signedUserAgent,
                signedFileArchivePath,
                signatureImagePath,
                signatureIntentText,
                signedAt);

            if (!signedEmployee)
            {
                DeleteArchivedFileIfExists(signedFileArchivePath);
                DeleteArchivedFileIfExists(signatureImagePath);
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

        private static string ComputeSignatureHash(ContractEntity contract, int signedBy, string? signedByUserName, string? signedByFullName, string? signedIpAddress, string? signedUserAgent, string? signedFileArchivePath, string? signatureImageHash, DateTime signedAt, string fileHash, string? signatureIntentText, string? signatureCode, string? signNote)
        {
            var payload = string.Join("|", new[]
            {
                contract.Id.ToString(),
                contract.EmployeeId.ToString(),
                signedBy.ToString(),
                (signedByUserName ?? string.Empty).Trim(),
                (signedByFullName ?? string.Empty).Trim(),
                (signedIpAddress ?? string.Empty).Trim(),
                (signedUserAgent ?? string.Empty).Trim(),
                (signedFileArchivePath ?? string.Empty).Trim(),
                (signatureImageHash ?? string.Empty).Trim(),
                signedAt.ToString("O"),
                fileHash,
                (signatureIntentText ?? string.Empty).Trim(),
                (signatureCode ?? string.Empty).Trim(),
                (signNote ?? string.Empty).Trim()
            });

            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hashBytes);
        }

        private static string BuildContractSignatureIntentText()
        {
            return "Toi xac nhan chinh toi la nguoi ky, da doc, hieu, dong y voi noi dung hop dong va dong y ky dien tu noi bo cho hop dong nay.";
        }

        private static string? BuildSignNote(string? signatureIntentText, string? signatureCode, string? signNote)
        {
            var code = (signatureCode ?? string.Empty).Trim();
            var note = (signNote ?? string.Empty).Trim();

            var normalized = string.Empty;
            if (!string.IsNullOrWhiteSpace(signatureIntentText))
            {
                normalized = $"INTENT:{signatureIntentText.Trim()}";
            }
            if (!string.IsNullOrWhiteSpace(code))
            {
                normalized = string.IsNullOrEmpty(normalized)
                    ? $"CODE:{code}"
                    : $"{normalized}; CODE:{code}";
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

        private async Task<string?> SaveSignatureImageAsync(ContractEntity contract, byte[] signatureImageBytes, DateTime signedAt, string fileHash)
        {
            var relativePath = $"/signed-archive/contract-signatures/{signedAt:yyyy}/{signedAt:MM}/sign-{contract.Id}-emp-{contract.EmployeeId}-{signedAt:yyyyMMddHHmmss}-{fileHash.Substring(0, Math.Min(12, fileHash.Length))}.png";
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

        private async Task<string?> ArchiveSignedContractAsync(ContractEntity contract, string sourceFilePath, byte[] signatureImageBytes, DateTime signedAt, string fileHash)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath) || !global::System.IO.File.Exists(sourceFilePath))
            {
                return null;
            }

            var extension = global::System.IO.Path.GetExtension(sourceFilePath);
            var archiveRelativePath = $"/signed-archive/contracts/{signedAt:yyyy}/{signedAt:MM}/contract-{contract.Id}-emp-{contract.EmployeeId}-{signedAt:yyyyMMddHHmmss}-{fileHash.Substring(0, Math.Min(12, fileHash.Length))}{extension}";
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

            try
            {
                var fullPath = ResolveArchiveAbsolutePath(archiveRelativePath);
                if (global::System.IO.File.Exists(fullPath))
                {
                    var fileInfo = new global::System.IO.FileInfo(fullPath);
                    if (fileInfo.IsReadOnly)
                    {
                        fileInfo.IsReadOnly = false;
                    }

                    global::System.IO.File.Delete(fullPath);
                }
            }
            catch
            {
            }
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
    }
}
