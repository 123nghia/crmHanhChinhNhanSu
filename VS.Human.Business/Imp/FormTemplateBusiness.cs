using Microsoft.AspNetCore.Http;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class FormTemplateBusiness : BaseBusiness, IFormTemplateBusiness
    {
        public FormTemplateBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor contextAccessor)
            : base(unitOfWork, contextAccessor)
        {
        }

        public Task<BaseList> GetAll(FormTemplateRequest request)
        {
            return _unitOfWork.FormTemplateRep.GetAll(request);
        }

        public Task<FormTemplateItem> GetById(int id)
        {
            return _unitOfWork.FormTemplateRep.GetById(id);
        }

        public Task<bool> Save(FormTemplateItem item)
        {
            return _unitOfWork.FormTemplateRep.Save(item);
        }

        public Task<bool> Delete(int id)
        {
            return _unitOfWork.FormTemplateRep.Delete(id);
        }
    }
}
