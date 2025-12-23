using Dapper;
using Microsoft.Extensions.Configuration;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class DocumentDataRep : RepositoryBase<DocumentData>, IDocumentDataRep
    {
        public DocumentDataRep(IConfiguration configuration)
            : base(configuration)
        {
            tableName = "DocumentData";
            sqlGetALl = "sp_DocumentData_getAll";
        }

        private async Task<bool> Update(DocumentData item)
        {
            var parameter = new
            {
                item.Id,
                item.DisplayText,
                item.ValueFile,
                item.ParentId,
                item.AccessLevel,
                UpdatedBy = item.UpdatedBy
            };
            return await this.ExecuteSQL("sp_DocumentData_update", parameter);
        }

        private async Task<bool> Add(DocumentData item)
        {
            var parameter = new
            {
                item.RelId,
                item.RelCode,
                item.DisplayText,
                item.ValueFile,
                item.Code,
                DataType = item.dataType,
                item.ParentId,
                item.IsFolder,
                item.AccessLevel,
                item.CreatedBy
            };
            return await this.ExecuteSQL("sp_DocumentData_insert", parameter);
        }

        public async Task<bool> AddOrUpdate(DocumentData item)
        {
            if (item.Id > 0)
            {
                return await Update(item);
            }
            return await Add(item);
        }

        public async Task<BaseList> GetAll(DocumentDataRquest request)
        {
            var result = await GetBaseAll<DocumentDataIndexModel>(request,
            new
            {
                request.Token,
                request.RelId,
                request.RelCode,
                request.DataType,
                request.ParentId,
                request.CurrentUserId,
                request.Limit,
                request.Page,
                request.OrderBy
            });
            return result;
        }

        public async Task<List<int>> GetShares(int documentId)
        {
            var sql = "SELECT UserId FROM DocumentShares WHERE DocumentId = @documentId";
            using (var _con = GetConnection())
            {
                var result = await _con.QueryAsync<int>(sql, new { documentId });
                return result.ToList();
            }
        }

        public async Task<bool> AddShare(int documentId, int userId)
        {
            var sql = "IF NOT EXISTS (SELECT 1 FROM DocumentShares WHERE DocumentId = @documentId AND UserId = @userId) " +
                      "INSERT INTO DocumentShares (DocumentId, UserId) VALUES (@documentId, @userId)";
            using (var _con = GetConnection())
            {
                var affected = await _con.ExecuteAsync(sql, new { documentId, userId });
                return affected > 0;
            }
        }

        public async Task<bool> RemoveShare(int documentId, int userId)
        {
            var sql = "DELETE FROM DocumentShares WHERE DocumentId = @documentId AND UserId = @userId";
            using (var _con = GetConnection())
            {
                var affected = await _con.ExecuteAsync(sql, new { documentId, userId });
                return affected > 0;
            }
        }

        public async Task<DocumentData?> GetByToken(string token)
        {
            var sql = "SELECT * FROM DocumentData WHERE ShareToken = @token AND ISNULL(Deleted,0) = 0";
            using (var _con = GetConnection())
            {
                return await _con.QueryFirstOrDefaultAsync<DocumentData>(sql, new { token });
            }
        }
    }
}
