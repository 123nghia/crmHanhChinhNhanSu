using Microsoft.Extensions.Configuration;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class GroupRep : RepositoryBase<GroupItem>, IGroupRep
    {

        public GroupRep(IConfiguration configuration)
            : base(configuration)
        {

            tableName = "Group";


            sqlGetALl = "sp_Group_getAll";
        }

        private async Task<bool> Update(GroupItem item)
        {
            var parameter = new
            {
                item.Id,
                item.Name,
                item.UpdatedBy,
                item.Status,
                item.ManagerId,
                item.IsActive
            };
            return await this.ExecuteSQL("sp_group_update", parameter);
        }
        private async Task<bool> Add(GroupItem item)
        {
            var itemInsert = new GroupItem()
            {
                Name = item.Name,
                ManagerId = item.ManagerId,
                IsActive = 1,
                Status = item.Status,
                Deleted = false,
                CreatedBy = item.CreatedBy,
                UpdatedBy = item.UpdatedBy,
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now,

            };
            var parameter = new
            {
                itemInsert.Name,
                itemInsert.ManagerId,
                itemInsert.IsActive,
                itemInsert.Status,
                itemInsert.CreatedBy,
                itemInsert.CreateAt
            };
            return await this.ExecuteSQL("sp_group_insert", parameter);
        }

        public async Task<bool> AddOrUpdateMember(GroupMember item)
        {
            if (item.Id < 0)
            {
                return await AddMember(item);
            }
            else
            {
                return await UpdateMember(item);
            }
        }

        private async Task<bool> AddMember(GroupMember item)
        {
            var itemInsert = new GroupMember()
            {
                MemberId = item.MemberId,
                GroupId = item.GroupId,
                Deleted = false,
                CreatedBy = item.CreatedBy,
                UpdatedBy = item.UpdatedBy,
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now,

            };
            var parameter = new
            {
                itemInsert.MemberId,
                itemInsert.GroupId,

                itemInsert.CreatedBy,
                itemInsert.CreateAt
            };
            return await this.ExecuteSQL("sp_groupEmp_insert", parameter);
        }
        private async Task<bool> UpdateMember(GroupMember item)
        {
            return true;
            var parameter = new
            {
                item.Id,
                item.GroupId,
                item.MemberId,

            };

            return await this.ExecuteSQL("sp_Groupemp_update", parameter);
        }

        public async Task<bool> AddOrUpdate(GroupItem item)
        {
            if (item.Id > 0)
            {
                var itemUpdate = await GetById(item.Id);
                if (itemUpdate != null)
                {
                    itemUpdate.Name = item.Name;
                    itemUpdate.UpdatedBy = item.UpdatedBy;
                    itemUpdate.ManagerId = item.ManagerId;
                    itemUpdate.Status = item.Status;
                    itemUpdate.IsActive = item.IsActive;

                    return await Update(itemUpdate);
                }
            }
            return await Add(item);
        }

        public async Task<BaseList> GetAll(GroupRequest request)
        {
            var result = await this.GetBaseAll<GroupIndexModel>(request,
            new
            {
                request.Token,
                request.From,
                request.To,
                request.Status,
                request.UserId,
                request.Limit,

                request.Page,
                request.OrderBy
            });
            return result;
        }

        public async Task<bool> Delete(int id)
        {
            return await DeleteBase(id, tableDelete: "Group");
        }



        public async Task<bool> DeleteMember(int id)
        {
            return await DeleteBase(id, tableDelete: "groupmember");
        }

        public async Task<BaseList> GetAllMember(int groupId)
        {
            var result = await this.GetBaseAll<GroupMemberIndexModel>
                (new GroupMemberRequest()
                {

                    GroupId = groupId
                }, new
                {
                    GroupId = groupId
                }, "sp_group_getAllMember"
                );

            return result;
        }

        public async Task<BaseList> GetAllMemberNotGroup()
        {
            var result = await this.GetBaseAll<EmployeeIndexModel>
                (new GroupMemberRequest()
                { }, new { }, "sp_group_getAllMemberNotGroup"
                );

            return result;
        }




    }
}
