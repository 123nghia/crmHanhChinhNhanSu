namespace VS.Human.Rep.Model
{
    public class ScheduleInterview : BaseModel
    {
        public int RelId { get; set; }
        public string RelCode { get; set; }
        public int Type { get; set; }
        public DateTime? ScheduleDate  { get; set; }
        public string? AddressInfo { get; set; }
        public string Noted { get; set; }
       
        public ScheduleInterview()
        {
        
        }
    }
}
