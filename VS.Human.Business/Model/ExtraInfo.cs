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
        public bool? IsThuXacNhan { get;  set; }
        public string ChungTuThue { get;  set; }
        public string TaxCode { get;  set; }
        public string Dependent { get;  set; }
        public string DependentName { get;  set; }
        public int BiaSo { get;  set; }
        public int PageTax { get;  set; }
        public string CodeBHXH { get;  set; }
        public string RegBHYT { get;  set; }
        public DateTime? BHXHStartMonth { get; set; }
        public DateTime? PITDate { get; set; }
        public DateTime? EffectedFrom { get; set; }
    }


}
