using Quartz;
using VS.Human.Business;
using Microsoft.Extensions.DependencyInjection;

namespace crmHuman.ImpJob
{
    public class ContractExpiryJob : IJob
    {
        public static IServiceProvider? ServiceProvider { get; set; }

        public async Task Execute(IJobExecutionContext context)
        {
            var provider = ServiceProvider;
            if (provider == null)
            {
                return;
            }

            using var scope = provider.CreateScope();
            var contractBusiness = scope.ServiceProvider.GetService<IContractBusiness>();
            var notificationBusiness = scope.ServiceProvider.GetService<INotificationBusiness>();
            if (contractBusiness == null || notificationBusiness == null)
            {
                return;
            }

            var days = context.MergedJobDataMap.GetInt("Days");
            if (days <= 0)
            {
                days = 30;
            }

            var targetDate = DateTime.Today.AddDays(days);
            var contracts = await contractBusiness.GetExpiring(days);
            foreach (var contract in contracts)
            {
                if (contract.EmployeeId <= 0 || !contract.EndDate.HasValue)
                {
                    continue;
                }

                if (contract.EndDate.Value.Date != targetDate)
                {
                    continue;
                }

                var message = $"Contract will expire on {contract.EndDate.Value:dd/MM/yyyy}.";
                await notificationBusiness.CreateNotification(contract.EmployeeId, message, "/Contract", "ContractExpiry");
            }
        }
    }
}
