namespace VS.Human.Item
{
    public class ImpactDashboardOverviewReponse
    {
        public List<ImpactDashboardOverviewReponseItem>? Data { get; set; }

    }

    public class ImpactDashboardOverviewReponseItem
    {
        public string ReasonCode { get; set; }

        public string? ReasonName { get; set; }
        public int Sum { get; set; }

        public float Percent { get; set; }



    }
    public class ReportImpactRequest : BaseSearchRequest
    {
        public string? UserId { get; set; }

        public int? VendorId { get; set; }
        public int? StatusSearch { get; set; }
        public string? LineCode { get; set; }


        public ReportImpactRequest()
        {
            this.Page = 1;
            this.Limit = 10;
        }
    }

    public class ReportNoAgreeRequest
    {
        public int Total { get; set; }


    }
    public class ReportCDRequest : BaseSearchRequest
    {

        public string LineCode { get; set; }
        public string Disposition { get; set; }

        public string PhoneLog { get; set; }

        public string NoAgree { get; set; }

        public int? TimeTalkBegin { get; set; }
        public int? TimeTalkEnd { get; set; }
        public int? TimeFrom1 { get; set; }
        public int? TimeFrom2 { get; set; }

        public int? VendorId { get; set; }

        public int? GroupId { get; set; }

        public int? MemberId { get; set; }

        public int? ProjectId { get; set; }


        public ReportCDRequest()
        {
            TimeFrom2 = 64800;
            TimeFrom1 = 25200;
            TimeTalkEnd = 600;
            TimeTalkBegin = 0;



        }
    }

    public class ReportCDRReponse : BaseSearchRepons

    {
        public ReportCDRReponse()
        {
            Total = 0;
        }
    }

    public class ReportCallRequest : BaseSearchRequest
    {

        public string? LineCode { get; set; }
        public string? Disposition { get; set; }

        public string? PhoneLog { get; set; }

        public string? NoAgree { get; set; }



        public int? VendorId { get; set; }
        public ReportCallRequest()
        {

        }
    }
    public class ReportImpactReponse : BaseSearchRepons

    {

        public ReportImpactReponse()
        {
            Total = 0;
        }
    }
    public class ReportCDRItem
    {
        public int Orderid { get; set; }
        public int TotalRecord { get; set; }
        public int Id { get; set; }
        public DateTime? Calldate { get; set; }
        public string? Dst { get; set; }
        public string? Src { get; set; }
        public string? Disposition { get; set; }
        public string? Lastapp { get; set; }
        public string? Billsec { get; set; }
        public string? Duration { get; set; }
        public string? Recordingfile { get; set; }

        public string? LineCode { get; set; }
        public int DurationBill { get; set; }
        public double DurationReal { get; set; }
        public string? NoAgree { get; set; }

        public string? UserName { get; set; }

        public bool IsCal { get; set; }
        public bool IsShow
        {
            get
            {
                if (Lastapp == "Dial" && Disposition == "ANSWERED")
                {
                    return true;
                }
                return false;
            }
        }


    }


    public class ReportCallItem
    {
        public int TotalRecord { get; set; }
        public DateTime? CreateAt { get; set; }
        public int Id { get; set; }
        public DateTime? Calldate { get; set; }
        public string? Phone { get; set; }
        public string? Src { get; set; }
        public string? Disposition { get; set; }
        public string? Lastapp { get; set; }
        public string? Billsec { get; set; }
        public string? Duration { get; set; }
        public string? Recordingfile { get; set; }
        public int DurationBill { get; set; }
        public double DurationReal { get; set; }
        public string? NoAgree { get; set; }

        public string? LineCode { get; set; }

        public bool IsCal { get; set; }
        public bool IsShow
        {
            get
            {
                if (Lastapp == "Dial" && Disposition == "ANSWERED")
                {
                    return true;
                }
                return false;
            }
        }


    }
    public class GetOverViewTalkingItem
    {
        public int Duration { get; set; }

        public int Billsec { get; set; }

        public string Disposition { get; set; }
    }
    public class FirstCallLastCallReponse : BaseSearchRepons

    {
        public int TotalRecord { get; set; }
        public FirstCallLastCallReponse()
        {
            Total = 0;
        }
    }
    public class GetOverViewTalkingItemReponse

