using Microsoft.AspNetCore.Http;
using VS.Human.Business.Model;

namespace VS.Human.Business
{
    public interface IEmployeeImportBusiness
    {
        Task<EmployeeImportResult> ImportAsync(IFormFile file, int userId);
    }

    public class EmployeeImportResult
    {
        public int Total { get; set; }
        public int TotalSuccess { get; set; }
        public int TotalError { get; set; }
        public List<EmployeeImportError> Errors { get; set; } = new List<EmployeeImportError>();
    }

    public class EmployeeImportError
    {
        public int Row { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}

