using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using VS.Human.Business.Helpers;
using VS.Human.Business.Model;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class EmployeeImportBusiness : BaseBusiness, IEmployeeImportBusiness
    {
        private readonly IEmpBusiness _empBusiness;

        public EmployeeImportBusiness(
            IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor,
            IEmpBusiness empBusiness)
            : base(unitOfWork, httpContextAccessor)
        {
            _empBusiness = empBusiness;
        }

        public async Task<EmployeeImportResult> ImportAsync(IFormFile file, int userId)
        {
            var result = new EmployeeImportResult();

            try
            {
                using var stream = file.OpenReadStream();
                using var document = SpreadsheetDocument.Open(stream, false);

                var workbookPart = document.WorkbookPart;
                var worksheetPart = workbookPart?.WorksheetParts.FirstOrDefault();
                if (worksheetPart == null)
                    return ErrorResult(result, "File Excel không hợp lệ");

                var sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();
                var rows = sheetData.Elements<Row>().ToList();

                if (rows.Count < 2)
                    return ErrorResult(result, "File Excel không có dữ liệu");

                int dataStartIndex = FindDataStartRow(rows, workbookPart);
                if (dataStartIndex == -1)
                    return ErrorResult(result, "Không tìm thấy dòng dữ liệu (Cột 0: STT, Cột 1: Họ tên)");

                for (int i = dataStartIndex; i < rows.Count; i++)
                {
                    await ProcessRow(rows[i], i + 1, workbookPart, userId, result);
                }
            }
            catch (Exception ex)
            {
                return ErrorResult(result, $"Lỗi đọc file Excel: {ex.Message}");
            }

            return result;
        }

        private async Task ProcessRow(Row row, int rowIndex, WorkbookPart workbookPart, int userId, EmployeeImportResult result)
        {
            result.Total++;
            try
            {
                var values = ExcelHelper.GetRowValues(row, workbookPart).ToList();
                if (values.All(string.IsNullOrWhiteSpace)) return;

                var employee = CreateEmployeeFromRow(values);
                var validationError = EmployeeImportValidator.ValidateRow(EmployeeMapper.MapToEmployeeInfoAdd(employee));

                if (validationError != null)
                {
                    AddError(result, rowIndex, validationError);
                    return;
                }

                await SaveEmployee(employee, userId, rowIndex, result);
            }
            catch (Exception ex)
            {
                AddError(result, rowIndex, $"Lỗi xử lý: {ex.Message}");
            }
        }

        private async Task SaveEmployee(Employee employee, int userId, int rowIndex, EmployeeImportResult result)
        {
            employee.CreatedBy = userId;
            employee.CreateAt = DateTime.Now;
            employee.Id = -1;
            employee.UserName = string.IsNullOrEmpty(employee.UserName) 
                ? EmployeeMapper.GenerateUserName(employee.Email, employee.Phone, employee.FullName) 
                : employee.UserName;
            employee.Pass = string.IsNullOrEmpty(employee.Pass) ? "Vietstar@2024" : employee.Pass;
            
            // Set default status
            employee.Status = 1;
            employee.IsActive = 1;

            var employeeInfoAdd = EmployeeMapper.MapToEmployeeInfoAdd(employee);
            employeeInfoAdd.CreatedBy = userId;
            employeeInfoAdd.CreateAt = DateTime.Now;

            if (await _empBusiness.Add(employeeInfoAdd))
            {
                result.TotalSuccess++;
            }
            else
            {
                AddError(result, rowIndex, "Không thể thêm nhân viên vào database");
            }
        }

        private Employee CreateEmployeeFromRow(List<string> values)
        {
            string Get(int index) => index < values.Count ? values[index]?.Trim() ?? "" : "";

            var employee = new Employee
            {
                FullName = Get(1),
                Phone = Get(18),
                PositionCode = Get(3),
                DepartmentCode = Get(4),
                Gender = NormalizeGender(Get(5)),
                PlaceOfBirth = Get(7),
                Religion = Get(8),
                EducationLevel = Get(9),
                Maritalstatus = Get(10),
                NationalId = Get(11),
                NationalPlace = Get(13),
                PermanentAddress = Get(14),
                TemporaryAddress = Get(15),
                Email = Get(16),
                PersonalEmail = Get(17),
                EmergencyContact = Get(19),
                BeneficiaryName = Get(20),
                BankAccount = Get(21),
                BankName = Get(22)
            };

            if (ExcelHelper.TryParseDate(Get(2), out var onboard)) employee.Onboard = onboard;
            if (ExcelHelper.TryParseDate(Get(6), out var dob)) employee.Dob = dob;
            if (ExcelHelper.TryParseDate(Get(12), out var nationalDate)) employee.NationalDate = nationalDate;

            return employee;
        }

        private string NormalizeGender(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "0";
            
            input = input.Trim().ToLower();
            if (input == "nam" || input == "trai" || input == "male" || input == "m" || input == "1") return "1";
            if (input == "nữ" || input == "nu" || input == "gái" || input == "gai" || input == "female" || input == "f" || input == "2") return "2";
            
            return "0"; // Default to Unknown
        }

        private int FindDataStartRow(List<Row> rows, WorkbookPart workbookPart)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                var values = ExcelHelper.GetRowValues(rows[i], workbookPart).ToList();
                if (values.Count > 1)
                {
                    var col0 = values[0]?.Trim() ?? "";
                    var col1 = values[1]?.Trim() ?? "";
                    if (int.TryParse(col0, out _) && !string.IsNullOrWhiteSpace(col1) 
                        && !col1.Contains("Full Name") && !col1.Contains("Tên nhân viên"))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        private EmployeeImportResult ErrorResult(EmployeeImportResult result, string message)
        {
            result.Errors.Add(new EmployeeImportError { Row = 0, Content = message });
            result.TotalError++;
            return result;
        }

        private void AddError(EmployeeImportResult result, int row, string message)
        {
            result.TotalError++;
            result.Errors.Add(new EmployeeImportError { Row = row, Content = message });
        }
    }
}