    {
        public List<GetOverViewTalkingItem> Data { get; set; }
        public GetOverViewTalkingItemReponse()
        {

        }
    }
    public class GetOverViewInfoReponseItem
    {

        public int Total { get; set; }

        public string? Type { get; set; }
    }
    public class GetOverViewInfoReponse

    {

        public List<GetOverViewInfoReponseItem> Data { get; set; }


        public GetOverViewInfoReponse()
        {

        }
    }
    public class FirstCallLastCallRequest : BaseSearchRequest
    {

        public int? VendorId { get; set; }
        public string? LineCode { get; set; }
        public FirstCallLastCallRequest()
        {



        }
    }

    public class ReportNoCDRequest : ReportCDRequest
    {
        public string? NoAgree { get; set; }

    }
    public class ReportCDRItemExport
    {
        public string? Src { get; set; }
        public DateTime? Calldate { get; set; }
        public string? Dst { get; set; }
        private string? Disposition { get; set; }
        private double? DurationReal { get; set; }

        public string DurationRealText
        {

            get
            {
                if (!DurationReal.HasValue)
                {
                    return "00:00:00";
                }
                TimeSpan t = TimeSpan.FromSeconds(DurationReal.Value);
                string s = string.Format("{0}:{1}:{2}", ((int)t.TotalHours), t.Minutes, t.Seconds);
                return s;
            }
        }
        public string? Duration { get; set; }
        private string? Recordingfile { get; set; }
        public string? RecordingfileUrl
        {
            get
            {
                return "http://42.115.94.180:7777/api/file/getaudio10?filePath=" + Recordingfile;
            }
        }
        public string? NoAgree { get; set; }

    }

    public class GetReportOverviewAgrreeRequest
    {

        public string? LineCode { get; set; }

        public string? UserId { get; set; }

    }

    public class ReportImpactItem
    {

        public int TotalRecord { get; set; }

        public string? CustomerName { get; set; }
        public string? NoAgreement { get; set; }
        public string? Phone1 { get; set; }
        public string? OfficeNumber { get; set; }
        public string? OtherPhone { get; set; }
        public string? HouseNumber { get; set; }
        public string? ReasonName { get; set; }
        public string? CampaignName { get; set; }
        public string? UpdateName { get; set; }
        public string? AuthorName { get; set; }
        public DateTime? CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }

    }
    public class ReportImpactItemEx
    {

        public string? NoAgreement { get; set; }


        public string? CustomerName { get; set; }
        public DateTime? Dob { get; set; }
        public string? NationalId { get; set; }
        public DateTime? RegisterDay { get; set; }

        public string? CodeProduct { get; set; }

        public string? NameProduct { get; set; }

        public string? PriceProduct { get; set; }

        public string? TotalFines { get; set; }

        public string? TotalMoneyPaid { get; set; }
        public string? Tenure { get; set; }
        public string? EMI { get; set; }

        public DateTime LastPadDay { get; set; }

        public string? NoTenure { get; set; }

        public string? DebitOriginal { get; set; }


        public string? DPD { get; set; }

        public string? MobilePhone { get; set; }

        public string? OtherPhone { get; set; }
        public string? NoteFirstTime { get; set; }

        public string? Road { get; set; }
        public string? SuburbanDir { get; set; }
        public string? Provice { get; set; }

        public string? Road1 { get; set; }
        public string? SuburbanDir1 { get; set; }
        public string? Provice1 { get; set; }

        public string? Road2 { get; set; }
        public string? SuburbanDir2 { get; set; }
        public string? Provice2 { get; set; }

        public string StatusName { get; set; }

        public string status { get; set; }

        public string EmployeeName { get; set; }

        public string? NoteIm { get; set; }
        public string? LastNote { get; set; }


        public DateTime? Promiseday { get; set; }
        public string MoneyPromise { get; set; }
        public DateTime? DaysuggestTime { get; set; }

        public DateTime? CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }

    }

    public class GetOverViewInfoRequest : BaseSearchRepons

    {

        public string? LineCode { get; set; }
        public GetOverViewInfoRequest()
        {
            Total = 0;
        }
    }




}

