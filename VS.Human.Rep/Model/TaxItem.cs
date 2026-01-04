namespace VS.Human.Rep.Model
{
    public class TaxItem : BaseModel
    {
            public string UserName { get; set; } = string.Empty;
            public string CodeId { get; set; } = string.Empty;
            public string Number { get; set; } = string.Empty;
            public string ChungTuThue { get; set; } = string.Empty;
            public int PageTax { get; set; } 
            public int BiaSo { get; set; }
            public DateTime? PITDate { get; set; }          // Ngày cấp mã số thuế
            public DateTime? EffectedFrom { get; set; }     // Hiệu lực từ
            public string DependentName { get; set; } = string.Empty;
            public string Dependent { get; set; } = string.Empty;
            public bool? IsConfirmletter { get; set; }

            public string DependentCode{get;set;}

        
    }
}
