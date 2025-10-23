namespace VS.Human.Rep.Model
{
    public class ProcessingCalTimeIndexModel
    {
        public int Total { get; set; }
        public decimal? Duration { get; set; }

        public decimal? Billsec { get; set; }

        public string? LineCode { get; set; }

        public string? Disposition { get; set; }





    }


    public class GetAllRecordGroupByLineCodeIndexModel : BaseModel
    {
        public int? TotalRecord { get; set; }
        public int? SumCall { get; set; }

        public string LineCode { get; set; }

        public int? SumNoAgree { get; set; }

        public int? SumAn { get; set; }

        public int? SumNoBussy { get; set; }

        public int? SumNOCancel { get; set; }

        public int? SumNOAswer { get; set; }

        public int? SumNOChanel { get; set; }

        public int? SumNOServe { get; set; }

        public decimal? TimeWaiting { get; set; }

        public decimal? TimCall { get; set; }

        public int? YearR { get; set; }

        public int? MonthR { get; set; }

        public int? DayR { get; set; }

        public int Total { get; set; }

        public int SumNoFail { get; set; }

        public decimal? PerPercent { get; set; }

        public decimal? TimeTalking { get; set; }

        public string? UserName { get; set; }
    }

    public class GetOverViewDashBoardIndexModel
    {
        public int SumCall { get; set; }
        public int SumNoAgree { get; set; }
        public decimal Perpercent { get; set; }

        public decimal Timcall { get; set; }
        public decimal TimeWaiting { get; set; }
        public decimal TimeTalking { get; set; }

        public GetOverViewDashBoardIndexModel()
        {
            SumCall = 0;
            SumNoAgree = 0;
            Perpercent = 0;
            Timcall = 0;
            TimeWaiting = 0;
            TimeTalking = 0;
        }

    }
}
