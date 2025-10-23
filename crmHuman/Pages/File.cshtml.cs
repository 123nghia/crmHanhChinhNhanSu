using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Item;

namespace crmHuman.Pages
{
    [Authorize]
    public class FileModel : BaseModel2
    {
        private readonly ILogger<FileModel> _logger;
        private readonly IEmpBusiness _empBusiness;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _hostingEnvironment;
        public List<string> TableColumnTextAdmin { get; set; }
        public EmployeeRequest RequestSearch { get; set; }
        public BaseList DataAll { get; set; }



        public int TotalRecord
        {

            get
            {
                return DataAll.Total;

            }
        }
        public FileModel(ILogger<FileModel> logger,
            IEmpBusiness empBusiness,
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment hostingEnvironment
            )
        {
            _logger = logger;
            _empBusiness = empBusiness;
            TitlePage = "Danh sách nhân viên";
            KeyPage = "Employee";

            TableColumnText = new List<string>()
            {
                "STT","Họ tên","Tài khoản","Vai trò", "Ngày Onboard","Cập nhật gần nhất","Thao tác"
            };

            TableColumnTextAdmin = new List<string>()
            {
                "STT","Họ tên","Tài khoản","Vai trò","Trạng thái", "Ngày Onboard","Cập nhật gần nhất","Thao tác"
            };
            _hostingEnvironment = hostingEnvironment;

        }

        public async Task<IActionResult> OnPostUpload
         (UploadFileAdd request)
        {
            var listEror = new List<object>();
            var fileRequest = request.FileRequest;
            if (string.IsNullOrEmpty(fileRequest.FileName))
            {
                var itemError = new
                {
                    name = "txtFileRequest",
                    Content = "Thiếu thông tin file "
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
            var path = Path.Combine(_hostingEnvironment.WebRootPath, "assets/", "CV/", request.Type, fileRequest.FileName);
            var pathfolder = Path.Combine(_hostingEnvironment.WebRootPath, "assets", "CV", request.Type);


            bool folderExists = Directory.Exists(pathfolder);
            if (!folderExists)

            {
                Directory.CreateDirectory(pathfolder);
            }


            var fileName = request.Type + "" + DateTime.Now.Second.ToString() + new Random().Next(100) + Path.GetExtension(fileRequest.FileName);
            var pathFile = Path.Combine(pathfolder, fileName);
            using (FileStream stream = new FileStream(pathFile, FileMode.Create))
            {
                await fileRequest.CopyToAsync(stream);
                stream.Close();
            }
            var linkResult = "/assets/CV/" + request.Type + "/" + fileName;
            var dataReponse = new
            {
                success = true,
                linkResult
            };
            return new JsonResult(dataReponse)
            {
                StatusCode = StatusCodes.Status200OK

            };
        }

    }
}
