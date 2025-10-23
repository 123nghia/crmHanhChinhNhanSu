using VS.Human.Rep.Model;

namespace crmHuman.DisplayModel
{
    public class CandidateDisplayEdit : Candidate
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
    }
}
