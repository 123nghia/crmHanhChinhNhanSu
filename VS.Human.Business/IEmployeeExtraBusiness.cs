using VS.Human.Business.Model;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface IEmployeeExtraBusiness
    {

        Task<bool> UpdateRelation(RelationItemAdd item);

        Task<bool> UpdateTax(TaxItemAdd item);
        Task<TaxItem> GetTaxItem(string userId);

        Task<bool> UpdateHDLDItem(HDLDItemAdd item);
        Task<RelationItem> GetInfo(string userName);

        Task<HDLD> GetHDLD(string userId);


        Task<bool> UpdateBHXH(BHXHItemAdd item);
        Task<BHXHItem> GetBHXH(string userId);

        Task<bool> UpdateEmployeeInfother(EmployeeInfoOther requestAdd);

    }
}
