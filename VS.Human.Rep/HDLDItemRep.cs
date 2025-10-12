using Microsoft.Extensions.Configuration;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class HDLDItemRep : RepositoryBase<HDLD>, IHDLDItemRep
    {

        public HDLDItemRep(IConfiguration configuration)
            : base(configuration)
        {

            tableName = "hdldItem";
            sqlGetALl = "sp_hdldItem_getAll";
        }

        private async Task<bool> Update(HDLD item)
        {
            var parameter = new
            {
                item.Id,
                item.NoAgree,
                item.Start,
                item.End,
                item.CodeId,
                item.UserId,
                item.UpdatedBy,

            };
            return await this.ExecuteSQL("sp_hdldItem_update", parameter);

        }

        private async Task<bool> Add(HDLD item)
        {
            var parameter = new
            {
                item.NoAgree,
                item.Start,
                item.End,
                item.CodeId,
                item.UserId,
                item.CreatedBy
            };
            return await this.ExecuteSQL("sp_hdldItem_insert", parameter);
        }

        public async Task<bool> AddOrUpdate(HDLD item)
        {


            if (item.Id > 0)
            {
                var itemUpdate = await GetById(item.Id);
                if (itemUpdate != null)
                {
                    itemUpdate.NoAgree = item.NoAgree;
                    itemUpdate.Start = item.Start;
                    itemUpdate.End = item.End;
                    itemUpdate.CodeId = item.CodeId;
                    itemUpdate.UserId = item.UserId;
                    itemUpdate.UpdatedBy = item.UpdatedBy;
                    return await Update(itemUpdate);
                }
            }
            return await Add(item);
        }

        public async Task<HDLD> GetAll(string userid)
        {
            var data = await GetFirstRecordBySql<HDLD>("sp_hdldItem_getAll", new
            {
                UserName = userid
            });
            return data;
        }
        public async Task<HDLD> GetInfo(string userName)
        {
            var data = await GetFirstRecordBySql<HDLD>("sp_hdldItem_getAll", new
            {
                userid = userName
            });
            return data;
        }

    }
}
