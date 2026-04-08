using Dapper;
using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Linq;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class FormTemplateRep : RepositoryBase<FormTemplateItem>, IFormTemplateRep
    {
        public FormTemplateRep(IConfiguration configuration)
            : base(configuration)
        {
            tableName = "FormTemplates";
        }

        public async Task<BaseList> GetAll(FormTemplateRequest request)
        {
            var page = request.Page;
            var limit = request.Limit;
            ProcessInputPaging(ref page, ref limit, out var offset);

            const string sql = @"
SELECT
    COUNT(1) OVER() AS TotalRecord,
    d.Id,
    d.Title,
    d.FileName,
    d.FilePath,
    d.CreateAt,
    d.CreatedBy,
    d.UpdateAt,
    d.UpdatedBy,
    e.FullName AS AuthorName
FROM FormTemplates d
LEFT JOIN Employees e ON d.CreatedBy = e.Id
WHERE ISNULL(d.Deleted, 0) = 0
  AND (@Token = '' OR d.Title LIKE N'%' + @Token + '%' OR d.Content LIKE N'%' + @Token + '%')
  AND (@From IS NULL OR d.CreateAt >= @From)
  AND (@To IS NULL OR d.CreateAt <= @To)
ORDER BY d.CreateAt DESC, d.Id DESC
OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;";

            using var con = GetConnection();
            var data = (await con.QueryAsync<FormTemplateIndexModel>(sql, new
            {
                Token = request.Token ?? string.Empty,
                request.From,
                request.To,
                Offset = offset,
                Limit = limit
            })).ToList();

            return new BaseList
            {
                Total = data.FirstOrDefault()?.TotalRecord ?? 0,
                Data = data
            };
        }

        public async Task<FormTemplateItem> GetById(int id)
        {
            using var con = GetConnection();
            const string sql = @"
SELECT TOP 1
    d.*,
    e.FullName AS AuthorName
FROM FormTemplates d
LEFT JOIN Employees e ON d.CreatedBy = e.Id
WHERE d.Id = @Id
  AND ISNULL(d.Deleted, 0) = 0;";

            var result = await con.QueryFirstOrDefaultAsync<FormTemplateItem>(sql, new { Id = id });
            return result ?? new FormTemplateItem { Id = -1 };
        }

        public async Task<bool> Save(FormTemplateItem item)
        {
            if (item == null)
            {
                return false;
            }

            using var con = GetConnection();
            if (item.Id > 0)
            {
                const string updateSql = @"
UPDATE FormTemplates
SET Title = @Title,
    Content = @Content,
    FileName = @FileName,
    FilePath = @FilePath,
    FileSize = @FileSize,
    UpdatedBy = @UpdatedBy,
    UpdateAt = GETDATE()
WHERE Id = @Id
  AND ISNULL(Deleted, 0) = 0;";

                var affected = await con.ExecuteAsync(updateSql, item);
                return affected > 0;
            }

            const string insertSql = @"
INSERT INTO FormTemplates
(
    Title,
    Content,
    FileName,
    FilePath,
    FileSize,
    Deleted,
    IsActive,
    CreatedBy,
    UpdatedBy,
    CreateAt,
    UpdateAt
)
VALUES
(
    @Title,
    @Content,
    @FileName,
    @FilePath,
    @FileSize,
    0,
    1,
    @CreatedBy,
    @UpdatedBy,
    GETDATE(),
    GETDATE()
);
SELECT CAST(SCOPE_IDENTITY() AS int);";

            var newId = await con.ExecuteScalarAsync<int>(insertSql, item);
            item.Id = newId;
            return newId > 0;
        }

        public Task<bool> Delete(int id)
        {
            return DeleteBase(id, tableDelete: tableName);
        }
    }
}
