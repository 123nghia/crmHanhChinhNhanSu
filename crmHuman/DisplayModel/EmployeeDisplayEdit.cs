using VS.Human.Rep.Model;

namespace crmHuman.DisplayModel
{
    public class EmployeeDisplayEdit : Employee
    {
        public string DayOfBirthDisplay
        {
            get
            {
                if (Dob.HasValue)
                {
                    return Dob.Value.ToString("yyyy-MM-dd");
                }
                return string.Empty;
            }
        }


        public string NationalDateDisplay
        {
            get
            {
                if (NationalDate.HasValue)
                {
                    return NationalDate.Value.ToString("yyyy-MM-dd");
                }
                return string.Empty;
            }
        }
        public string OnboardDateDisplay
        {
            get
            {
                if (Onboard.HasValue)
                {
                    return Onboard.Value.ToString("yyyy-MM-dd");
                }
                return string.Empty;
            }
        }
        public string BankName { get; set; } = string.Empty;
        public string BankAccount { get; set; } = string.Empty;

        public RelationItem? DataRelation { get; set; }
        public HDLD? HDLD { get; set; }
        public TaxItem? TaxItem { get; set; }

        public BHXHItem? BHXHItem { get; set; }

        public List<string> DataCheckList { get; set; }

        public EmployeeDisplayEdit()
        {
            DataCheckList = new List<string>();
        }


    }
}
