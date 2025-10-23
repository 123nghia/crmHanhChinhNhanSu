using Microsoft.AspNetCore.Http;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class ParrentChildBussiness : BaseBusiness, IParrentChildBussiness
    {
        public ParrentChildBussiness(IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {

        }
        public Task<bool> AddOrUpdate(ParrentChild parrentChild)
        {
            return _unitOfWork.ParrentChildRep.AddOrUpdate(parrentChild);
        }
        public Task<BaseList> GetAll(int? Rel, int? Type)
        {
            return _unitOfWork.ParrentChildRep.GetAll(Rel, Type);
        }
    }
}
