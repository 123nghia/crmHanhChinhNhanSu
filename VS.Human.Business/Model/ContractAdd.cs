using Microsoft.AspNetCore.Http;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Model
{
    public class ContractAdd : Contract
    {
        public IFormFile? ContractFile { get; set; }
    }

    public class ContractSignRequest
    {
        public int Id { get; set; }
        public string? PasswordConfirm { get; set; }
        public string? SignatureCode { get; set; }
        public string? SignNote { get; set; }
        public bool AcceptTerms { get; set; }
        public string? SignatureDataUrl { get; set; }
    }
}
