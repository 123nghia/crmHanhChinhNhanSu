using crmHuman.DisplayModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VS.Human.Business;
using VS.Human.Business.Model;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace crmHuman.Pages.Candidate
{
    [Authorize]
    public class DocumentsModel : BaseModel2
    {
        private readonly ICandidateBusiness _candidateBusiness;
        private readonly IDocumentDataBussiness _documentDataBusiness;
        private readonly ImasterDataBussiness _masterDataBusiness;

        public VS.Human.Rep.Model.Candidate Candidate { get; set; } = new VS.Human.Rep.Model.Candidate();
        public BaseList DocumentList { get; set; } = new BaseList();
        public List<DataMasterItem> DocumentTypes { get; set; } = new List<DataMasterItem>();

        public DocumentsModel(
            ICandidateBusiness candidateBusiness,
            IDocumentDataBussiness documentDataBusiness,
            ImasterDataBussiness masterDataBusiness)
        {
            _candidateBusiness = candidateBusiness;
            _documentDataBusiness = documentDataBusiness;
            _masterDataBusiness = masterDataBusiness;
            TitlePage = "Bổ sung hồ sơ";
            KeyPage = "CandidateDocuments";
        }

        public async Task<IActionResult> OnGet()
        {
            GetInfoUser();
            if (UserData.RoleCode != "CANDIDATE")
            {
                return Redirect("/");
            }

            Candidate = await _candidateBusiness.GetById(UserData.UserId);
            if (Candidate == null || Candidate.Id <= 0)
            {
                return Redirect("/Login");
            }

            // Load documents for this candidate (DataType: 1 for Candidate documents)
            DocumentList = await _documentDataBusiness.GetAll(new DocumentDataRquest
            {
                DataType = 1,
                RelId = Candidate.Id,
                CurrentUserId = UserData.UserId
            });

            // Load master data for document types (TypeData: 7)
            var masterData = await _masterDataBusiness.GetallByTypeData(7);
            foreach (var item in masterData)
            {
                if (item.ApplyFor == 1) continue; // Skip types not for candidates
                DocumentTypes.Add(new DataMasterItem
                {
                    Code = item.Code,
                    Name = item.Name,
                    TypeData = item.TypeData
                });
            }

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateDocuments([FromBody] DocumentDataAddRequest request)
        {
            GetInfoUser();
            if (UserData.RoleCode != "CANDIDATE")
            {
                return new JsonResult(new { success = false, message = "Access denied" });
            }

            request.RelId = UserData.UserId;
            request.DataType = 1; // Candidate documents
            request.UserId = UserData.UserId;

            var result = await _documentDataBusiness.AddOrUpdate(request);
            return new JsonResult(new { success = result });
        }
    }
}
