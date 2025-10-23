using Microsoft.Extensions.Configuration;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class RelationItemRep : RepositoryBase<RelationItem>, IRelationItemRep
    {

        public RelationItemRep(IConfiguration configuration)
            : base(configuration)
        {

            tableName = "RelationItem";
            sqlGetALl = "sp_RelationItem_getAll";
        }

        private async Task<bool> Update(RelationItem item)
        {
            var parameter = new
            {
                item.Id,
                item.Relationcode,
                item.Name,
                item.Phone,
                item.Noted,
                item.AddressInfo,
                item.UpdatedBy,

            };
            return await this.ExecuteSQL("sp_RelationItem_update", parameter);
        }

        private async Task<bool> Add(RelationItem item)
        {
            var parameter = new
            {
                item.UserName,
                item.Relationcode,
                item.Name,
                item.Phone,
                item.Noted,
                item.AddressInfo,
                item.CreatedBy
            };
            return await this.ExecuteSQL("sp_RelationItem_insert", parameter);
        }

        public async Task<bool> AddOrUpdate(RelationItem item)
        {


            if (item.Id > 0)
            {
                var itemUpdate = await GetById(item.Id);
                if (itemUpdate != null)
                {
                    itemUpdate.Name = item.Name;
                    itemUpdate.AddressInfo = item.AddressInfo;
                    itemUpdate.Noted = item.Noted;
                    itemUpdate.Phone = item.Phone;
                    itemUpdate.Relationcode = item.Relationcode;
                    itemUpdate.UpdatedBy = item.UpdatedBy;
                    return await Update(itemUpdate);
                }
            }
            return await Add(item);
        }

        public async Task<RelationItem> GetAll(string userName)
        {
            var data = await GetFirstRecordBySql<RelationItem>("sp_RelationItem_getAll", new
            {
                UserName = userName
            });
            return data;
        }
        public async Task<RelationItem> GetInfo(string userName)
        {
            var data = await GetFirstRecordBySql<RelationItem>("sp_RelationItem_getAll", new
            {
                UserName = userName
            });
            return data;
        }

    }
}
