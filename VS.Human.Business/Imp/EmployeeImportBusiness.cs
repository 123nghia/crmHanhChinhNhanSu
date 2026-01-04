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

                int headerRowIndex = FindHeaderRow(rows, workbookPart);
                if (headerRowIndex == -1)
                    return ErrorResult(result, "Khong tim thay dong tieu de (header) hop le.");

                var headerMap = GetHeaderMap(rows[headerRowIndex], workbookPart);
                if (!headerMap.Any())
                    return ErrorResult(result, "Khong tim thay cot du lieu trong file Excel.");

                int dataStartIndex = headerRowIndex + 2;
                if (dataStartIndex >= rows.Count)
                    return ErrorResult(result, "File Excel khong co du lieu sau dong tieu de.");

                for (int i = dataStartIndex; i < rows.Count; i++)
                {
                   
                    await ProcessRow(rows[i], i , workbookPart, userId, result, headerMap);
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

        private async Task ProcessRow(Row row, int rowIndex, WorkbookPart workbookPart, int userId, EmployeeImportResult result, System.Collections.Generic.Dictionary<string, int> headerMap)
        {
            try
            {
                var values = ExcelHelper.GetRowValues(row, workbookPart).ToList();

                var fullName = GetValue(headerMap, values, "FullName");
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    return;
                }
                if (values.All(string.IsNullOrWhiteSpace)) return;

                result.Total++;
                var rawPosition = GetValue(headerMap, values, "Position");
                var rawDepartment = GetValue(headerMap, values, "Department");
                var rawEducation = GetValue(headerMap, values, "EducationLevel");
                var rawMarital = GetValue(headerMap, values, "Maritalstatus");
                var rawReligion = GetValue(headerMap, values, "Religion");
                var contractData = CreateContractData(values, headerMap);
                var insuranceData = CreateInsuranceData(values, headerMap);
                var positionCode = await EnsureMasterDataAsync(rawPosition, 2, userId);    // TypeData 2 = Position
                var departmentCode = await EnsureMasterDataAsync(rawDepartment, 5, userId); // TypeData 5 = Department
                var educationCode = await EnsureMasterDataAsync(rawEducation, 14, userId);  // TypeData 14 = Education level
                var maritalCode = await EnsureMasterDataAsync(rawMarital, 13, userId);      // TypeData 13 = Marital status
                var religionCode = await EnsureMasterDataAsync(rawReligion, 20, userId);    // TypeData 20 = Religion

                if (!string.IsNullOrWhiteSpace(contractData.ContractTypeRaw))
                {
                    // TypeData 1 = Loại HĐ (master data)
                    contractData.ContractTypeCode = await EnsureMasterDataAsync(contractData.ContractTypeRaw, 1, userId);
                }
                contractData.CodeId = Guid.NewGuid().ToString("N").Substring(0,8).ToUpper();
                var employee = CreateEmployeeFromRow(values, headerMap, positionCode, departmentCode, educationCode, maritalCode, religionCode);
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

        private string GetValue(Dictionary<string, int> headerMap, List<string> values, string key)
        {
            if (headerMap.TryGetValue(key, out var idx) && idx < values.Count)
            {
                return values[idx]?.Trim() ?? "";
            }
            return "";
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
                // await TrySaveContractAsync(createdEmployee, contractData, userId, rowIndex, result);
                // await TrySaveInsuranceAsync(createdEmployee, insuranceData, userId, rowIndex, result);
            }
            else
            {
                AddError(result, rowIndex, "Khong them nhan vien vao database");
            }
        }

        private ContractRowData CreateContractData(System.Collections.Generic.List<string> values, Dictionary<string, int> headerMap)
        {
            var data = new ContractRowData
            {
                NoAgree = GetValue(headerMap, values, "ContractNo"),
                ContractTypeRaw = GetValue(headerMap, values, "ContractType")
            };

            var startStr = GetValue(headerMap, values, "ContractStart");
            if (!string.IsNullOrWhiteSpace(startStr) && ExcelHelper.TryParseDate(startStr, out var start)) data.Start = start;

            var endStr = GetValue(headerMap, values, "ContractEnd");
            if (!string.IsNullOrWhiteSpace(endStr) && ExcelHelper.TryParseDate(endStr, out var end)) data.End = end;

            return data;
        }

        private InsuranceRowData CreateInsuranceData(System.Collections.Generic.List<string> values, Dictionary<string, int> headerMap)
        {
            var data = new InsuranceRowData
            {
                PITDateRaw = GetValue(headerMap, values, "PITDate"),
                Dependent = GetValue(headerMap, values, "Dependent"),
                NumberCode = GetValue(headerMap, values, "InsuranceNumber"),
                RegHospital = GetValue(headerMap, values, "RegHospital"),
                TaxCode = GetValue(headerMap, values, "TaxCode")
            };

            var effectedStr = GetValue(headerMap, values, "EffectedFrom");
            if (!string.IsNullOrWhiteSpace(effectedStr) && ExcelHelper.TryParseDate(effectedStr, out var effected)) data.EffectedFrom = effected;
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

            if (!string.IsNullOrWhiteSpace(insuranceData.RegHospital) || !string.IsNullOrWhiteSpace(insuranceData.TaxCode))
            {
                var tax = await _unitOfWork.TaxtItemRep.GetInfo(employee.UserName) ?? new TaxItem();
                tax.UserName = employee.UserName;
                tax.CodeId = string.IsNullOrWhiteSpace(tax.CodeId) ? employee.Id.ToString() : tax.CodeId;
                tax.RegBHYT = insuranceData.RegHospital ?? tax.RegBHYT;
                tax.Number = insuranceData.TaxCode ?? tax.Number;
                tax.CreatedBy = tax.CreatedBy == 0 ? userId : tax.CreatedBy;
                tax.UpdatedBy = userId;

                var savedTax = await _unitOfWork.TaxtItemRep.AddOrUpdate(tax);
                if (!savedTax)
                {
                    AddError(result, rowIndex, "Khong luu duoc thong tin Tax");
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
            public string? TaxCode { get; set; }

            public bool HasData =>
                !string.IsNullOrWhiteSpace(NumberCode) ||
                !string.IsNullOrWhiteSpace(RegHospital) ||
                EffectedFrom.HasValue ||
                PITDate.HasValue ||
                !string.IsNullOrWhiteSpace(Dependent) ||
                !string.IsNullOrWhiteSpace(TaxCode);
        }

        private Employee CreateEmployeeFromRow(System.Collections.Generic.List<string> values, Dictionary<string, int> headerMap, string? positionCode, string? departmentCode, string? educationCode, string? maritalCode, string? religionCode)
        {
            var employee = new Employee
            {
                FullName = GetValue(headerMap, values, "FullName"),
                Phone = GetValue(headerMap, values, "Phone"),
                PositionCode = positionCode ?? NormalizeCode(GetValue(headerMap, values, "Position")),
                DepartmentCode = departmentCode ?? NormalizeCode(GetValue(headerMap, values, "Department")),
                Gender = NormalizeGender(GetValue(headerMap, values, "Gender")),
                PlaceOfBirth = GetValue(headerMap, values, "PlaceOfBirth"),
                Religion = religionCode ?? GetValue(headerMap, values, "Religion"),
                EducationLevel = educationCode ?? GetValue(headerMap, values, "EducationLevel"),
                Maritalstatus = maritalCode ?? GetValue(headerMap, values, "Maritalstatus"),
                NationalId = GetValue(headerMap, values, "NationalId"),
                NationalPlace = GetValue(headerMap, values, "NationalPlace"),
                PermanentAddress = GetValue(headerMap, values, "PermanentAddress"),
                TemporaryAddress = GetValue(headerMap, values, "TemporaryAddress"),
                Email = GetValue(headerMap, values, "Email"),
                PersonalEmail = GetValue(headerMap, values, "PersonalEmail"),
                EmergencyContact = GetValue(headerMap, values, "EmergencyContact"),
                BeneficiaryName = GetValue(headerMap, values, "BeneficiaryName"),
                BankAccount = GetValue(headerMap, values, "BankAccount"),
                BankName = GetValue(headerMap, values, "BankName"),
                Noted = GetValue(headerMap, values, "Noted")
            };

            var onboardStr = GetValue(headerMap, values, "Onboard");
            if (!string.IsNullOrWhiteSpace(onboardStr) && ExcelHelper.TryParseDate(onboardStr, out var onboard)) employee.Onboard = onboard;

            var dobStr = GetValue(headerMap, values, "Dob");
            if (!string.IsNullOrWhiteSpace(dobStr) && ExcelHelper.TryParseDate(dobStr, out var dob)) employee.Dob = dob;

            var nationalDateStr = GetValue(headerMap, values, "NationalDate");
            if (!string.IsNullOrWhiteSpace(nationalDateStr) && ExcelHelper.TryParseDate(nationalDateStr, out var nationalDate)) employee.NationalDate = nationalDate;

            return employee;
        }

        // Ensure master data exists; if not, create it with the given typeData
        // If a value already exists (by name), reuse its code regardless of typeData
        private async Task<string?> EnsureMasterDataAsync(string rawValue, int typeData, int userId)
        {
            var trimmed = rawValue?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmed)) return null;

            var normalized = NormalizeCode(trimmed);

            // 1. Try by code first within the same typeData (for numeric codes)
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                var existingByCode = await _unitOfWork.MasterDataRep.GetByCode(normalized, typeData);
                if (existingByCode != null && existingByCode.Id > 0) return existingByCode.Code;
            }

            // 2. Try by exact name match within the same typeData
            var existingByName = await _unitOfWork.MasterDataRep.GetByName(trimmed, typeData);
            if (existingByName != null && existingByName.Id > 0) return existingByName.Code;

            // 3. Not found, insert new master data with the specified typeData
            var newItem = new MasterDataAdd
            {
                Name = trimmed,
                TypeData = typeData,
                IsActive = 1,
                CreatedBy = userId
            };

            if (await _unitOfWork.MasterDataRep.AddOrUpdate(newItem))
            {
                // Retrieve the newly inserted item to get its Code
                var inserted = await _unitOfWork.MasterDataRep.GetByName(trimmed, typeData);
                if (inserted != null && inserted.Id > 0) return inserted.Code;
            }

            // Fallback: return the raw normalized code if insertion failed
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

        // Find the row index where actual data starts (first row after header)
        private int FindHeaderRow(List<Row> rows, WorkbookPart workbookPart)
        {
            // Assume header is always the first row (index 0)
            if (rows.Count > 0)
            {
                return 0;
            }
            return -1;
        }

        private Dictionary<string, int> GetHeaderMap(Row headerRow, WorkbookPart workbookPart)
        {
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var headerValues = ExcelHelper.GetRowValues(headerRow, workbookPart).ToList();

            var standardHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"STT", "STT"},
                {"Full Name", "FullName"}, {"Họ tên", "FullName"}, {"Ho ten", "FullName"},
                {"Onboard", "Onboard"}, {"Ngày vào", "Onboard"}, {"Ngay vao", "Onboard"},
                {"Position", "Position"}, {"Chức danh", "Position"}, {"Chuc danh", "Position"},
                {"Department", "Department"}, {"Phòng ban", "Department"}, {"Phong ban", "Department"},
                {"Gender", "Gender"}, {"Giới tính", "Gender"}, {"Gioi tinh", "Gender"},
                {"Dob", "Dob"}, {"Ngày sinh", "Dob"}, {"Ngay sinh", "Dob"},
                {"PlaceOfBirth", "PlaceOfBirth"}, {"Nơi sinh", "PlaceOfBirth"}, {"Noi sinh", "PlaceOfBirth"},
                {"Religion", "Religion"}, {"Tôn giáo", "Religion"}, {"Ton giao", "Religion"},
                {"EducationLevel", "EducationLevel"}, {"Trình độ học vấn", "EducationLevel"}, {"Trinh do hoc van", "EducationLevel"},
                {"Maritalstatus", "Maritalstatus"}, {"Tình trạng hôn nhân", "Maritalstatus"}, {"Tinh trang hon nhan", "Maritalstatus"},
                {"NationalId", "NationalId"}, {"CMND/CCCD", "NationalId"}, {"CMND", "NationalId"}, {"CCCD", "NationalId"},
                {"NationalDate", "NationalDate"}, {"Ngày cấp", "NationalDate"}, {"Ngay cap", "NationalDate"},
                {"NationalPlace", "NationalPlace"}, {"Nơi cấp", "NationalPlace"}, {"Noi cap", "NationalPlace"},
                {"PermanentAddress", "PermanentAddress"}, {"Địa chỉ thường trú", "PermanentAddress"}, {"Dia chi thuong tru", "PermanentAddress"},
                {"TemporaryAddress", "TemporaryAddress"}, {"Địa chỉ tạm trú", "TemporaryAddress"}, {"Dia chi tam tru", "TemporaryAddress"},
                {"Email", "Email"},
                {"PersonalEmail", "PersonalEmail"}, {"Email cá nhân", "PersonalEmail"}, {"Email ca nhan", "PersonalEmail"},
                {"Phone", "Phone"}, {"Số điện thoại", "Phone"}, {"So dien thoai", "Phone"},
                {"EmergencyContact", "EmergencyContact"}, {"Người liên hệ khẩn cấp", "EmergencyContact"}, {"Nguoi lien he khan cap", "EmergencyContact"},
                {"BeneficiaryName", "BeneficiaryName"}, {"Người thụ hưởng", "BeneficiaryName"}, {"Nguoi thu huong", "BeneficiaryName"},
                {"BankAccount", "BankAccount"}, {"Số tài khoản", "BankAccount"}, {"So tai khoan", "BankAccount"},
                {"BankName", "BankName"}, {"Tên ngân hàng", "BankName"}, {"Ten ngan hang", "BankName"},
                {"PITDate", "PITDate"}, {"Ngày tính thuế TNCN", "PITDate"}, {"Ngay tinh thue TNCN", "PITDate"},
                {"Dependent", "Dependent"}, {"Số người phụ thuộc", "Dependent"}, {"So nguoi phu thuoc", "Dependent"},
                {"EffectedFrom", "EffectedFrom"}, {"Ngày hiệu lực BHXH", "EffectedFrom"}, {"Ngay hieu luc BHXH", "EffectedFrom"},
                {"InsuranceNumber", "InsuranceNumber"}, {"Mã BHXH/BHYT", "InsuranceNumber"}, {"Ma BHXH/BHYT", "InsuranceNumber"},
                {"RegHospital", "RegHospital"}, {"Nơi đăng ký KCB", "RegHospital"}, {"Noi dang ky KCB", "RegHospital"},
                {"ContractNo", "ContractNo"}, {"Số HĐLĐ", "ContractNo"}, {"So HDLD", "ContractNo"},
                {"ContractType", "ContractType"}, {"Loại HĐLĐ", "ContractType"}, {"Loai HDLD", "ContractType"},
                {"ContractStart", "ContractStart"}, {"Ngày bắt đầu HĐ", "ContractStart"}, {"Ngay bat dau HD", "ContractStart"},
                {"ContractEnd", "ContractEnd"}, {"Ngày kết thúc HĐ", "ContractEnd"}, {"Ngay ket thuc HD", "ContractEnd"},
                {"Noted", "Noted"}, {"Ghi chú", "Noted"}, {"Ghi chu", "Noted"}
            };

            for (int i = 0; i < headerValues.Count; i++)
            {
                var headerText = headerValues[i]?.Trim();
                if (!string.IsNullOrWhiteSpace(headerText))
                {
                    if (standardHeaders.TryGetValue(headerText, out var mappedName))
                    {
                        if (!headerMap.ContainsKey(mappedName)) // Only add the first occurrence if duplicates exist
                        {
                            headerMap.Add(mappedName, i);
                        }
                    }
                }
            }
            return headerMap;
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
