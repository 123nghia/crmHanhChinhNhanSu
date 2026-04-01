using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Quartz;
using Quartz.Impl;
using System.Globalization;
using crmHuman.Helpers;
using crmHuman.Model;
using VS.Human.Business;
using crmHuman.Services;
using crmHuman.ImpJob;

namespace crmHuman
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Force dd/MM/yyyy formatting globally via the Vietnamese culture.
            var vietnamCulture = new CultureInfo("vi-VN");
            vietnamCulture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
            vietnamCulture.DateTimeFormat.LongDatePattern = "dd/MM/yyyy HH:mm:ss";
            vietnamCulture.DateTimeFormat.FullDateTimePattern = "dd/MM/yyyy HH:mm:ss";
            CultureInfo.DefaultThreadCurrentCulture = vietnamCulture;
            CultureInfo.DefaultThreadCurrentUICulture = vietnamCulture;
            var localizationOptions = new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(vietnamCulture),
                SupportedCultures = new[] { vietnamCulture },
                SupportedUICultures = new[] { vietnamCulture },
                ApplyCurrentCultureToResponseHeaders = true
            };
            builder.Services.AddRazorPages();
            builder.Services.Config();
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromDays(1);
                options.SlidingExpiration = true;
                options.AccessDeniedPath = "/Home/Forbidden";
                options.LoginPath = "/Login";
                options.Events = new CookieAuthenticationEvents
                {
                    OnValidatePrincipal = async context =>
                    {
                        var roleCode = context.Principal?.FindFirst("RoleCode")?.Value;
                        if (string.IsNullOrWhiteSpace(roleCode) || string.Equals(roleCode, "CANDIDATE", StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }

                        var userIdText = context.Principal?.FindFirst("userId")?.Value;
                        if (!int.TryParse(userIdText, out var userId) || userId <= 0)
                        {
                            context.RejectPrincipal();
                            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            return;
                        }

                        var empBusiness = context.HttpContext.RequestServices.GetRequiredService<IEmpBusiness>();
                        var employee = await empBusiness.GetById(userId);
                        if (EmployeeSystemAccessPolicy.HasSystemAccess(employee))
                        {
                            return;
                        }

                        var userName = context.Principal?.FindFirst("UserName")?.Value;
                        var fullName = context.Principal?.FindFirst("FullName")?.Value;
                        UserActive.DataActiveOnline.MarkLogout(userId.ToString(), userName, fullName);

                        var logHistoryBusiness = context.HttpContext.RequestServices.GetService<ILogHistoryBusiness>();
                        if (logHistoryBusiness != null)
                        {
                            await logHistoryBusiness.LogLogout(userId, roleCode);
                        }

                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                };
            });
            builder.Services.AddHttpContextAccessor();
            
            // Đăng ký DatabaseMigrationService
            builder.Services.AddSingleton<DatabaseMigrationService>();
            builder.Services.AddHostedService<AttendanceRealtimeSyncService>();
            builder.Services.AddHostedService<StartupMailTestService>();
            
            //builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
            var app = builder.Build();
            
            // Chạy database migration khi startup
            try
            {
                var migrationService = app.Services.GetRequiredService<DatabaseMigrationService>();
                await migrationService.RunMigrationsAsync();
            }
            catch (Exception ex)
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Lỗi khi chạy database migration. Ứng dụng vẫn sẽ tiếp tục khởi động.");
                if (app.Environment.IsDevelopment())
                {
                    throw;
                }
            }
            
            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
                app.UseHttpsRedirection();
            }
            app.UseRequestLocalization(localizationOptions);
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseMiddleware<AuditLogMiddleware>();
            app.MapRazorPages();

            // Khoi chay Quartz scheduler
            try 
            {
                var scheduler = await StdSchedulerFactory.GetDefaultScheduler();
                await scheduler.Start();

                ContractExpiryJob.ServiceProvider = app.Services;
                var jobKey = new JobKey("ContractExpiryJob");
                if (!await scheduler.CheckExists(jobKey))
                {
                    var job = JobBuilder.Create<ContractExpiryJob>()
                        .WithIdentity(jobKey)
                        .UsingJobData("Days", 30)
                        .Build();

                    var trigger = TriggerBuilder.Create()
                        .WithIdentity("ContractExpiryTrigger")
                        .WithSchedule(CronScheduleBuilder.DailyAtHourAndMinute(8, 0))
                        .Build();

                    await scheduler.ScheduleJob(job, trigger);
                }
            }
            catch (Exception ex)
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Loi khi khoi chay Quartz scheduler.");
            }

            app.Run();
        }
    }
}
