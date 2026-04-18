using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using VS.Human.Rep;

namespace VS.Human.Business.Imp
{
    public class CallBusiness : BaseBusiness, ICallBussiness
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CallBusiness> _logger;

        public CallBusiness(
            IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            ILogger<CallBusiness> logger)
            : base(unitOfWork, httpContextAccessor)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> MakeCall(string phone, string type, int? IdRel, string chanel, int userId)
        {
            var normalizedPhone = (phone ?? string.Empty).Trim();
            var lineCode = (chanel ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedPhone) || string.IsNullOrWhiteSpace(lineCode))
            {
                return false;
            }

            var payload = new
            {
                fromExtension = lineCode,
                toNumber = normalizedPhone,
                metadata = new
                {
                    type,
                    idRel = IdRel,
                    employeeId = userId,
                    source = "crmHuman"
                }
            };

            using var client = new HttpClient
            {
                BaseAddress = new Uri(ResolveCallServiceBaseUrl())
            };
            client.DefaultRequestHeaders.Add("x-user-id", userId.ToString());
            client.DefaultRequestHeaders.Add("x-user-role", "admin");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var data = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            try
            {
                using var response = await client.PostAsync("api/calls/auto-dial", data);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning(
                        "FreePBX auto-dial failed. StatusCode={StatusCode}, Body={Body}, FromExtension={FromExtension}, ToNumber={ToNumber}",
                        (int)response.StatusCode,
                        body,
                        lineCode,
                        normalizedPhone);
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FreePBX auto-dial request failed. FromExtension={FromExtension}, ToNumber={ToNumber}", lineCode, normalizedPhone);
                return false;
            }
        }

        private string ResolveCallServiceBaseUrl()
        {
            var configuredUrl = _configuration["Telephony:FreePbxServiceBaseUrl"]?.Trim()
                ?? _configuration["Telephony:CallServiceBaseUrl"]?.Trim();
            if (string.IsNullOrWhiteSpace(configuredUrl))
            {
                configuredUrl = "http://192.168.1.9:3000/";
            }

            if (!configuredUrl.EndsWith("/", StringComparison.Ordinal))
            {
                configuredUrl += "/";
            }

            return configuredUrl;
        }
    }
}
