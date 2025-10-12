using VS.Human.Rep.Model;

namespace VS.Human.Business.Model
{


    public class RelationItemAdd : RelationItem
    {

    }

    public class TaxItemAdd : TaxItem
    {

    }

    public class BHXHItemAdd : BHXHItem
    {

    }


    public class HDLDItemAdd : HDLD
    {

    }

    public class EmployeeInfoOther : Employee
    {

        public int TypeUpdate { get; set; }
        public int EmployeeId { get; set; }

        public string BankAccount { get; set; }
        public string BankName { get; set; }
        public bool? IsThuXacNhan { get; internal set; }
        public string ChungTuThue { get; internal set; }
        public string TaxCode { get; internal set; }
        public string Dependent { get; internal set; }
        public string DependentName { get; internal set; }
        public int BiaSo { get; internal set; }
        public int PageTax { get; internal set; }
        public string CodeBHXH { get; internal set; }
        public string RegBHYT { get; internal set; }
    }


}
