using Microsoft.AspNetCore.Http;
using VS.Human.Business.Model;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{

    public partial class EmployeeExtraBusiness : BaseBusiness, IEmployeeExtraBusiness
    {

        public EmployeeExtraBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
             : base(unitOfWork, httpContextAccessor)
        {

        }

        public async Task<RelationItem> GetInfo(string userName)
        {
            return await _unitOfWork.RelationItemRep.GetInfo(userName);
        }


        public async Task<HDLD> GetHDLD(string userName)
        {
            return await _unitOfWork.HDLDItemRep.GetInfo(userName);
        }


        public async Task<bool> UpdateRelation(RelationItemAdd item)
        {

            var itemRealaton = await _unitOfWork.RelationItemRep.GetInfo(item.UserName);
            if (itemRealaton == null || itemRealaton.Id < 1)
            {
                item.Id = 0;
            }
            else item.Id = itemRealaton.Id;
            return await _unitOfWork.RelationItemRep.AddOrUpdate(item);
        }

        public async Task<bool> UpdateHDLDItem(HDLDItemAdd item)
        {

            var itemRealaton = await _unitOfWork.HDLDItemRep.GetInfo(item.UserId);
            if (itemRealaton == null || itemRealaton.Id < 1)
            {
                item.Id = 0;
            }
            else item.Id = itemRealaton.Id;
            return await _unitOfWork.HDLDItemRep.AddOrUpdate(item);
        }

        public async Task<bool> UpdateTax(TaxItemAdd item)
        {
            var itemTax = await _unitOfWork.TaxtItemRep.GetInfo(item.UserName);
            if (itemTax == null || itemTax.Id < 1)
            {
                item.Id = 0;
            }
            else
                item.Id = itemTax.Id;
            return await _unitOfWork.TaxtItemRep.AddOrUpdate(item);
        }
        public async Task<TaxItem> GetTaxItem(string userId)
        {
            var itemTax = await _unitOfWork.TaxtItemRep.GetInfo(userId);
            if (itemTax == null || itemTax.Id < 1)
            {
                itemTax = new TaxItem();
            }

            return itemTax;
        }

        public async Task<bool> UpdateBHXH(BHXHItemAdd item)
        {
            var itemTax = await _unitOfWork.BHXHItemRep.GetInfo(item.UserName);
            if (itemTax == null || itemTax.Id < 1)
            {
                item.Id = 0;
            }
            else
                item.Id = itemTax.Id;
            return await _unitOfWork.BHXHItemRep.AddOrUpdate(item);
        }
        public async Task<BHXHItem> GetBHXH(string userId)
        {
            var itemTax = await _unitOfWork.BHXHItemRep.GetInfo(userId);
            if (itemTax == null || itemTax.Id < 1)
            {
                itemTax = new BHXHItem();
            }

            return itemTax;
        }

        public async Task<bool> UpdateEmployeeInfother(EmployeeInfoOther requestAdd)
        {
            var employee = await _unitOfWork.EmployeeRep.GetById(requestAdd.EmployeeId);

            if (requestAdd.TypeUpdate == 3)
            {

                employee.BankAccount = requestAdd.BankAccount;
                employee.BankName = requestAdd.BankName;
                return await _unitOfWork.EmployeeRep.AddOrUpdate(employee);
            }
            var bhxhItem = await _unitOfWork.BHXHItemRep.GetInfo(employee.UserName);
            var taxCodeitem = await _unitOfWork.TaxtItemRep.GetInfo(employee.UserName);
            if (bhxhItem == null || bhxhItem.Id < 1)
            {
                bhxhItem = new BHXHItem
                {
                    UserName = employee.UserName,

                    Relid = employee.Id.ToString(),
                };
            }
            bhxhItem.IsConfirmletter = requestAdd.IsThuXacNhan;
            bhxhItem.ChungTuThue = requestAdd.ChungTuThue;

            bhxhItem.NumberCode = requestAdd.TaxCode;
            bhxhItem.Dependent = requestAdd.Dependent;
            bhxhItem.DependentName = requestAdd.DependentName;
            if (taxCodeitem == null || taxCodeitem.Id < 1)
            {
                taxCodeitem = new TaxItem
                {
                    UserName = employee.UserName,
                    CodeId = employee.Id.ToString()
                };
            }
            taxCodeitem.BiaSo = requestAdd.BiaSo;
            taxCodeitem.PageTax = requestAdd.PageTax;
            taxCodeitem.CodeId = requestAdd.CodeBHXH;
            taxCodeitem.RegBHYT = requestAdd.RegBHYT;
            if (requestAdd.TypeUpdate == 2)
            {

                await _unitOfWork.BHXHItemRep.AddOrUpdate(bhxhItem);
            }
            if (requestAdd.TypeUpdate == 1)
            {
                await _unitOfWork.TaxtItemRep.AddOrUpdate(taxCodeitem);
            }
            return true;
        }


    }
}
