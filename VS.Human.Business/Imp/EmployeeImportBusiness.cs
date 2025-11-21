using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using VS.Human.Business.Helpers;
using VS.Human.Business.Model;
using VS.Human.Rep;
using VS.Human.Rep.Model;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

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
                {
                    result.Errors.Add(new EmployeeImportError { Row = 0, Content = "File Excel không hợp lệ" });
                    result.TotalError = 1;
                    return result;
                }

                var sheetData = worksheetPart.Worksheet.Elements<SheetData>().First();
                var rows = sheetData.Elements<Row>().ToList();

                if (rows.Count < 2)
                {
                    result.Errors.Add(new EmployeeImportError { Row = 0, Content = "File Excel không có dữ liệu" });
                    result.TotalError = 1;
                    return result;
                }

                // TỰ ĐỘNG TÌM DÒNG BẮT ĐẦU DỮ LIỆU
                // Tìm dòng đầu tiên có STT = 1 hoặc có ít nhất 2 cột có dữ liệu
                int dataStartIndex = -1;
                for (int i = 0; i < rows.Count; i++)
                {
                    var values = GetRowValues(rows[i], workbookPart).ToList();
                    
                    // Bỏ qua dòng trống
                    if (values.All(v => string.IsNullOrWhiteSpace(v)))
                        continue;
                    
                    // Kiểm tra có phải dòng data không (cột 0 = STT, cột 1 = Họ tên)
                    // STT thường là số (1, 2, 3...) hoặc có Họ tên ở cột 1
                    if (values.Count > 1)
                    {
                        var col0 = values[0]?.Trim() ?? "";
                        var col1 = values[1]?.Trim() ?? "";
                        
                        // Nếu cột 0 là số và cột 1 có dữ liệu (không phải header)
                        if (int.TryParse(col0, out int stt) && !string.IsNullOrWhiteSpace(col1) 
                            && !col1.Contains("Full Name") && !col1.Contains("Tên nhân viên"))
                        {
                            dataStartIndex = i;
                            break;
                        }
                    }
                }

                if (dataStartIndex == -1)
                {
                    result.Errors.Add(new EmployeeImportError { Row = 0, Content = "Không tìm thấy dòng dữ liệu trong file Excel. Đảm bảo cột 0 là STT (số) và cột 1 là Họ tên" });
                    result.TotalError = 1;
                    return result;
                }

                // Không cần parse header vì dùng fixed index
                var headers = new List<string>(); // Dummy

                // Bắt đầu từ dòng tìm được
                for (int i = dataStartIndex; i < rows.Count; i++)
                {
                    var row = rows[i];
                    var rowIndex = i + 1; // Excel row number (1-based)
                    result.Total++;

                    try
                    {
                        var values = GetRowValues(row, workbookPart).ToList();
                        if (values.All(v => string.IsNullOrWhiteSpace(v)))
                        {
                            continue; // Skip empty rows
                        }

                        var employee = ParseEmployeeRow(headers, values, rowIndex, result.Errors);
                        if (employee != null)
                        {
                            employee.CreatedBy = userId;
                            employee.CreateAt = DateTime.Now;
                            employee.Id = -1; // New employee

                            // Generate UserName if not provided
                            if (string.IsNullOrEmpty(employee.UserName))
                            {
                                employee.UserName = EmployeeMapper.GenerateUserName(employee.Email, employee.Phone, employee.FullName);
                            }

                            // Set default password
                            if (string.IsNullOrEmpty(employee.Pass))
                            {
                                employee.Pass = "Vietstar@2024";
                            }

                            var employeeInfoAdd = new EmployeeInfoAdd
                            {
                                Id = employee.Id,
                                FullName = employee.FullName,
                                Phone = employee.Phone,
                                Email = employee.Email,
                                UserName = employee.UserName,
                                Pass = employee.Pass,
                                RoleCode = employee.RoleCode,
                                DepartmentCode = employee.DepartmentCode,
                                PositionCode = employee.PositionCode,
                                ManagerId = employee.ManagerId,
                                Dob = employee.Dob,
                                Onboard = employee.Onboard,
                                NationalId = employee.NationalId,
                                NationalDate = employee.NationalDate,
                                NationalPlace = employee.NationalPlace,
                                PermanentAddress = employee.PermanentAddress,
                                TemporaryAddress = employee.TemporaryAddress,
                                Noted = employee.Noted,
                                Status = employee.Status,
                                StatusWork = employee.StatusWork ?? string.Empty,
                                BankAccount = employee.BankAccount,
                                BankName = employee.BankName,
                                EducationLevel = employee.EducationLevel,
                                Maritalstatus = employee.Maritalstatus,
                                DocumentCheck = employee.DocumentCheck,
                                Gender = employee.Gender,
                                PlaceOfBirth = employee.PlaceOfBirth,
                                Religion = employee.Religion,
                                PersonalEmail = employee.PersonalEmail,
                                BeneficiaryName = employee.BeneficiaryName,
                                CreatedBy = userId,
                                CreateAt = DateTime.Now
                            };

                            var success = await _empBusiness.Add(employeeInfoAdd);
                            if (success)
                            {
                                result.TotalSuccess++;
                            }
                            else
                            {
                                result.TotalError++;
                                result.Errors.Add(new EmployeeImportError
                                {
                                    Row = rowIndex,
                                    Content = "Không thể thêm nhân viên vào database"
                                });
                            }
                        }
                        else
                        {
                            result.TotalError++;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.TotalError++;
                        result.Errors.Add(new EmployeeImportError
                        {
                            Row = rowIndex,
                            Content = $"Lỗi xử lý dòng: {ex.Message}"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(new EmployeeImportError
                {
                    Row = 0,
                    Content = $"Lỗi đọc file Excel: {ex.Message}"
                });
                result.TotalError++;
            }

            return result;
        }

        private Employee? ParseEmployeeRow(List<string> headers, List<string> values, int rowIndex, List<EmployeeImportError> errors)
        {
            var employee = new Employee();

            try
            {
                // ============================================
                // FORMAT FILE IMPORT (34 cột)
                // Dòng 1-41: Headers (merged, multi-line)
                // Dòng 42+: Dữ liệu
                // ============================================
                // Cột 0: STT (bỏ qua)
                // Cột 1: Họ tên (REQUIRED) ⭐
                // Cột 2: Ngày vào làm
                // Cột 3: Chức vụ
                // Cột 4: Bộ phận
                // Cột 5: Giới tính
                // Cột 6: Ngày sinh
                // Cột 7: Nơi sinh
                // Cột 8: Tôn giáo
                // Cột 9: Trình độ học vấn
                // Cột 10: Tình trạng hôn nhân
                // Cột 11: Số CMND/CCCD
                // Cột 12: Ngày cấp
                // Cột 13: Nơi cấp
                // Cột 14: Địa chỉ thường trú
                // Cột 15: Địa chỉ tạm trú
                // Cột 16: Email công ty
                // Cột 17: Email cá nhân
                // Cột 18: Số điện thoại (REQUIRED) ⭐
                // Cột 19: Liên hệ khẩn cấp
                // Cột 20: Tên chủ tài khoản (Người thụ hưởng)
                // Cột 21: Số tài khoản
                // Cột 22: Tên ngân hàng
                // Cột 23: Mã số thuế (PIT Code)
                // Cột 24: Ngày cấp MST
                // Cột 25: Số người phụ thuộc
                // Cột 26: Hiệu lực từ
                // Cột 27: Số sổ BHXH
                // Cột 28: Tên bệnh viện
                // Cột 29-32: Hợp đồng (bỏ qua)
                // ============================================

                // Helper để lấy giá trị an toàn
                string GetValue(int index)
                {
                    if (index < values.Count)
                        return values[index]?.Trim() ?? "";
                    return "";
                }

                // REQUIRED: Cột 1 - Họ và tên
                var fullName = GetValue(1);
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    errors.Add(new EmployeeImportError { Row = rowIndex, Content = "Thiếu thông tin 'Họ và tên' (Cột 1)" });
                    return null;
                }
                employee.FullName = fullName;

                // REQUIRED: Cột 18 - Số điện thoại
                var phone = GetValue(18);
                if (string.IsNullOrWhiteSpace(phone))
                {
                    errors.Add(new EmployeeImportError { Row = rowIndex, Content = "Thiếu thông tin 'Số điện thoại' (Cột 18)" });
                    return null;
                }
                employee.Phone = phone;

                // Cột 2: Ngày vào làm
                var onboardStr = GetValue(2);
                if (!string.IsNullOrWhiteSpace(onboardStr) && TryParseDate(onboardStr, out DateTime onboard))
                {
                    employee.Onboard = onboard;
                }

                // Cột 3: Chức vụ (Position)
                employee.PositionCode = GetValue(3);

                // Cột 4: Bộ phận (Department)
                employee.DepartmentCode = GetValue(4);

                // Cột 5: Giới tính
                employee.Gender = GetValue(5);

                // Cột 6: Ngày sinh
                var dobStr = GetValue(6);
                if (!string.IsNullOrWhiteSpace(dobStr) && TryParseDate(dobStr, out DateTime dob))
                {
                    employee.Dob = dob;
                }

                // Cột 7: Nơi sinh
                employee.PlaceOfBirth = GetValue(7);

                // Cột 8: Tôn giáo
                employee.Religion = GetValue(8);

                // Cột 9: Trình độ học vấn
                employee.EducationLevel = GetValue(9);

                // Cột 10: Tình trạng hôn nhân
                employee.Maritalstatus = GetValue(10);

                // Cột 11: Số CMND/CCCD
                employee.NationalId = GetValue(11);

                // Cột 12: Ngày cấp
                var nationalDateStr = GetValue(12);
                if (!string.IsNullOrWhiteSpace(nationalDateStr) && TryParseDate(nationalDateStr, out DateTime nationalDate))
                {
                    employee.NationalDate = nationalDate;
                }

                // Cột 13: Nơi cấp
                employee.NationalPlace = GetValue(13);

                // Cột 14: Địa chỉ thường trú
                employee.PermanentAddress = GetValue(14);

                // Cột 15: Địa chỉ tạm trú
                employee.TemporaryAddress = GetValue(15);

                // Cột 16: Email công ty
                employee.Email = GetValue(16);

                // Cột 17: Email cá nhân
                employee.PersonalEmail = GetValue(17);

                // Cột 20: Tên chủ tài khoản / Người thụ hưởng
                employee.BeneficiaryName = GetValue(20);

                // Cột 21: Số tài khoản
                employee.BankAccount = GetValue(21);

                // Cột 22: Tên ngân hàng
                employee.BankName = GetValue(22);

                // Cột 19: Liên hệ khẩn cấp
                employee.EmergencyContact = GetValue(19);

                return employee;
            }
            catch (Exception ex)
            {
                errors.Add(new EmployeeImportError { Row = rowIndex, Content = $"Lỗi parse dữ liệu: {ex.Message}" });
                return null;
            }
        }

        private bool TryParseDate(string dateStr, out DateTime date)
        {
            date = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(dateStr))
                return false;

            // Try common date formats
            var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd", "MM/dd/yyyy" };
            foreach (var format in formats)
            {
                if (DateTime.TryParseExact(dateStr, format, null, System.Globalization.DateTimeStyles.None, out date))
                {
                    return true;
                }
            }

            // Try general parse
            if (DateTime.TryParse(dateStr, out date))
            {
                return true;
            }

            return false;
        }

        private IEnumerable<string> GetRowValues(Row row, WorkbookPart workbookPart)
        {
            var sharedStringTable = workbookPart?.SharedStringTablePart?.SharedStringTable;
            var values = new List<string>();
            int currentIndex = 0;

            foreach (var cell in row.Elements<Cell>())
            {
                var cellRef = cell.CellReference?.Value;
                int cellIndex = currentIndex;

                if (!string.IsNullOrEmpty(cellRef))
                {
                    cellIndex = GetColumnIndex(cellRef);
                }

                // Fill gaps with empty strings
                while (currentIndex < cellIndex)
                {
                    values.Add(string.Empty);
                    currentIndex++;
                }

                var value = GetCellValue(cell, sharedStringTable);
                values.Add(value ?? string.Empty);
                currentIndex++;
            }

            return values;
        }

        private int GetColumnIndex(string cellReference)
        {
            // Remove digits to get column name (e.g., "AB12" -> "AB")
            string columnName = Regex.Replace(cellReference, "[0-9]", "");
            
            int columnIndex = 0;
            int factor = 1;

            // Calculate index from base-26 column name
            for (int i = columnName.Length - 1; i >= 0; i--)
            {
                if (char.IsLetter(columnName[i]))
                {
                    columnIndex += (char.ToUpper(columnName[i]) - 'A' + 1) * factor;
                    factor *= 26;
                }
            }

            return columnIndex - 1; // Convert to 0-based index
        }



        private string? GetCellValue(Cell cell, SharedStringTable? sharedStringTable)
        {
            if (cell.CellValue == null)
                return null;

            var value = cell.CellValue.Text;

            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                if (int.TryParse(value, out int index) && sharedStringTable != null)
                {
                    var item = sharedStringTable.Elements<SharedStringItem>().ElementAtOrDefault(index);
                    if (item == null) return null;

                    var sb = new StringBuilder();
                    foreach (var element in item.Elements())
                    {
                        if (element is Text text)
                        {
                            sb.Append(text.Text);
                        }
                        else if (element is Run run)
                        {
                            if (run.Text != null)
                                sb.Append(run.Text.Text);
                        }
                    }
                    return sb.ToString();
                }
            }

            return value;
        }

        /// <summary>
        /// Normalize header: bỏ dấu tiếng Việt, lowercase, trim
        /// </summary>
        private string NormalizeHeader(string header)
        {
            if (string.IsNullOrWhiteSpace(header))
                return "";

            // Lowercase và trim
            var normalized = header.ToLower().Trim();

            // Bỏ dấu tiếng Việt
            normalized = RemoveVietnameseTones(normalized);

            // Bỏ các ký tự đặc biệt, giữ lại chữ, số, khoảng trắng
            normalized = Regex.Replace(normalized, @"[^a-z0-9\s]", "");

            // Bỏ khoảng trắng thừa
            normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

            return normalized;
        }

        /// <summary>
        /// Bỏ dấu tiếng Việt
        /// </summary>
        private string RemoveVietnameseTones(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
            var result = new System.Text.StringBuilder();

            foreach (var c in normalized)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    result.Append(c);
                }
            }

            return result.ToString().Normalize(System.Text.NormalizationForm.FormC)
                .Replace("đ", "d").Replace("Đ", "d");
        }

        /// <summary>
        /// Lấy giá trị từ dictionary với nhiều key có thể
        /// </summary>
        private string GetValueFromDict(Dictionary<string, string> dict, params string[] possibleKeys)
        {
            foreach (var key in possibleKeys)
            {
                var normalizedKey = NormalizeHeader(key);
                if (dict.ContainsKey(normalizedKey) && !string.IsNullOrWhiteSpace(dict[normalizedKey]))
                {
                    return dict[normalizedKey];
                }
            }
            return string.Empty;
        }
    }
}

