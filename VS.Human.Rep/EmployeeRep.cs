using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Linq;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class EmployeeRep : RepositoryBase<Employee>, IEmployeeRep
    {

        public EmployeeRep(IConfiguration configuration)
            : base(configuration)
        {

            tableName = "Employees";
        }

        public async Task<Employee> GetById(int id)
        {
            using (var con = GetConnection())
            {
                var sql = @"
                    SELECT TOP 1 d.*, dbo.getDisplayMasterData(d.StatusWork) AS StatusWorkText, gm.GroupId
                    FROM Employees d
                    OUTER APPLY (
                        SELECT TOP 1 GroupId
                        FROM GroupMember
                        WHERE MemberId = d.Id AND ISNULL(Deleted, 0) = 0
                        ORDER BY Id DESC
                    ) gm
                    WHERE d.Id = @id";
                
                var result = await con.QuerySingleOrDefaultAsync<Employee>(sql, new { id });
                if (result == null)
                {
                    return new Employee { Id = -1 };
                }
                return result;
            }
        }
        public async Task<Employee> Login(string userName, string password)
        {
            var modelCheck = new
            {
                userName,
                password
            };
            var result = await ExecuteSQL<Employee>("sp_emp_login",
              modelCheck);
            return result;
        }

        private async Task<bool> Update(Employee item)
        {
            var parameter = new
            {
                item.Id,
                item.FullName,
                item.NationalDate,
                item.NationalId,
                item.NationalPlace,
                item.Dob,
                item.Onboard,
                item.ResignationDate,
                item.Phone,
                item.PositionCode,
                item.RoleCode,
                item.ManagerId,
                item.DepartmentCode,
                item.GroupId,
                item.Email,
                item.CVLink,
                item.Noted,
                item.PermanentAddress,
                item.TemporaryAddress,
                item.DocumentStatus,
                item.Status,
                item.UpdatedBy,
                item.UpdateAt,
                item.IsActive,
                item.BankAccount,
                item.BankName,
                item.EducationLevel,
                item.Maritalstatus,
                item.DocumentCheck,
                item.StatusWork,
                item.Gender,
                item.PlaceOfBirth,
                item.Ethnicity,
                item.Religion,
                item.PersonalEmail,
                item.BeneficiaryName,
                item.EmergencyContact,
                item.FingerprintCode
            };

            return await this.ExecuteSQL("sp_emp_update", parameter);
        }
        private async Task<bool> Add(Employee item)
        {
            item.CreateAt = DateTime.Now;
            item.UpdateAt = DateTime.Now;

            var parameter = new
            {
                item.FullName,
                item.NationalDate,
                item.NationalId,
                item.NationalPlace,
                item.Dob,
                item.Onboard,
                item.ResignationDate,
                item.Phone,
                item.PositionCode,
                item.RoleCode,
                item.UserName,
                item.Pass,
                item.ManagerId,
                item.DepartmentCode,
                item.GroupId,
                item.Email,
                item.CVLink,
                item.Noted,
                item.PermanentAddress,
                item.TemporaryAddress,
                item.DocumentStatus,
                item.Status,
                item.CreatedBy,
                item.UpdatedBy,
                item.CreateAt,
                item.UpdateAt,
                item.IsActive,
                item.EducationLevel,
                item.DocumentCheck,
                item.Maritalstatus,
                item.StatusWork,
                item.BankAccount,
                item.BankName,
                item.Gender,
                item.PlaceOfBirth,
                item.Ethnicity,
                item.Religion,
                item.PersonalEmail,
                item.BeneficiaryName,
                item.EmergencyContact,
                item.FingerprintCode
            };

            return await this.ExecuteSQL("sp_emp_insert", parameter);
        }
        public async Task<Employee> CheckDuplicate(string email, string phone)
        {
            var parameter = new
            {
                email,

                phone
            };
            var result = await ExecuteSQL<Employee>("sp_Check_Duplicate", parameter);
            return result;
        }

        public async Task<Employee> GetByUserName(string userName)
        {
            var parameter = new { userName };
            var sql = "SELECT TOP 1 * FROM Employees WHERE UserName = @userName AND ISNULL(Deleted,0)=0";
            return await ExecuteSQL<Employee>(sql, parameter);
        }

        public async Task<Employee?> GetByFingerprintCode(string fingerprintCode)
        {
            if (string.IsNullOrWhiteSpace(fingerprintCode))
            {
                return null;
            }

            var parameter = new { fingerprintCode };
            var sql = "SELECT TOP 1 * FROM Employees WHERE FingerprintCode = @fingerprintCode AND ISNULL(Deleted,0)=0";
            return await ExecuteSQL<Employee>(sql, parameter);
        }

        public async Task<Employee> GetLastByEmailOrPhone(string email, string phone)
        {
            var parameter = new { email, phone };
            var sql = @"SELECT TOP 1 * FROM Employees 
                        WHERE (Email = @email OR Phone = @phone)
                        ORDER BY Id DESC";
            return await ExecuteSQL<Employee>(sql, parameter);
        }

        public async Task<List<Employee>> GetDuplicateSeeds()
        {
            var sql = "SELECT Id, Email, Phone, NationalId FROM Employees WHERE ISNULL(Deleted,0)=0";
            var result = await GetDataList<Employee>(sql);
            return result ?? new List<Employee>();
        }

        public async Task<List<Employee>> GetByRoleCodes(IEnumerable<string> roleCodes)
        {
            var codes = roleCodes?
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => code.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (codes == null || codes.Count == 0)
            {
                return new List<Employee>();
            }

            using (var con = GetConnection())
            {
                var sql = "SELECT * FROM Employees WHERE RoleCode IN @roleCodes AND ISNULL(Deleted,0)=0";
                var result = await con.QueryAsync<Employee>(sql, new { roleCodes = codes });
                return result?.ToList() ?? new List<Employee>();
            }
        }

        public async Task<bool> IsPeopleManager(int employeeId)
        {
            if (employeeId <= 0)
            {
                return false;
            }

            using (var con = GetConnection())
            {
                var sql = @"
                    SELECT CASE
                        WHEN EXISTS (
                            SELECT 1
                            FROM [Group] g
                            WHERE TRY_CONVERT(int, g.ManagerId) = @EmployeeId
                              AND ISNULL(g.Deleted, 0) = 0
                        )
                        OR EXISTS (
                            SELECT 1
                            FROM Employees e
                            WHERE e.ManagerId = @EmployeeId
                              AND e.Id <> @EmployeeId
                              AND ISNULL(e.Deleted, 0) = 0
                        )
                        THEN CAST(1 AS bit)
                        ELSE CAST(0 AS bit)
                    END";

                return await con.ExecuteScalarAsync<bool>(sql, new { EmployeeId = employeeId });
            }
        }

        public async Task<Employee?> GetTeamLeadByDepartmentCode(string departmentCode)
        {
            if (string.IsNullOrWhiteSpace(departmentCode))
            {
                return null;
            }

            using (var con = GetConnection())
            {
                var sql = @"
                    SELECT TOP 1 *
                    FROM Employees
                    WHERE ISNULL(Deleted, 0) = 0
                      AND ISNULL(IsActive, 0) = 1
                      AND DepartmentCode = @DepartmentCode
                      AND RoleCode IN ('3', 'TL')
                    ORDER BY UpdateAt DESC, Id DESC";

                return await con.QueryFirstOrDefaultAsync<Employee>(sql, new
                {
                    DepartmentCode = departmentCode.Trim()
                });
            }
        }

        public async Task<List<Employee>> GetActiveByDepartmentCode(string departmentCode)
        {
            if (string.IsNullOrWhiteSpace(departmentCode))
            {
                return new List<Employee>();
            }

            using (var con = GetConnection())
            {
                var sql = @"
                    SELECT *
                    FROM Employees
                    WHERE ISNULL(Deleted, 0) = 0
                      AND ISNULL(IsActive, 0) = 1
                      AND DepartmentCode = @DepartmentCode
                    ORDER BY NEWID()";

                var result = await con.QueryAsync<Employee>(sql, new
                {
                    DepartmentCode = departmentCode.Trim()
                });

                return result?.ToList() ?? new List<Employee>();
            }
        }
        public async Task<bool> ChangePassword(string password, int id)
        {
            var parameter = new
            {
                password,
                id
            };

            return await this.ExecuteSQL("sp_emp_changePassword", parameter);
        }

        public async Task<bool> UpdateCredentials(int id, string userName, string password)
        {
            var sql = "UPDATE Employees SET UserName = @userName, Pass = @password, UpdateAt = getdate() WHERE Id = @id";
            return await ExecuteSQL(sql, new
            {
                id,
                userName,
                password
            });
        }

        public async Task<bool> UpdateAvatar(int id, string avatarFile, int updatedBy)
        {
            var sql = @"
                UPDATE Employees
                SET AvatarFile = @avatarFile,
                    UpdatedBy = @updatedBy,
                    UpdateAt = GETDATE()
                WHERE Id = @id AND ISNULL(Deleted, 0) = 0";

            return await ExecuteSQL(sql, new
            {
                id,
                avatarFile,
                updatedBy
            });
        }

        public async Task<bool> UpdateMailSignature(int id, string? mailSignature, int updatedBy)
        {
            var sql = @"
                UPDATE Employees
                SET MailSignature = @mailSignature,
                    UpdatedBy = @updatedBy,
                    UpdateAt = GETDATE()
                WHERE Id = @id AND ISNULL(Deleted, 0) = 0";

            return await ExecuteSQL(sql, new
            {
                id,
                mailSignature,
                updatedBy
            });
        }

        public async Task<bool> AddOrUpdate(Employee item)
        {
            if (item.Id > 0)
            {
                var itemUpdate = await GetById(item.Id);
                if (itemUpdate != null)
                {
                    itemUpdate.FullName = item.FullName;
                    itemUpdate.CreatedBy = item.CreatedBy;
                    itemUpdate.Id = item.Id;
                    itemUpdate.RoleCode = item.RoleCode;
                    itemUpdate.Dob = item.Dob;
                    itemUpdate.ManagerId = item.ManagerId;
                    itemUpdate.DepartmentCode = item.DepartmentCode;
                    itemUpdate.GroupId = item.GroupId;
                    itemUpdate.PositionCode = item.PositionCode;
                    itemUpdate.Email = item.Email;
                    itemUpdate.CVLink = item.CVLink;
                    itemUpdate.Phone = item.Phone;
                    itemUpdate.IsActive = item.IsActive;
                    itemUpdate.Noted = item.Noted;
                    itemUpdate.Onboard = item.Onboard;
                    itemUpdate.ResignationDate = item.ResignationDate;
                    itemUpdate.PermanentAddress = item.PermanentAddress;
                    itemUpdate.TemporaryAddress = item.TemporaryAddress;
                    itemUpdate.NationalId = item.NationalId;
                    itemUpdate.NationalDate = item.NationalDate;
                    itemUpdate.NationalPlace = item.NationalPlace;
                    itemUpdate.UpdatedBy = item.UpdatedBy;
                    itemUpdate.UpdateAt = DateTime.Now;
                    itemUpdate.DocumentStatus = item.DocumentStatus;
                    itemUpdate.Status = item.Status;
                    itemUpdate.BankAccount = item.BankAccount;
                    itemUpdate.BankName = item.BankName;
                    itemUpdate.EducationLevel = item.EducationLevel;
                    itemUpdate.Maritalstatus = item.Maritalstatus;
                    itemUpdate.DocumentCheck = item.DocumentCheck;
                    itemUpdate.StatusWork = item.StatusWork;
                    itemUpdate.Religion = item.Religion;
                    itemUpdate.Gender = item.Gender;
                    itemUpdate.PlaceOfBirth = item.PlaceOfBirth;
                    itemUpdate.Ethnicity = item.Ethnicity;
                    itemUpdate.PersonalEmail = item.PersonalEmail;
                    itemUpdate.BeneficiaryName = item.BeneficiaryName;
                    itemUpdate.EmergencyContact = item.EmergencyContact;
                    itemUpdate.FingerprintCode = item.FingerprintCode;
                    return await Update(itemUpdate);
                }
            }
            return await Add(item);
        }

        public async Task<BaseList> GetAll(EmployeeRequest request)
        {
            var page = request.Page;
            var limit = request.Limit;
            ProcessInputPaging(ref page, ref limit, out var offset);

            var result = await this.GetBaseAll<EmployeeIndexModel>(request,
            new
            {
                offset,
                limit,
                fromDate = request.From,
                toDate = request.To,
                 request.Status,
                request.StatusWork,
                request.DocumentStatus,
                 request.Token,
                 request.GroupId,
              request.MemberId,
                IsDeleted = request.IsDeleted ?? false,
                UserId = request.UserId,
                OrderBy = request.OrderBy


            });
            return result;
        }

        /// <summary>
        /// Lấy danh sách nhân viên với đầy đủ thông tin cho chế độ chỉnh sửa mở rộng
        /// Sử dụng stored procedure riêng sp_Employee_getAll_Extended
        /// </summary>
        public async Task<List<EmployeeExtendedModel>> ExecuteExport(EmployeeRequest request)
        {
            return await ExecuteSQL<EmployeeExtendedModel>(
                BuildEmployeeExtendedSql(includePaging: false),
                BuildEmployeeExtendedTextParams(request),
                System.Data.CommandType.Text);
        }

        public async Task<BaseList> GetAllExtended(EmployeeRequest request)
        {
            var page = request.Page;
            var limit = request.Limit;
            ProcessInputPaging(ref page, ref limit, out var offset);

            if (!await EmployeeExtendedProcedureSupportsRoleCodeAsync())
            {
                var fallbackData = await ExecuteSQL<EmployeeExtendedModel>(
                    BuildEmployeeExtendedSql(includePaging: true),
                    BuildEmployeeExtendedTextParams(request, offset, limit),
                    System.Data.CommandType.Text);

                return new BaseList
                {
                    Total = fallbackData.FirstOrDefault()?.TotalRecord ?? 0,
                    Data = fallbackData
                };
            }

            var result = await this.GetBaseAll<EmployeeExtendedModel>(request,
            new
            {
                offset,
                limit,
                fromDate = request.From,
                toDate = request.To,
                request.Status,
                request.StatusWork,
                request.DocumentStatus,
                request.Token,
                request.GroupId,
                request.MemberId,
                request.RoleCode,
                IsDeleted = request.IsDeleted ?? false,
                UserId = request.UserId,
                OrderBy = request.OrderBy
            }, sqlPro: "sp_Employee_getAll_Extended");
            return result;
        }

        private async Task<bool> EmployeeExtendedProcedureSupportsRoleCodeAsync()
        {
            using var con = GetConnection();
            const string sql = @"
SELECT COUNT(1)
FROM sys.parameters
WHERE object_id = OBJECT_ID('dbo.sp_Employee_getAll_Extended')
  AND name = '@RoleCode';";

            var count = await con.ExecuteScalarAsync<int>(sql);
            return count > 0;
        }

        private static object BuildEmployeeExtendedTextParams(EmployeeRequest request, int? offset = null, int? limit = null)
        {
            return new
            {
                Token = (request.Token ?? string.Empty).Trim(),
                UserId = request.UserId ?? -1,
                RoleCode = (request.RoleCode ?? string.Empty).Trim(),
                GroupId = request.GroupId ?? -1,
                Status = request.Status ?? -1,
                StatusWork = request.StatusWork,
                DocumentStatus = request.DocumentStatus,
                fromDate = request.From,
                toDate = request.To,
                offset = offset ?? 0,
                limit = limit ?? (request.Limit > 0 ? request.Limit : 10)
            };
        }

        private static string BuildEmployeeExtendedSql(bool includePaging)
        {
            var selectPrefix = includePaging ? "COUNT(d.Id) OVER() AS TotalRecord,\n        " : string.Empty;
            var pagingClause = includePaging ? "\n    OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY" : string.Empty;

            return $@"
    SELECT 
        {selectPrefix}d.*, 
        dbo.getDisplayMasterData(d.Status) AS StatusText,
        dbo.getDisplayMasterData(d.StatusWork) AS StatusWorkText,
        dbo.getDisplayMasterData(d.DocumentStatus) AS DocumentStatusText,
        dbo.getDisplayMasterdata(d.DepartmentCode) AS DepartmentText,
        dbo.getDisplayMasterdata(d.PositionCode) AS PositionText,
        COALESCE(
            (SELECT TOP 1 Name FROM MasterData md WHERE md.Code = d.EducationLevel AND md.TypeData = 14 AND ISNULL(md.Deleted,0)=0),
            dbo.getDisplayMasterData(d.EducationLevel),
            d.EducationLevel
        ) AS EducationLevelText,
        COALESCE(
            (SELECT TOP 1 Name FROM MasterData md WHERE md.Code = d.Maritalstatus AND md.TypeData = 13 AND ISNULL(md.Deleted,0)=0),
            dbo.getDisplayMasterData(d.Maritalstatus),
            d.Maritalstatus
        ) AS MaritalstatusText,
        COALESCE(
            (SELECT TOP 1 Name
             FROM MasterData md
             WHERE md.TypeData = 21
               AND ISNULL(md.Deleted,0)=0
               AND (md.Code = d.Ethnicity OR md.Id = TRY_CONVERT(int, d.Ethnicity))),
            dbo.getDisplayMasterData(TRY_CONVERT(int, d.Ethnicity)),
            NULLIF(d.Ethnicity, '')
        ) AS EthnicityText,
        COALESCE(
            (SELECT TOP 1 Name FROM MasterData md WHERE md.Code = d.Religion AND md.TypeData = 20 AND ISNULL(md.Deleted,0)=0),
            dbo.getDisplayMasterData(d.Religion),
            d.Religion
        ) AS ReligionText,
        dbo.getFullName(d.ManagerId) AS ManagerName,
        gm.GroupId,
        g.Name AS GroupName,
        
        (SELECT TOP 1 NoAgree FROM hdldItem h WHERE h.UserId = CAST(d.Id AS NVARCHAR(50)) AND ISNULL(h.Deleted,0)=0 ORDER BY h.Start DESC, h.Id DESC) as HD_SoHD,
        (SELECT TOP 1 Start FROM hdldItem h WHERE h.UserId = CAST(d.Id AS NVARCHAR(50)) AND ISNULL(h.Deleted,0)=0 ORDER BY h.Start DESC, h.Id DESC) as HD_NgayBatDau,
        (SELECT TOP 1 [End] FROM hdldItem h WHERE h.UserId = CAST(d.Id AS NVARCHAR(50)) AND ISNULL(h.Deleted,0)=0 ORDER BY h.Start DESC, h.Id DESC) as HD_NgayKetThuc,
        (SELECT TOP 1 dbo.getDisplayMasterData(CodeId) FROM hdldItem h WHERE h.UserId = CAST(d.Id AS NVARCHAR(50)) AND ISNULL(h.Deleted,0)=0 ORDER BY h.Start DESC, h.Id DESC) as HD_LoaiHD,

        (SELECT TOP 1 Number FROM TaxItem t WHERE t.UserName = d.UserName AND ISNULL(t.Deleted,0)=0 ORDER BY t.Id DESC) as Tax_MST,
        (SELECT TOP 1 PITDate FROM TaxItem t WHERE t.UserName = d.UserName AND ISNULL(t.Deleted,0)=0 ORDER BY t.Id DESC) as Tax_NgayCap,
        (SELECT TOP 1 EffectedFrom FROM TaxItem t WHERE t.UserName = d.UserName AND ISNULL(t.Deleted,0)=0 ORDER BY t.Id DESC) as Tax_NgayHieuLuc,
        (SELECT TOP 1 Dependent FROM TaxItem t WHERE t.UserName = d.UserName AND ISNULL(t.Deleted,0)=0 ORDER BY t.Id DESC) as Tax_NguoiPhuThuoc,

        (SELECT TOP 1 NumberCode FROM BHXHItem b WHERE b.UserName = d.UserName AND ISNULL(b.Deleted,0)=0 ORDER BY b.Id DESC) as BHXH_SoSo,
        (SELECT TOP 1 RegBHYT FROM BHXHItem b WHERE b.UserName = d.UserName AND ISNULL(b.Deleted,0)=0 ORDER BY b.Id DESC) as BHXH_NoiDangKy,
        (SELECT TOP 1 StartMonth FROM BHXHItem b WHERE b.UserName = d.UserName AND ISNULL(b.Deleted,0)=0 ORDER BY b.Id DESC) as BHXH_ThangBatDau,

        (SELECT TOP 1 Name FROM RelationItem r WHERE r.UserName = d.UserName AND ISNULL(r.Deleted,0)=0 ORDER BY r.Id DESC) as RelationName,
        (SELECT TOP 1 Relationcode FROM RelationItem r WHERE r.UserName = d.UserName AND ISNULL(r.Deleted,0)=0 ORDER BY r.Id DESC) as RelationCode,
        (SELECT TOP 1 dbo.getDisplayMasterData(Relationcode) FROM RelationItem r WHERE r.UserName = d.UserName AND ISNULL(r.Deleted,0)=0 ORDER BY r.Id DESC) as RelationText,
        (SELECT TOP 1 Phone FROM RelationItem r WHERE r.UserName = d.UserName AND ISNULL(r.Deleted,0)=0 ORDER BY r.Id DESC) as RelationPhone,
        (SELECT TOP 1 AddressInfo FROM RelationItem r WHERE r.UserName = d.UserName AND ISNULL(r.Deleted,0)=0 ORDER BY r.Id DESC) as RelationAddress

    FROM Employees d
    LEFT JOIN GroupMember gm ON d.Id = gm.MemberId AND ISNULL(gm.Deleted, 0) = 0
    LEFT JOIN [Group] g ON gm.GroupId = g.Id AND ISNULL(g.Deleted, 0) = 0
    WHERE ISNULL(d.Deleted, 0) = 0
      AND (@Token = '' OR d.UserName LIKE N'%' + @Token + '%' OR d.FullName LIKE N'%' + @Token + '%' OR d.Phone LIKE N'%' + @Token + '%')
      AND (@GroupId <= 0 OR gm.GroupId = @GroupId)
      AND (@Status < 0 OR d.IsActive = @Status)
      AND (@StatusWork IS NULL OR @StatusWork = '' OR @StatusWork = '-1' OR d.StatusWork = @StatusWork)
      AND (@DocumentStatus IS NULL OR @DocumentStatus = '' OR @DocumentStatus = '-1' OR d.DocumentStatus = @DocumentStatus)
      AND (@fromDate IS NULL OR d.CreateAt >= @fromDate)
      AND (@toDate IS NULL OR d.CreateAt <= @toDate)
      AND (
            @UserId <= 0
            OR @RoleCode IN ('1', '8', '9')
            OR (
                d.Id IN (SELECT Id FROM getAllUserByUserId(@UserId))
                AND d.Id <> @UserId
            )
          )
    ORDER BY d.UpdateAt DESC{pagingClause}";
        }

        public async Task<BaseList> GetAllManager(int leadGroup = -1)
        {
            var result = await this.GetBaseAll<ManagerLeadIndex>(new BaseRequest { },
            new
            {
                groupLeadId = leadGroup
            }, sqlPro: "sp_group_getAllLead");
            return result;
        }

        public async Task<Account> GetByLineCode(string lineCode)
        {
            using (var con = GetConnection())
            {
                var sql = "select top 1  * from Employees d where d.LineCode = @LineCode  ";
                var result = await con.QuerySingleOrDefaultAsync<Account>(sql, new { LineCode = lineCode });
                return result;
            }
        }

        public async Task<BaseList> GetLeaveBalances(LeaveBalanceRequest request)
        {
            var page = request.Page;
            var limit = request.Limit;
            ProcessInputPaging(ref page, ref limit, out var offset);

            request.Page = page;
            request.Limit = limit;

            var sql = @"
                ;WITH EmployeeBase AS
                (
                    SELECT
                        d.Id,
                        d.UserName,
                        d.FullName,
                        d.DepartmentCode,
                        dbo.getDisplayMasterdata(d.DepartmentCode) AS DepartmentText,
                        d.PositionCode,
                        dbo.getDisplayMasterdata(d.PositionCode) AS PositionText,
                        d.StatusWork,
                        dbo.getDisplayMasterdata(d.StatusWork) AS StatusWorkText,
                        ISNULL(d.AllowedLeaveDays, 0) AS AllowedLeaveDays,
                        ISNULL(d.CarryOverLeaveDays, 0) AS CarryOverLeaveDays,
                        ISNULL(d.ExpiredLeaveDays, 0) AS ExpiredLeaveDays,
                        ISNULL(d.UsedLeaveDays, 0) AS UsedLeaveDays
                    FROM Employees d
                    WHERE ISNULL(d.Deleted, 0) = 0
                      AND (@Token = '' OR d.UserName LIKE N'%' + @Token + '%' OR d.FullName LIKE N'%' + @Token + '%' OR d.Phone LIKE N'%' + @Token + '%')
                      AND (@StatusWork IS NULL OR @StatusWork = '' OR @StatusWork = '-1' OR d.StatusWork = @StatusWork)
                      AND (@DepartmentCode IS NULL OR @DepartmentCode = '' OR @DepartmentCode = '-1' OR d.DepartmentCode = @DepartmentCode)
                      AND (@PositionCode IS NULL OR @PositionCode = '' OR @PositionCode = '-1' OR d.PositionCode = @PositionCode)
                ),
                LeaveAgg AS
                (
                    SELECT
                        l.EmployeeId,
                        SUM(CASE WHEN l.Status IN (3,4) AND l.LeaveTypeCode = 'NP' THEN ISNULL(l.NumDays, 0) ELSE 0 END) AS UsedAnnualLeaveDays,
                        SUM(CASE WHEN l.Status IN (3,4) AND l.LeaveTypeCode = 'NB' THEN ISNULL(l.NumDays, 0) ELSE 0 END) AS UsedSickLeaveDays,
                        SUM(CASE WHEN l.Status IN (3,4) AND l.LeaveTypeCode = 'NVR' THEN ISNULL(l.NumDays, 0) ELSE 0 END) AS UsedPersonalLeaveDays,
                        SUM(CASE WHEN l.Status IN (3,4) AND l.LeaveTypeCode = 'NTS' THEN ISNULL(l.NumDays, 0) ELSE 0 END) AS UsedMaternityLeaveDays,
                        SUM(CASE WHEN l.Status IN (3,4) AND l.LeaveTypeCode = 'NKL' THEN ISNULL(l.NumDays, 0) ELSE 0 END) AS UsedUnpaidLeaveDays
                    FROM LeaveRequests l
                    WHERE ISNULL(l.Deleted, 0) = 0
                    GROUP BY l.EmployeeId
                )
                SELECT
                    COUNT(1) OVER() AS TotalRecord,
                    e.Id,
                    e.UserName,
                    e.FullName,
                    e.DepartmentCode,
                    e.DepartmentText,
                    e.PositionCode,
                    e.PositionText,
                    e.StatusWork,
                    e.StatusWorkText,
                    e.AllowedLeaveDays,
                    e.CarryOverLeaveDays,
                    e.ExpiredLeaveDays,
                    e.UsedLeaveDays,
                    e.AllowedLeaveDays + e.CarryOverLeaveDays - e.ExpiredLeaveDays - e.UsedLeaveDays AS RemainingLeaveDays,
                    CASE
                        WHEN e.UsedLeaveDays > ISNULL(la.UsedAnnualLeaveDays, 0) THEN e.UsedLeaveDays
                        ELSE ISNULL(la.UsedAnnualLeaveDays, 0)
                    END AS UsedAnnualLeaveDays,
                    ISNULL(la.UsedSickLeaveDays, 0) AS UsedSickLeaveDays,
                    ISNULL(la.UsedPersonalLeaveDays, 0) AS UsedPersonalLeaveDays,
                    ISNULL(la.UsedMaternityLeaveDays, 0) AS UsedMaternityLeaveDays,
                    ISNULL(la.UsedUnpaidLeaveDays, 0) AS UsedUnpaidLeaveDays,
                    CASE
                        WHEN e.UsedLeaveDays > ISNULL(la.UsedAnnualLeaveDays, 0) THEN e.UsedLeaveDays
                        ELSE ISNULL(la.UsedAnnualLeaveDays, 0)
                    END
                    + ISNULL(la.UsedSickLeaveDays, 0)
                    + ISNULL(la.UsedPersonalLeaveDays, 0)
                    + ISNULL(la.UsedMaternityLeaveDays, 0)
                    + ISNULL(la.UsedUnpaidLeaveDays, 0) AS TotalApprovedLeaveDays
                FROM EmployeeBase e
                LEFT JOIN LeaveAgg la ON e.Id = la.EmployeeId
                ORDER BY e.FullName
                OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;";

            using var con = GetConnection();
            var data = (await con.QueryAsync<LeaveBalanceIndexModel>(sql, new
            {
                Offset = offset,
                Limit = limit,
                Token = request.Token ?? string.Empty,
                StatusWork = request.StatusWork,
                DepartmentCode = request.DepartmentCode,
                PositionCode = request.PositionCode
            })).ToList();

            return new BaseList
            {
                Total = data.FirstOrDefault()?.TotalRecord ?? 0,
                Data = data
            };
        }

        public async Task<bool> UpdateLeaveBalance(int employeeId, decimal? allowedLeaveDays, decimal? carryOverLeaveDays, decimal? expiredLeaveDays, int userId)
        {
            var p = new DynamicParameters();
            p.Add("@EmployeeId", employeeId);
            p.Add("@AllowedLeaveDays", allowedLeaveDays);
            p.Add("@CarryOverLeaveDays", carryOverLeaveDays);
            p.Add("@ExpiredLeaveDays", expiredLeaveDays);
            p.Add("@UpdatedBy", userId);

            return await ExecuteSQL("sp_Employee_UpdateLeaveBalance", p, CommandType.StoredProcedure);
        }


        public async Task<bool> Delete(int id, bool reactive)
        {
            return await this.DeleteBase(id, tableDelete: "", delete: reactive == false ? 1 : 0);
        }


    }
}
