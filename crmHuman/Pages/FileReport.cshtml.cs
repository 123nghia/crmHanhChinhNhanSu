using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Item;

namespace crmHuman.Pages
{
    [Authorize]
    public class FileReport : BaseModel2
    {
        private readonly ILogger<FileReport> _logger;
        private readonly IEmpBusiness _empBusiness;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _hostingEnvironment;
        public List<string> TableColumnTextAdmin { get; set; }
        public EmployeeRequest RequestSearch { get; set; }
        private readonly IExportFileBussiness _exportFileBussiness;


        public FileReport(ILogger<FileReport> logger,
            IEmpBusiness empBusiness,
            IExportFileBussiness exportFileBussiness,
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment hostingEnvironment
            )
        {
            _logger = logger;
            _empBusiness = empBusiness;
            _hostingEnvironment = hostingEnvironment;
            _exportFileBussiness = exportFileBussiness;

        }

        public async Task<IActionResult> OnPostFileDashboard
         (OrderRequest request)
        {
            var listEror = new List<object>();


            var path = "";
            var pathfolder = "";

            GetInfoUser();


            request.RoleCode = UserData.RoleCode;
            request.UserId = UserData.UserId;
            request.UserName = UserData.UserName;
            var pathfile = await _exportFileBussiness.ExportFileDashboard(request);

            var linkResult = pathfile;
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
