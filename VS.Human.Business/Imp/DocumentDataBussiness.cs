using Microsoft.AspNetCore.Http;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class DocumentDataBussiness : BaseBusiness, IDocumentDataBussiness
    {
        public DocumentDataBussiness(IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {

        }


        public async Task<bool> AddOrUpdate(DocumentDataAddRequest item)
        {
            var relId = item.RelId;
            var dataDocument = item.Data;
            foreach (var item1 in dataDocument)
            {
                var itemDocumentAdd = new DocumentData()
                {
                    Id = item1.Id,
                    Code = item1.Code,
                    DisplayText = item1.DisplayText,
                    ValueFile = item1.ValueFile,
                    RelId = relId
                };
                await _unitOfWork.DocumentDataRep.AddOrUpdate(itemDocumentAdd);
            }
            return true;
        }
        public async Task<bool> Delete(int id)
        {
            return await _unitOfWork.DocumentDataRep.Delete(id);
        }

        public async Task<BaseList> GetAll(DocumentDataRquest request)
        {
            return await _unitOfWork.DocumentDataRep.GetAll(request);
        }

        public async Task<BaseList> GetallRegional()
        {
            return await _unitOfWork.LocationRep.GetAll();
        }
        public async Task<DocumentData> GetById(int id)
        {
            return await _unitOfWork.DocumentDataRep.GetById(id);
        }


    }
}
