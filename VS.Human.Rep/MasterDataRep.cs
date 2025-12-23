using Dapper;
using Microsoft.Extensions.Configuration;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class MasterDataRep : RepositoryBase<MasterData>, IMasterDataRep
    {

        public MasterDataRep(IConfiguration configuration)
            : base(configuration)
        {

            tableName = "MasterData";
            sqlGetALl = "sp_masterData_getAll";
        }

        private async Task<bool> Update(MasterData item)
        {
            var parameter = new
            {
                item.Id,
                item.Name,
                item.Noted,
                item.Extra,
                item.IsActive,
                item.ApplyFor,
                item.UpdatedBy,

            };
            return await this.ExecuteSQL("sp_masterData_update", parameter);
        }
        private async Task<bool> Add(MasterData item)
        {
            var parameter = new
            {
                item.Name,
                item.Noted,
                item.Extra,
                item.ApplyFor,
                item.TypeData,
                item.CreatedBy
            };
            return await this.ExecuteSQL("sp_masterData_insert", parameter);
        }

        public async Task<bool> AddOrUpdate(MasterData item)
        {
            if (item.Id > 0)
            {
                var itemUpdate = await GetById(item.Id);
                if (itemUpdate != null)
                {
                    itemUpdate.Name = item.Name;
                    itemUpdate.Extra = item.Extra;
                    itemUpdate.ApplyFor = item.ApplyFor;
                    itemUpdate.UpdatedBy = item.UpdatedBy;
                    itemUpdate.Noted = item.Noted;
                    itemUpdate.IsActive = item.IsActive;
                    return await Update(itemUpdate);
                }
            }
            return await Add(item);
        }

        public async Task<BaseList> GetAll(CommonRequest request)
        {
            var result = await GetBaseAll<CommonIndexModel>(request,
            new
            {
                request.Token,
                request.From,
                request.To,
                request.Type,
                request.UserId,
                request.ApplyFor,
                request.Limit,
                request.Page,
                request.OrderBy
            });
            return result;
        }

        public async Task<MasterData?> GetByCode(string code, int typeData)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            var sql = "SELECT TOP 1 * FROM MasterData WHERE Code = @code AND TypeData = @typeData AND ISNULL(Deleted,0) = 0";
            return await ExecuteSQL<MasterData>(sql, new { code, typeData });
        }

        public async Task<MasterData?> GetByName(string name, int typeData)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var sql = "SELECT TOP 1 * FROM MasterData WHERE Name = @name AND TypeData = @typeData AND ISNULL(Deleted,0) = 0";
            return await ExecuteSQL<MasterData>(sql, new { name, typeData });
        }

        public async Task<int> GetNextAvailableTypeData()
        {
            var sql = "SELECT ISNULL(MAX(TypeData), 100) + 1 FROM MasterData WHERE ISNULL(Deleted,0) = 0";
            var result = await ExecuteSQLScalar<int>(sql, new { });
            // Ensure we start from at least 100 to avoid conflicts with existing system types
            return result < 100 ? 100 : result;
        }

        public async Task<List<MasterData>> GetByTypeData(int typeData)
        {
            var sql = "SELECT * FROM MasterData WHERE TypeData = @typeData AND ISNULL(Deleted,0) = 0";
            using (var _con = GetConnection())
            {
                var result = await _con.QueryAsync<MasterData>(sql, new { typeData });
                return result.ToList();
            }
        }
    }
}
