using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class GroupBusiness : BaseBusiness, IGroupBusiness
    {
        private const string RoleHcns = "9";

        public GroupBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {
        }

        public async Task<bool> Add(EmployeeAdd itemAdd)
        {
            var item = new Employee();
            item.FullName = itemAdd.FullName;
            item.Phone = itemAdd.Phone;
            item.RoleCode = itemAdd.RoleCode;
            item.UserName = itemAdd.UserName;
            item.CreatedBy = itemAdd.CreatedBy;
            item.Noted = itemAdd.Noted;
            item.Dob = itemAdd.Dob;
            item.IsActive = itemAdd.IsActive;
            var passNew = getMD5(itemAdd.Pass);
            item.Pass = passNew;
            item.CreateAt = DateTime.Now;
            item.CreatedBy = GetUserId();
            return await _unitOfWork.EmployeeRep.AddOrUpdate(item);
        }

        public async Task<bool> Update(EmployeeAdd itemUpdate)
        {
            var item = new Employee();
            item.FullName = itemUpdate.FullName;
            item.CreatedBy = itemUpdate.CreatedBy;
            item.Id = itemUpdate.Id;
            item.RoleCode = itemUpdate.RoleCode;
            item.Dob = itemUpdate.Dob;
            item.Phone = itemUpdate.Phone;
            item.IsActive = itemUpdate.IsActive;
            item.Noted = itemUpdate.Noted;
            item.UpdatedBy = GetUserId();
            return await _unitOfWork.EmployeeRep.AddOrUpdate(item);
        }

        public Task<bool> Delete(int id)
        {
            return _unitOfWork.GroupRep.Delete(id);
        }

        public async Task<GroupItem> GetById(int id)
        {
            return await _unitOfWork.GroupRep.GetById(id);
        }

        public async Task<bool> AddOrUpdate(GroupItem item)
        {
            return await _unitOfWork.GroupRep.AddOrUpdate(item);
        }

        public async Task<bool> AddOrUpdateMember(GroupMember item)
        {
            return await _unitOfWork.GroupRep.AddOrUpdateMember(item);
        }

        public async Task<bool> DeleteMember(int id)
        {
            return await _unitOfWork.GroupRep.DeleteMember(id);
        }

        public async Task<BaseList> GetAllMember(int groupId)
        {
            return await _unitOfWork.GroupRep.GetAllMember(groupId);
        }

        public async Task<BaseList> GetAllMemberNotGroup()
        {
            return await _unitOfWork.GroupRep.GetAllMemberNotGroup();
        }

        public async Task<BaseList> GetAll(GroupRequest request)
        {
            return await _unitOfWork.GroupRep.GetAll(request);
        }

        public async Task<BaseList> GetAllManager(int leadGroupId = -1)
        {
            var baseList = await _unitOfWork.EmployeeRep.GetAllManager(leadGroupId);
            var managers = baseList.Data?.Cast<ManagerLeadIndex>().ToList() ?? new List<ManagerLeadIndex>();

            var hcnsEmployees = await _unitOfWork.EmployeeRep.GetByRoleCodes(new[] { RoleHcns });
            managers.AddRange(hcnsEmployees
                .Where(x => x.Id > 0)
                .Select(x => new ManagerLeadIndex
                {
                    Id = x.Id,
                    UserName = x.UserName ?? string.Empty,
                    FullName = x.FullName ?? string.Empty,
                    RoleLabel = "HCNS"
                }));

            var mergedManagers = managers
                .Where(x => x.Id > 0)
                .GroupBy(x => x.Id)
                .Select(g => g.First())
                .OrderBy(x => x.FullName)
                .ThenBy(x => x.UserName)
                .ToList();

            baseList.Data = mergedManagers;
            baseList.Total = mergedManagers.Count;
            return baseList;
        }
    }
}
