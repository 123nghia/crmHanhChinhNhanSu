using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace crmHuman.Pages.Cloud
{
    [Authorize]
    public class StorageModel : BaseModel2
    {
        private readonly IDocumentDataBussiness _documentBusiness;
        private readonly IEmpBusiness _empBusiness;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public BaseList Documents { get; set; }
        public int? CurrentParentId { get; set; }
        public List<DocumentDataIndexModel> Breadcrumbs { get; set; } = new List<DocumentDataIndexModel>();
        public BaseList AllEmployees { get; set; }

        public StorageModel(
            IDocumentDataBussiness documentBusiness,
            IEmpBusiness empBusiness,
            IWebHostEnvironment hostingEnvironment)
        {
            _documentBusiness = documentBusiness;
            _empBusiness = empBusiness;
            _hostingEnvironment = hostingEnvironment;
            TitlePage = "Kho tài liệu trực tuyến";
        }

        public async Task OnGetAsync(int? parentId)
        {
            GetInfoUser();
            CurrentParentId = parentId;
            var userId = UserData.UserId;
            
            Documents = await _documentBusiness.GetDocuments(parentId, userId);

            // Build breadcrumbs
            if (parentId.HasValue)
            {
                var current = await _documentBusiness.GetById(parentId.Value);
                while (current != null)
                {
                    Breadcrumbs.Insert(0, new DocumentDataIndexModel 
                    { 
                        Id = current.Id, 
                        DisplayText = current.DisplayText 
                    });
                    if (current.ParentId.HasValue)
                        current = await _documentBusiness.GetById(current.ParentId.Value);
                    else
                        break;
                }
            }

            // For sharing modal
            AllEmployees = await _empBusiness.GetAll(new EmployeeRequest { Limit = 1000 });
        }

        public async Task<IActionResult> OnPostCreateFolderAsync(string name, int? parentId)
        {
            GetInfoUser();
            var userId = UserData.UserId;
            var result = await _documentBusiness.CreateFolder(name, parentId, userId);
            return new JsonResult(new { success = result > 0 });
        }

        public async Task<IActionResult> OnPostUploadFileAsync(IFormFile file, int? parentId)
        {
            if (file == null || file.Length == 0)
                return new JsonResult(new { success = false, message = "No file selected." });

            GetInfoUser();
            var userId = UserData.UserId;
            var folderPath = Path.Combine(_hostingEnvironment.WebRootPath, "uploads", "cloud", userId.ToString());
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"/uploads/cloud/{userId}/{fileName}";
            var result = await _documentBusiness.SaveFile(file.FileName, relativePath, parentId, userId);

            return new JsonResult(new { success = result > 0, path = relativePath });
        }

        public async Task<IActionResult> OnGetSharesAsync(int id)
        {
            var shares = await _documentBusiness.GetShares(id);
            return new JsonResult(shares);
        }

        public async Task<IActionResult> OnPostUpdateSharesAsync(int id, [FromBody] List<int> userIds)
        {
            var result = await _documentBusiness.UpdateShares(id, userIds);
            return new JsonResult(new { success = result });
        }

        public async Task<IActionResult> OnPostUpdateAccessLevelAsync(int id, int accessLevel)
        {
            GetInfoUser();
            var userId = UserData.UserId;
            var result = await _documentBusiness.UpdateAccessLevel(id, accessLevel, userId);
            return new JsonResult(new { success = result });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            GetInfoUser();
            var userId = UserData.UserId;
            // Check ownership if needed: var item = await _documentBusiness.GetById(id);
            var result = await _documentBusiness.Delete(id);
            return new JsonResult(new { success = result });
        }
    }
}
