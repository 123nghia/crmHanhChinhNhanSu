namespace VS.Human.Item
{
    public class ReportCDInputRequest : BaseRequest
    {

        public int? GroupId { get; set; }
        public int? MemberId { get; set; }

        public int? Status { get; set; }



        public ReportCDInputRequest() : base()
        {

        }


    }


    public class GetAllRecordGroupByLineCodeInputRequest : BaseRequest
    {

    }
}
