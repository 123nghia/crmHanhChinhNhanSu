using Microsoft.AspNetCore.Http;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class MailGroupBusiness : BaseBusiness, IMailGroupBusiness
    {
        public MailGroupBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor contextAccessor)
            : base(unitOfWork, contextAccessor)
        {
        }

        public Task<BaseList> GetAll(MailGroupRequest request)
        {
            return _unitOfWork.MailGroupRep.GetAll(request);
        }

        public Task<List<MailGroupItem>> GetSelectableGroupsAsync()
        {
            return _unitOfWork.MailGroupRep.GetSelectableGroupsAsync();
        }

        public Task<MailGroupItem> GetById(int id)
        {
            return _unitOfWork.MailGroupRep.GetById(id);
        }

        public Task<bool> Save(MailGroupItem item)
        {
            return _unitOfWork.MailGroupRep.Save(item);
        }

        public Task<bool> Delete(int id)
        {
            return _unitOfWork.MailGroupRep.Delete(id);
        }

        public Task<List<string>> ResolveRecipientsAsync(IEnumerable<int> groupIds)
        {
            return _unitOfWork.MailGroupRep.ResolveRecipientsAsync(groupIds);
        }
    }
}
