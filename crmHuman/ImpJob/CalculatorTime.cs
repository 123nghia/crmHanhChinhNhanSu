using Quartz;
using VS.Human.Business;
namespace crmHuman.ImpJob
{

    public class CalculatorTime : IJob
    {

        private readonly ICalculateTimeBusiness business;

        public CalculatorTime(ICalculateTimeBusiness _business)
        {
            business = _business;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await business.CalculatingTalktime();
        }
    }
}
