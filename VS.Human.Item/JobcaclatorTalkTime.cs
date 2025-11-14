using System.Collections;
using System.Globalization;

namespace VS.Human.Item
{

    public class BaseSearchRequest
    {
        public string? Token { get; set; }
        public int Page { get; set; }
        public int Limit { get; set; }
        public string? OrderBy { get; set; }
        public string? Status { get; set; }
        public string? UserId { get; set; }
        private DateTime? FromTimeAss { get; set; }
        public DateTime? From
        {

            get
            {
                return FromTimeAss;
            }
            set
            {
                if (value.HasValue)
                {
                    FromTimeAss = new DateTime(value.Value.Year, value.Value.Month, value.Value.Day, 0, 0, 0);

                }
                else
                {
                    FromTimeAss = null;
                }

            }
        }
        private DateTime? ToTimeAss { get; set; }
        public DateTime? To
        {
            get
            {
                return ToTimeAss;
            }
            set
            {
                if (value.HasValue)
                {
                    ToTimeAss = new DateTime(value.Value.Year, value.Value.Month, value.Value.Day, 23, 59, 59);

                }
                else
                {
                    ToTimeAss = null;
                }

            }
        }



        public string? FromText
        {
            get
            {
                if (From.HasValue)
                {
                    return From.Value.ToString("yyyy-MM-dd",  CultureInfo.InvariantCulture);
                }
                return "";
            }
        }
        public string? ToText
        {
            get
            {
                if (To.HasValue)
                {
                    return To.Value.ToString("yyyy-MM-dd",  CultureInfo.InvariantCulture);
                }
                return "";
            }
        }
        public BaseSearchRequest()
        {
            Page = 1;

            Limit = 10;
            FromTimeAss = DateTime.Now.AddMonths(-1);
            ToTimeAss = DateTime.Now;

        }
    }
    public class GetAllRecordGroupByLineCodeRequest : BaseSearchRequest
    {
        public string? LineCode { get; set; }
        public DateTime? TimeSelect { get; set; }
        public int? GroupId { get; set; }

        public int? MemberId { get; set; }
        public int? VendorId { get; set; }

        public GetAllRecordGroupByLineCodeRequest()
        {

        }
    }

    public class GetAllRecordGroupByLineCodeExportRequest : BaseSearchRequest
    {
        public string? LineCode { get; set; }
        public DateTime? TimeSelect { get; set; }

        public int? VendorId { get; set; }

        public GetAllRecordGroupByLineCodeExportRequest()
        {

        }
    }

    public class GetOverViewDashboardReponse : BaseSearchRepons

    {

        public GetOverViewDashboardReponse()
        {
            Total = 0;
        }
    }


    public class BaseSearchRepons
    {

        public int Total { get; set; }
        public IEnumerable? Data { get; set; }
    }


    public class GetAllRecordGroupByLineCodeReponse : BaseSearchRepons

    {
        public GetAllRecordGroupByLineCodeReponse()
        {
            Total = 0;
        }
    }

    public class GetAllTrackingGroupByLineCodeReponse : BaseSearchRepons

    {

        public GetAllTrackingGroupByLineCodeReponse()
        {
            Total = 0;
        }
    }

    public class GetAllRecordGroupByLineCodeExportReponse : BaseSearchRepons

    {
        public GetAllRecordGroupByLineCodeExportReponse()
        {
            Total = 0;
        }
    }


    public class GetOverViewDashboard : BaseSearchRequest
    {

        public int? VendorId { get; set; }
        public string? LineCode { get; set; }

        public string? UserName { get; set; }

        public int? GroupId { get; set; }

        public int? MemberId { get; set; }

    }

}

