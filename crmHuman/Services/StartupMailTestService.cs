using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VS.Human.Business;

namespace crmHuman.Services
{
    public class StartupMailTestService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StartupMailTestService> _logger;

        public StartupMailTestService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<StartupMailTestService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var enabled = _configuration.GetValue<bool>("MailTest:Enabled");
            if (!enabled)
            {
                return;
            }

            var toEmail = _configuration["MailTest:ToEmail"];
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("MailTest enabled but ToEmail is empty.");
                return;
            }

            var templateCode = _configuration["MailTest:TemplateCode"];
            if (string.IsNullOrWhiteSpace(templateCode))
            {
                templateCode = "LEAVE_CREATE";
            }

            var delaySeconds = _configuration.GetValue<int?>("MailTest:DelaySeconds") ?? 5;
            if (delaySeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }

            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var now = DateTime.Now;
            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["EmployeeName"] = "Auto Test",
                ["EmployeeEmail"] = toEmail.Trim(),
                ["ManagerName"] = "Auto Test Manager",
                ["LeaveTypeName"] = "Nghỉ phép",
                ["LeaveTypeCode"] = "TEST",
                ["FromDate"] = now.ToString("dd/MM/yyyy"),
                ["ToDate"] = now.ToString("dd/MM/yyyy"),
                ["NumDays"] = "1",
                ["Reason"] = "Auto test email",
                ["StatusText"] = "Test",
                ["ApproverName"] = "Auto Test",
                ["ApproverRole"] = "System",
                ["Comment"] = "Auto test email",
                ["Action"] = "Test",
                ["CreateAt"] = now.ToString("dd/MM/yyyy HH:mm"),
                ["HandoverEmployeeName"] = "Auto Test"
            };

            var result = await emailService.SendTemplateWithErrorAsync(templateCode, new[] { toEmail.Trim() }, tokens);
            if (result.Success)
            {
                _logger.LogInformation("MailTest sent to {ToEmail} using template {TemplateCode}.", toEmail, templateCode);
            }
            else
            {
                _logger.LogWarning("MailTest failed: {Error}", result.Error ?? "Unknown error");
            }
        }
    }
}
