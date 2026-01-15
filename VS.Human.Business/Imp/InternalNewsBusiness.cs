using Microsoft.AspNetCore.Http;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class InternalNewsBusiness : BaseBusiness, IInternalNewsBusiness
    {
        public InternalNewsBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {
        }

        public Task<BaseList> GetAll(InternalNewsRequest request)
        {
            return _unitOfWork.InternalNewsRep.GetAll(request);
        }

        public Task<InternalNewsItem> GetById(int id)
        {
            return _unitOfWork.InternalNewsRep.GetById(id);
        }

        public Task<bool> AddOrUpdate(InternalNewsItem item)
        {
            return _unitOfWork.InternalNewsRep.AddOrUpdate(item);
        }

        public Task<bool> Delete(int id)
        {
            return _unitOfWork.InternalNewsRep.Delete(id);
        }
    }
}
