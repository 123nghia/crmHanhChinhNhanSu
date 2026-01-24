using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
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
            if (UserData.RoleCode == "2")
            {
                return Redirect("/");
            }

            if (!Request.Query.ContainsKey("from"))
            {
                request.From = null;
            }
            if (!Request.Query.ContainsKey("to"))
            {
                request.To = null;
            }

            await LoadReferenceData();
            return await GetAll(request);
        }

        private async Task LoadReferenceData()
        {
            ContractTypes = await _masterDataBussiness.GetallByTypeData(1);
            EmployeeList = await _empBusiness.GetAll(new EmployeeRequest
            {
                Limit = 1000,
                Page = 1
            });
        }

        public async Task<ActionResult> GetAll(ContractRequest request2)
        {
            RequestSearch = request2;
            DataAll = await _contractBusiness.GetAll(request2);
            return Page();
        }

        public async Task<PartialViewResult> OnGetFormEdit(int id = -1)
        {
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

        public async Task<PartialViewResult> OnGetHistory(int contractId)
        {
            var history = await _contractBusiness.GetHistory(contractId);
            return Partial("Contract/History", history);
        }
    }
}
