using Microsoft.AspNetCore.Http;
using VS.Human.Item;
using VS.Human.Rep;

namespace VS.Human.Business.Imp
{
    public class CalculateTimeBusiness : BaseBusiness, ICalculateTimeBusiness
    {
        private readonly IReportTalkTimeGroupByDay reportTalkTimeGroupByDay;
        private IHandleReportBussiness _handleReportBussiness;
        public CalculateTimeBusiness(IUnitOfWork unitOfWork,
            IReportTalkTimeGroupByDay _reportTalkTimeGroupByDay,
              IHandleReportBussiness handleReportBussiness,
            IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {
            reportTalkTimeGroupByDay = _reportTalkTimeGroupByDay;
            _handleReportBussiness = handleReportBussiness;
        }

        public async Task<bool> CalculatingTalktime()
        {
            var timerun = DateTime.Now;
            timerun = timerun.AddMinutes(-12);
            await _handleReportBussiness.CalTalkingTime(timerun);
            Task.WaitAll();
            var startTime = timerun.AddDays(-10);
            var endTime = DatetimeUtility.EndDateTime(DateTime.Now.AddDays(1));

            while (startTime < endTime)
            {
                await reportTalkTimeGroupByDay
                .ProcessCalReportGroupByDay(new GetAllRecordGroupByLineCodeRequest()
                {
                    TimeSelect = startTime
                });
                Task.WaitAll();
                startTime = startTime.AddDays(1);
            }
            Task.WaitAll();
            return true;
        }
    }
}
