using Dapper;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Mail;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class MailGroupRep : RepositoryBase<MailGroupItem>, IMailGroupRep
    {
        public MailGroupRep(IConfiguration configuration)
            : base(configuration)
        {
            tableName = "MailGroups";
        }

        public async Task<BaseList> GetAll(MailGroupRequest request)
        {
            var page = request.Page;
            var limit = request.Limit;
            ProcessInputPaging(ref page, ref limit, out var offset);

            using var con = GetConnection();

            const string companyCountSql = @"
SELECT COUNT(1)
FROM Employees
WHERE ISNULL(Deleted, 0) = 0
  AND ISNULL(IsActive, 0) = 1
  AND NULLIF(LTRIM(RTRIM(Email)), '') IS NOT NULL;";

            const string personalCountSql = @"
SELECT COUNT(1)
FROM Employees
WHERE ISNULL(Deleted, 0) = 0
  AND ISNULL(IsActive, 0) = 1
  AND NULLIF(LTRIM(RTRIM(PersonalEmail)), '') IS NOT NULL;";

            var companyCount = await con.ExecuteScalarAsync<int>(companyCountSql);
            var personalCount = await con.ExecuteScalarAsync<int>(personalCountSql);

            const string sql = @"
SELECT
    COUNT(1) OVER() AS TotalRecord,
    g.Id,
    g.Name,
    g.Description,
    g.IncludeAllCompanyEmails,
    g.IncludeAllPersonalEmails,
    g.IsBlocked,
    g.CreateAt,
    g.CreatedBy,
    g.UpdateAt,
    g.UpdatedBy,
    ISNULL((
        SELECT COUNT(1)
        FROM MailGroupRecipients r
        WHERE r.MailGroupId = g.Id
          AND ISNULL(r.Deleted, 0) = 0
          AND ISNULL(r.IsBlocked, 0) = 0
    ), 0)
    + CASE WHEN g.IncludeAllCompanyEmails = 1 THEN @CompanyCount ELSE 0 END
    + CASE WHEN g.IncludeAllPersonalEmails = 1 THEN @PersonalCount ELSE 0 END AS RecipientCount
FROM MailGroups g
WHERE ISNULL(g.Deleted, 0) = 0
  AND (@Token = '' OR g.Name LIKE N'%' + @Token + '%' OR g.Description LIKE N'%' + @Token + '%')
ORDER BY g.UpdateAt DESC, g.Id DESC
OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;";

            var data = (await con.QueryAsync<MailGroupIndexModel>(sql, new
            {
                Token = request.Token ?? string.Empty,
                CompanyCount = companyCount,
                PersonalCount = personalCount,
                Offset = offset,
                Limit = limit
            })).ToList();

            return new BaseList
            {
                Total = data.FirstOrDefault()?.TotalRecord ?? 0,
                Data = data
            };
        }

        public async Task<List<MailGroupItem>> GetSelectableGroupsAsync()
        {
            using var con = GetConnection();
            const string sql = @"
SELECT
    Id,
    Name,
    Description,
    IncludeAllCompanyEmails,
    IncludeAllPersonalEmails,
    IsBlocked,
    CreateAt,
    CreatedBy,
    UpdateAt,
    UpdatedBy,
    Deleted,
    IsActive
FROM MailGroups
WHERE ISNULL(Deleted, 0) = 0
  AND ISNULL(IsActive, 0) = 1
  AND ISNULL(IsBlocked, 0) = 0
ORDER BY Name;";

            var result = await con.QueryAsync<MailGroupItem>(sql);
            return result.ToList();
        }

        public async Task<MailGroupItem> GetById(int id)
        {
            using var con = GetConnection();
            const string sql = @"
SELECT TOP 1 *
FROM MailGroups
WHERE Id = @Id
  AND ISNULL(Deleted, 0) = 0;";

            var group = await con.QueryFirstOrDefaultAsync<MailGroupItem>(sql, new { Id = id });
            if (group == null)
            {
                return new MailGroupItem { Id = -1 };
            }

            const string recipientSql = @"
SELECT *
FROM MailGroupRecipients
WHERE MailGroupId = @MailGroupId
  AND ISNULL(Deleted, 0) = 0
ORDER BY Id;";

            var recipients = await con.QueryAsync<MailGroupRecipient>(recipientSql, new { MailGroupId = id });
            group.Recipients = recipients.ToList();
            return group;
        }

        public async Task<bool> Save(MailGroupItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Name))
            {
                return false;
            }

            var cleanRecipients = item.Recipients?
                .Select(NormalizeRecipient)
                .Where(x => x != null)
                .GroupBy(x => x!.Email!, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First()!)
                .ToList() ?? new List<MailGroupRecipient>();

            using var con = GetConnection();
            using var tran = con.BeginTransaction();
            try
            {
                if (item.Id > 0)
                {
                    const string updateSql = @"
UPDATE MailGroups
SET Name = @Name,
    Description = @Description,
    IncludeAllCompanyEmails = @IncludeAllCompanyEmails,
    IncludeAllPersonalEmails = @IncludeAllPersonalEmails,
    IsBlocked = @IsBlocked,
    IsActive = @IsActive,
    UpdatedBy = @UpdatedBy,
    UpdateAt = GETDATE()
WHERE Id = @Id
  AND ISNULL(Deleted, 0) = 0;";

                    var affected = await con.ExecuteAsync(updateSql, item, tran);
                    if (affected <= 0)
                    {
                        tran.Rollback();
                        return false;
                    }
                }
                else
                {
                    const string insertSql = @"
INSERT INTO MailGroups
(
    Name,
    Description,
    IncludeAllCompanyEmails,
    IncludeAllPersonalEmails,
    IsBlocked,
    Deleted,
    IsActive,
    CreatedBy,
    UpdatedBy,
    CreateAt,
    UpdateAt
)
VALUES
(
    @Name,
    @Description,
    @IncludeAllCompanyEmails,
    @IncludeAllPersonalEmails,
    @IsBlocked,
    0,
    @IsActive,
    @CreatedBy,
    @UpdatedBy,
    GETDATE(),
    GETDATE()
);
SELECT CAST(SCOPE_IDENTITY() AS int);";

                    item.Id = await con.ExecuteScalarAsync<int>(insertSql, item, tran);
                    if (item.Id <= 0)
                    {
                        tran.Rollback();
                        return false;
                    }
                }

                await con.ExecuteAsync(@"
UPDATE MailGroupRecipients
SET Deleted = 1,
    UpdatedBy = @UpdatedBy,
    UpdateAt = GETDATE()
WHERE MailGroupId = @MailGroupId
  AND ISNULL(Deleted, 0) = 0;", new
                {
                    MailGroupId = item.Id,
                    UpdatedBy = item.UpdatedBy > 0 ? item.UpdatedBy : item.CreatedBy
                }, tran);

                const string recipientInsertSql = @"
INSERT INTO MailGroupRecipients
(
    MailGroupId,
    Email,
    DisplayName,
    IsBlocked,
    Deleted,
    IsActive,
    CreatedBy,
    UpdatedBy,
    CreateAt,
    UpdateAt
)
VALUES
(
    @MailGroupId,
    @Email,
    @DisplayName,
    @IsBlocked,
    0,
    1,
    @CreatedBy,
    @UpdatedBy,
    GETDATE(),
    GETDATE()
);";

                foreach (var recipient in cleanRecipients)
                {
                    recipient.MailGroupId = item.Id;
                    recipient.CreatedBy = item.Id > 0 && item.CreatedBy > 0 ? item.CreatedBy : item.UpdatedBy;
                    recipient.UpdatedBy = item.UpdatedBy > 0 ? item.UpdatedBy : item.CreatedBy;
                    recipient.IsActive = 1;
                    await con.ExecuteAsync(recipientInsertSql, recipient, tran);
                }

                tran.Commit();
                return true;
            }
            catch
            {
                tran.Rollback();
                return false;
            }
        }

        public Task<bool> Delete(int id)
        {
            return DeleteBase(id, tableDelete: tableName);
        }

        public async Task<List<string>> ResolveRecipientsAsync(IEnumerable<int> groupIds)
        {
            var ids = groupIds?
                .Where(x => x > 0)
                .Distinct()
                .ToList() ?? new List<int>();

            if (ids.Count == 0)
            {
                return new List<string>();
            }

            using var con = GetConnection();

            const string groupSql = @"
SELECT *
FROM MailGroups
WHERE Id IN @Ids
  AND ISNULL(Deleted, 0) = 0
  AND ISNULL(IsActive, 0) = 1
  AND ISNULL(IsBlocked, 0) = 0;";

            var groups = (await con.QueryAsync<MailGroupItem>(groupSql, new { Ids = ids })).ToList();
            if (groups.Count == 0)
            {
                return new List<string>();
            }

            var recipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (groups.Any(x => x.IncludeAllCompanyEmails || x.IncludeAllPersonalEmails))
            {
                const string employeeSql = @"
SELECT Email, PersonalEmail
FROM Employees
WHERE ISNULL(Deleted, 0) = 0
  AND ISNULL(IsActive, 0) = 1;";

                var employees = await con.QueryAsync(employeeSql);
                var includeCompany = groups.Any(x => x.IncludeAllCompanyEmails);
                var includePersonal = groups.Any(x => x.IncludeAllPersonalEmails);

                foreach (var employee in employees)
                {
                    if (includeCompany)
                    {
                        AddEmail(recipients, (string?)employee.Email);
                    }

                    if (includePersonal)
                    {
                        AddEmail(recipients, (string?)employee.PersonalEmail);
                    }
                }
            }

            const string recipientSql = @"
SELECT Email
FROM MailGroupRecipients
WHERE MailGroupId IN @Ids
  AND ISNULL(Deleted, 0) = 0
  AND ISNULL(IsBlocked, 0) = 0;";

            var manualRecipients = await con.QueryAsync<string>(recipientSql, new { Ids = ids });
            foreach (var email in manualRecipients)
            {
                AddEmail(recipients, email);
            }

            return recipients.ToList();
        }

        private static MailGroupRecipient? NormalizeRecipient(MailGroupRecipient? recipient)
        {
            if (recipient == null)
            {
                return null;
            }

            var normalizedEmail = NormalizeEmail(recipient.Email);
            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                return null;
            }

            return new MailGroupRecipient
            {
                Id = recipient.Id,
                Email = normalizedEmail,
                DisplayName = string.IsNullOrWhiteSpace(recipient.DisplayName) ? null : recipient.DisplayName.Trim(),
                IsBlocked = recipient.IsBlocked
            };
        }

        private static void AddEmail(HashSet<string> recipients, string? email)
        {
            var normalizedEmail = NormalizeEmail(email);
            if (!string.IsNullOrWhiteSpace(normalizedEmail))
            {
                recipients.Add(normalizedEmail);
            }
        }

        private static string? NormalizeEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            try
            {
                return new MailAddress(email.Trim()).Address;
            }
            catch
            {
                return null;
            }
        }
    }
}
