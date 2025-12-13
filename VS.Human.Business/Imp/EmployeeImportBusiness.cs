using System;
using System.Linq;
using System.Transactions;
using System.Text.RegularExpressions;
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
            var transactionOptions = new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted
            };

            // Ensure the whole import is atomic; any row error will rollback previous inserts
            using var scope = new TransactionScope(TransactionScopeOption.Required, transactionOptions, TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                using var stream = file.OpenReadStream();
                using var document = SpreadsheetDocument.Open(stream, false);

                var workbookPart = document.WorkbookPart;
                var worksheetPart = workbookPart?.WorksheetParts.FirstOrDefault();
                if (worksheetPart == null)
                    return ErrorResult(result, "File Excel khong hop le");

                var sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();
                var rows = sheetData.Elements<Row>().ToList();

                if (rows.Count < 2)
                    return ErrorResult(result, "File Excel khong co du lieu");

                int dataStartIndex = FindDataStartRow(rows, workbookPart);
                if (dataStartIndex == -1)
                    return ErrorResult(result, "Khong tim thay dung du lieu (Cot 0: STT, Cot 1: Ho ten)");

                for (int i = dataStartIndex; i < rows.Count; i++)
                {
                    await ProcessRow(rows[i], i + 1, workbookPart, userId, result);
                }
            }
            catch (Exception ex)
            {
                return ErrorResult(result, $"Loi doc file Excel: {ex.Message}");
            }

            if (result.TotalError == 0)
            {
                scope.Complete();
            }
            else
            {
                result.TotalSuccess = 0;
                if (!result.Errors.Any(e => e.Row == 0))
                {
                    result.Errors.Insert(0, new EmployeeImportError
                    {
                        Row = 0,
                        Content = "Import failed, all changes were rolled back."
                    });
                    result.TotalError = result.Errors.Count;
                }
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

                var rawPosition = values.Count > 3 ? values[3] : string.Empty;
                var rawDepartment = values.Count > 4 ? values[4] : string.Empty;
                var rawEducation = values.Count > 9 ? values[9] : string.Empty;
                var rawMarital = values.Count > 10 ? values[10] : string.Empty;
                var rawReligion = values.Count > 8 ? values[8] : string.Empty;
                var contractData = CreateContractData(values);
                var insuranceData = CreateInsuranceData(values);

                var positionCode = await EnsureMasterDataAsync(rawPosition, 2, userId);    // TypeData 2 = Position
                var departmentCode = await EnsureMasterDataAsync(rawDepartment, 5, userId); // TypeData 5 = Department
                var educationCode = await EnsureMasterDataAsync(rawEducation, 14, userId);  // TypeData 14 = Education level
                var maritalCode = await EnsureMasterDataAsync(rawMarital, 13, userId);      // TypeData 13 = Marital status
                var religionCode = await EnsureMasterDataAsync(rawReligion, 20, userId);    // TypeData 10 = Religion

                if (!string.IsNullOrWhiteSpace(contractData.ContractTypeRaw))
                {
                    // TypeData 1 = Loại HĐ (master data)
                    contractData.ContractTypeCode = await EnsureMasterDataAsync(contractData.ContractTypeRaw, 1, userId);
                }

                var employee = CreateEmployeeFromRow(values, positionCode, departmentCode, educationCode, maritalCode, religionCode);
                var validationError = EmployeeImportValidator.ValidateRow(EmployeeMapper.MapToEmployeeInfoAdd(employee));

                if (validationError != null)
                {
                    AddError(result, rowIndex, validationError);
                    return;
                }

                await SaveEmployee(employee, contractData, insuranceData, userId, rowIndex, result);
            }
            catch (Exception ex)
            {
                AddError(result, rowIndex, $"Loi xu ly: {ex.Message}");
            }
        }

        private async Task SaveEmployee(Employee employee, ContractRowData contractData, InsuranceRowData insuranceData, int userId, int rowIndex, EmployeeImportResult result)
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

            var createdEmployee = await _empBusiness.Add(employeeInfoAdd);
            if (createdEmployee != null && createdEmployee.Id > 0)
            {
                result.TotalSuccess++;
                await TrySaveContractAsync(createdEmployee, contractData, userId, rowIndex, result);
                await TrySaveInsuranceAsync(createdEmployee, insuranceData, userId, rowIndex, result);
            }
            else
            {
                AddError(result, rowIndex, "Khong them nhan vien vao database");
            }
        }

        private ContractRowData CreateContractData(List<string> values)
        {
            string Get(int index) => index < values.Count ? values[index]?.Trim() ?? "" : "";

            var data = new ContractRowData
            {
                NoAgree = Get(29),
                CodeId = Get(29),
                ContractTypeRaw = Get(30)
            };

            if (ExcelHelper.TryParseDate(Get(31), out var start)) data.Start = start;
            if (ExcelHelper.TryParseDate(Get(32), out var end)) data.End = end;

            return data;
        }

        private InsuranceRowData CreateInsuranceData(List<string> values)
        {
            string Get(int index) => index < values.Count ? values[index]?.Trim() ?? "" : "";
            var data = new InsuranceRowData
            {
                PITDateRaw = Get(24),
                Dependent = Get(25),
                NumberCode = Get(27),   // Mã BHXH/BHYT
                RegHospital = Get(28)   // Tên bệnh viện
            };

            if (ExcelHelper.TryParseDate(Get(26), out var effected)) data.EffectedFrom = effected; // EffectedFrom ở cột 26
            if (ExcelHelper.TryParseDate(data.PITDateRaw, out var pit)) data.PITDate = pit;
            return data;
        }

        private async Task TrySaveContractAsync(Employee employee, ContractRowData contractData, int userId, int rowIndex, EmployeeImportResult result)
        {
            if (!contractData.HasData) return;

            var hdld = new HDLD
            {
                NoAgree = contractData.NoAgree ?? string.Empty,
                CodeId = contractData.ContractTypeCode ?? contractData.CodeId ?? string.Empty,
                Start = contractData.Start,
                End = contractData.End,
                UserId = employee.UserName,
                CreatedBy = userId,
                UpdatedBy = userId
            };

            var saved = await _unitOfWork.HDLDItemRep.AddOrUpdate(hdld);
            if (!saved)
            {
                AddError(result, rowIndex, "Khong luu duoc thong tin hop dong (HDLD)");
            }
        }

        private async Task TrySaveInsuranceAsync(Employee employee, InsuranceRowData insuranceData, int userId, int rowIndex, EmployeeImportResult result)
        {
            if (!insuranceData.HasData) return;

            var bhxh = await _unitOfWork.BHXHItemRep.GetInfo(employee.UserName) ?? new BHXHItem();
            bhxh.UserName = employee.UserName;
            bhxh.Relid = employee.Id.ToString();
            bhxh.NumberCode = insuranceData.NumberCode ?? bhxh.NumberCode;
            bhxh.EffectedFrom = insuranceData.EffectedFrom ?? bhxh.EffectedFrom;
            bhxh.PITDate = insuranceData.PITDate ?? bhxh.PITDate;
            bhxh.Dependent = string.IsNullOrWhiteSpace(insuranceData.Dependent) ? bhxh.Dependent : insuranceData.Dependent;
            bhxh.CreatedBy = bhxh.CreatedBy == 0 ? userId : bhxh.CreatedBy;
            bhxh.UpdatedBy = userId;

            var saved = await _unitOfWork.BHXHItemRep.AddOrUpdate(bhxh);
            if (!saved)
            {
                AddError(result, rowIndex, "Khong luu duoc thong tin BHXH/BHYT");
                return;
            }

            if (!string.IsNullOrWhiteSpace(insuranceData.RegHospital))
            {
                var tax = await _unitOfWork.TaxtItemRep.GetInfo(employee.UserName) ?? new TaxItem();
                tax.UserName = employee.UserName;
                tax.CodeId = string.IsNullOrWhiteSpace(tax.CodeId) ? employee.Id.ToString() : tax.CodeId;
                tax.RegBHYT = insuranceData.RegHospital;
                tax.CreatedBy = tax.CreatedBy == 0 ? userId : tax.CreatedBy;
                tax.UpdatedBy = userId;

                var savedTax = await _unitOfWork.TaxtItemRep.AddOrUpdate(tax);
                if (!savedTax)
                {
                    AddError(result, rowIndex, "Khong luu duoc thong tin RegBHYT");
                }
            }
        }

        private class ContractRowData
        {
            public string? NoAgree { get; set; }
            public string? CodeId { get; set; }
            public string? ContractTypeRaw { get; set; }
            public string? ContractTypeCode { get; set; }
            public DateTime? Start { get; set; }
            public DateTime? End { get; set; }

            public bool HasData =>
                !string.IsNullOrWhiteSpace(NoAgree) ||
                !string.IsNullOrWhiteSpace(CodeId) ||
                !string.IsNullOrWhiteSpace(ContractTypeRaw) ||
                Start.HasValue ||
                End.HasValue;
        }

        private class InsuranceRowData
        {
            public string? PITDateRaw { get; set; }
            public string? NumberCode { get; set; }    // Mã BHXH/BHYT
            public string? RegHospital { get; set; }   // Tên bệnh viện (tạm lưu ChungTuThue)
            public DateTime? EffectedFrom { get; set; }
            public DateTime? PITDate { get; set; }
            public string? Dependent { get; set; }

            public bool HasData =>
                !string.IsNullOrWhiteSpace(NumberCode) ||
                !string.IsNullOrWhiteSpace(RegHospital) ||
                EffectedFrom.HasValue ||
                PITDate.HasValue ||
                !string.IsNullOrWhiteSpace(Dependent);
        }

        private Employee CreateEmployeeFromRow(List<string> values, string? positionCode, string? departmentCode, string? educationCode, string? maritalCode, string? religionCode)
        {
            string Get(int index) => index < values.Count ? values[index]?.Trim() ?? "" : "";

            var employee = new Employee
            {
                FullName = Get(1),
                Phone = Get(18),
                PositionCode = positionCode ?? NormalizeCode(Get(3)),
                DepartmentCode = departmentCode ?? NormalizeCode(Get(4)),
                Gender = NormalizeGender(Get(5)),
                PlaceOfBirth = Get(7),
                Religion = religionCode ?? Get(8),
                EducationLevel = educationCode ?? Get(9),
                Maritalstatus = maritalCode ?? Get(10),
                NationalId = Get(11),
                NationalPlace = Get(13),
                PermanentAddress = Get(14),
                TemporaryAddress = Get(15),
                Email = Get(16),
                PersonalEmail = Get(17),
                EmergencyContact = Get(19),
                BeneficiaryName = Get(20),
                BankAccount = Get(21),
                BankName = Get(22),
                Noted = Get(33)
            };

            if (ExcelHelper.TryParseDate(Get(2), out var onboard)) employee.Onboard = onboard;
            if (ExcelHelper.TryParseDate(Get(6), out var dob)) employee.Dob = dob;
            if (ExcelHelper.TryParseDate(Get(12), out var nationalDate)) employee.NationalDate = nationalDate;

            return employee;
        }

        private async Task<string?> EnsureMasterDataAsync(string rawValue, int typeData, int userId)
        {
            var trimmed = rawValue?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmed)) return null;

            var normalized = NormalizeCode(trimmed);

            // Try by code first (for numeric codes)
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                var existingByCode = await _unitOfWork.MasterDataRep.GetByCode(normalized, typeData);
                if (existingByCode != null && existingByCode.Id > 0) return existingByCode.Code;
            }

            // Try by exact name match
            var existingByName = await _unitOfWork.MasterDataRep.GetByName(trimmed, typeData);
            if (existingByName != null && existingByName.Id > 0) return existingByName.Code;

            // Insert new master data
            var newItem = new MasterDataAdd
            {
                Name = trimmed,
                TypeData = typeData,
                IsActive = 1,
                CreatedBy = userId
            };

            if (await _unitOfWork.MasterDataRep.AddOrUpdate(newItem))
            {
                var inserted = await _unitOfWork.MasterDataRep.GetByName(trimmed, typeData);
                if (inserted != null && inserted.Id > 0) return inserted.Code;
            }

            // Fallback to normalized numeric code (if any)
            return normalized;
        }

        private string? NormalizeCode(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var digits = new string(input.Where(char.IsDigit).ToArray());
            return string.IsNullOrWhiteSpace(digits) ? null : digits;
        }

        private string NormalizeGender(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "0";
            
            input = input.Trim().ToLower();
            if (input == "nam" || input == "trai" || input == "male" || input == "m" || input == "1") return "1";
            if (input == "nu" || input == "n\u1eef" || input == "gai" || input == "g\u00e1i" || input == "female" || input == "f" || input == "2") return "2";
            
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
                        && !col1.Contains("Full Name", StringComparison.OrdinalIgnoreCase) 
                        && !col1.Contains("Ten nhan vien", StringComparison.OrdinalIgnoreCase))
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
