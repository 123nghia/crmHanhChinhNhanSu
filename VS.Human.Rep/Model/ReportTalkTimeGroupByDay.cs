using VS.Human.Item;

namespace VS.Human.Rep.Model
{
    public class ReportTalkTimeGroupByDay : BaseModel
    {
        public string? LineCode { get; set; }
        public int? SumCall { get; set; }
        public int? SumNoAgree { get; set; }
        public DateTime? BusinessTime { get; set; }
        public double? PerPercent { get; set; }
        public int? SumAn { get; set; }
        public int? VendorId { get; set; }
        public int? SumNoBussy { get; set; }
        public int? SumNOCancel { get; set; }
        public int? SumNOAswer { get; set; }
        public int? SumNOChanel { get; set; }
        public int? SumNOServe { get; set; }
        public decimal? TimeWaiting { get; set; }
        public decimal? Timcall { get; set; }
        public decimal? TimeTalking { get; set; }
        public int? YearR { get; set; }
        public int? MonthR { get; set; }
        public int? DayR { get; set; }
        public int? SumNoFail { get; set; }
        public ReportTalkTimeGroupByDay()
        {
        }

    }


    public class Account
    {
        public string? UserName { get; set; }
        //public string? Code { get; set; }
        public string? RoleId { get; set; }
        //public string? Token { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public List<string>? Permissions { get; set; }
        public string? Phone { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public int OrgId { get; set; }

        public int? VendorId { get; set; }

        public string? LineCode { get; set; }

        public int? LineId { get; set; }

        public string? Pass { get; set; }



    }


    public class ReportTalkTimeRequest : BaseSearchRequest
    {

    }

    public class ReportTalkTimeReponse : BaseSearchRepons

    {
        public ReportTalkTimeReponse()
        {
            Total = 0;
        }
    }


    public class ReportTalkTimeIndexModel : BaseIndexModel
    {
        public string? LineCode { get; set; }
        public string? NoAgree { get; set; }
        public string? Linkedid { get; set; }

        public string? PhoneLog { get; set; }

        public string? FileRecording { get; set; }

        public int? Duration { get; set; }

        public int? CompanyId { get; set; }

        public int? CampangnId { get; set; }

        public DateTime? CallDate { get; set; }
        public DateTime? EventTime { get; set; }
        public string? Lastapp { get; set; }
        public string? LastData { get; set; }

        public string? Disposition { get; set; }

        public ReportTalkTimeIndexModel()
        {

        }


    }
    public class ImpactHistoryIndexModel : BaseIndexModel
    {
        public string? ShortDescription { get; set; }
        public string? StatusIm { get; set; }

        public string? NoteIm { get; set; }

        public DateTime? Promiseday { get; set; }

        public string? MoneyPromise { get; set; }

        public DateTime? DaysuggestTime { get; set; }

        public int? StatusFollow { get; set; }

        public int? Relationship { get; set; }

        public int? ProfileId { get; set; }

        public int? Priority { get; set; }

        public string? StatusName { get; set; }

        public string? ColorCode { get; set; }
        public DateTime? CreateAt { get; set; }

        public string? StatusCode { get; set; }


    }

    public class ReportQuerryNewTaltimeIndex : BaseIndexModel
    {
        public string? LineCode { get; set; }
        public string? NoAgree { get; set; }

        public string? PhoneLog { get; set; }

        public string? FileRecording { get; set; }

        public int? Duration { get; set; }

        public int? CompanyId { get; set; }

        public int? CampangnId { get; set; }

        public DateTime? CallDate { get; set; }

        public DateTime? EventTime { get; set; }

        public string Linkedid { get; set; }

        public string Disposition { get; set; }

        public int? SourceCall { get; set; }

        public int DurationBill { get; set; }
        public double DurationReal { get; set; }
        public string? Lastapp { get; set; }
        public string? Lastdata { get; set; }
    }
    public class ReportQuerryRecordingFileIndex : BaseIndexModel
    {
        public string File { get; set; }
        public int? Duration { get; set; }

        public int updated { get; set; }


    }

    public class HandlelFileRecordingRequest : BaseSearchRequest
    {
        public DateTime? TimeSelect { get; set; }

        public DateTime? TimeFrom { get; set; }
        public DateTime? TimeTo { get; set; }

        public DateTime? Linked { get; set; }
    }

}
