namespace VS.Human.Item
{
    public class EmployeeRequest : BaseRequest
    {

        public int? GroupId { get; set; }
        public int? MemberId { get; set; }

        public int? Status { get; set; }
        public string? DocumentStatus { get; set; }







        public EmployeeRequest() : base()
        {

        }


    }

    public class JobRequest : BaseRequest
    {

        public int? GroupId { get; set; }


        public int? MemberId { get; set; }

        public int? Status { get; set; }




        public JobRequest() : base()
        {

        }


    }

    public class JobRequest2 : BaseRequest
    {

        public int? PartnerId { get; set; }
        public int? ProjectId { get; set; }

        public int? LinhvucId { get; set; }




        public JobRequest2() : base()
        {
            PartnerId = -1;
            ProjectId = -1;
            LinhvucId = -1;
        }


    }

    public class OrderRequest : BaseRequest
    {

        public bool DisplayAll { get; set; }
        public string? OrderCode { get; set; }
        public int OrderId { get; set; }
        public int? Marketting { get; set; }
        public bool CTV { get; set; }
        public int? GroupId { get; set; }
        public int? MemberId { get; set; }

        public string? RoleCode { get; set; }

        public bool? Tracking { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public int? Job { get; set; }

        public int? Isapply { get; set; }
        public int? IsReturn { get; set; }
        public int? IsClose { get; set; }

        public int? IsPush { get; set; }

        public OrderRequest() : base()
        {
            Marketting = 1;
            Isapply = 0;
            Limit = 100;
            IsClose = 0;
            IsReturn = 0;
            Tracking = false;
            CTV = false;

            var timeCurrent = DateTime.Now;

            var beginOfMoth = new DateTime(timeCurrent.Year, timeCurrent.Month,
                1, 0, 0, 0);

            var timeNextMonth = beginOfMoth.AddMonths(1);

            var timeBeginNextMonth = new DateTime(timeNextMonth.Year,
                timeNextMonth.Month,
                1, 0, 0, 0);
            var lastDayOfMonth = timeBeginNextMonth.AddSeconds(-1);
            //var monday = DateTime.Now.AddDays(DayOfWeek.Monday - DateTime.Now.DayOfWeek);
            //var sunday = monday.AddDays(6);

            //sunday = new DateTime(sunday.Year, sunday.Month, sunday.Day, 23, 59, 59);
            this.From = beginOfMoth;
            this.To = lastDayOfMonth;
            //this.To = DateTime.Now;
            IsPush = 0;

        }
    }


    public class OrderReportRequest : BaseRequest
    {
        public string? OrderCode { get; set; }
        public int OrderId { get; set; }
        public int? Marketting { get; set; }
        public int? GroupId { get; set; }
        public int? MemberId { get; set; }

        public string? RoleCode { get; set; }

        public bool? Tracking { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public int? Job { get; set; }

        public int? Isapply { get; set; }
        public int? IsReturn { get; set; }
        public int? IsClose { get; set; }


        public OrderReportRequest() : base()
        {
            Marketting = 1;
            Isapply = 0;
            Limit = 100;
            IsClose = 0;
            IsReturn = 0;
            Tracking = false;

        }
    }
    public class CandidateRequest : BaseRequest
    {
        public string Status { get; set; }
        public string RoleCode { get; set; }
        public int LoadAll { get; set; }
        public int? GroupId { get; set; }
        public int? MemberId { get; set; }
        public int CandidateStatus { get; set; }


        public int DocumentStatus { get; set; }
        public int ManagerId { get; set; }
        public bool IsEmployee { get; set; }
        public CandidateRequest() : base()
        {
            LoadAll = 0;
            Limit = 100;
        }
    }

    public class CandidateEditRequest
    {
        public int? Id { get; set; }
    }


    public class OnboardMemberRequest : BaseRequest
    {
        public string Status { get; set; }
        public string RoleCode { get; set; }

        public int? GroupId { get; set; }
        public int? MemberId { get; set; }
        public OnboardMemberRequest() : base()
        {

            Limit = 100;
        }


    }

    public class CheckDuplicateRequest : BaseRequest
    {

        public string KeyCheck { get; set; }
        public CheckDuplicateRequest() : base()
        {

        }


    }





}
