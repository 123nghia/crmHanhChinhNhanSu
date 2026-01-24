using Microsoft.AspNetCore.Http;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Model
{
    public class ContractAdd : Contract
    {
        public IFormFile? ContractFile { get; set; }
    }
}
