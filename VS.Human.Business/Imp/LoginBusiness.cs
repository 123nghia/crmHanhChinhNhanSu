using Microsoft.AspNetCore.Http;
using VS.Human.Rep;

namespace VS.Human.Business.Imp
{
    public class LoginBusiness : BaseBusiness, ILoginBussiness
    {
        public LoginBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {

        }

        public async Task<string> Print()
        {
            await _unitOfWork.EmployeeRep.Login("nghiai", "nhi");
            return "Nghia";
        }
    }
}
