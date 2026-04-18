using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;
using VS.Human.Rep;
using VS.Human.Rep.Model;

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
            var result = await MakeCallDetailed(phone, type, IdRel, chanel, userId);
            return result.Success;
        }

        public async Task<CallActionResult> MakeCallDetailed(string phone, string type, int? IdRel, string chanel, int userId, string? sourcePage = null)
        {
            var normalizedPhone = (phone ?? string.Empty).Trim();
            var lineCode = (chanel ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedPhone) || string.IsNullOrWhiteSpace(lineCode))
            {
                return new CallActionResult
                {
                    Success = false,
                    Error = "Missing phone number or SIP line."
                };
            }

            var entityType = NormalizeEntityType(type);
            var entityId = IdRel.GetValueOrDefault();
            var employee = userId > 0 ? await _unitOfWork.EmployeeRep.GetById(userId) : null;
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

            var requestJson = JsonConvert.SerializeObject(payload);
            var callLogId = 0;
            if (entityId > 0)
            {
                callLogId = await _unitOfWork.CallLogRep.InsertAsync(new CallLog
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    EmployeeId = userId,
                    EmployeeName = employee?.FullName ?? employee?.UserName,
                    Extension = lineCode,
                    LineCode = lineCode,
                    PhoneNumber = normalizedPhone,
                    Direction = CallDirections.Outbound,
                    StartTime = DateTime.Now,
                    Status = CallStatuses.Initiated,
                    SourcePage = sourcePage ?? entityType,
                    ProviderRequestJson = requestJson,
                    CreatedBy = userId,
                    UpdatedBy = userId
                });

                await _unitOfWork.WorkflowTimelineRep.AddAsync(new WorkflowTimelineEvent
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    EventCode = WorkflowEventCodes.CallStarted,
                    EventTitle = $"Call started to {normalizedPhone}",
                    TriggeredBy = userId,
                    TriggeredByName = employee?.FullName ?? employee?.UserName,
                    OccurredAt = DateTime.Now,
                    MetadataJson = requestJson,
                    CreatedBy = userId,
                    UpdatedBy = userId
                });
            }

            using var client = new HttpClient
            {
                BaseAddress = new Uri(ResolveCallServiceBaseUrl())
            };
            client.DefaultRequestHeaders.Add("x-user-id", userId.ToString());
            client.DefaultRequestHeaders.Add("x-user-role", "admin");
            AddInternalApiKey(client);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var data = new StringContent(requestJson, Encoding.UTF8, "application/json");

            try
            {
                using var response = await client.PostAsync("api/calls/auto-dial", data);
                var body = await response.Content.ReadAsStringAsync();
                var providerCallId = ExtractProviderCallId(body);
                var status = response.IsSuccessStatusCode ? CallStatuses.Ringing : CallStatuses.Failed;

                if (callLogId > 0)
                {
                    await _unitOfWork.CallLogRep.UpdateProviderResultAsync(
                        callLogId,
                        status,
                        providerCallId,
                        body,
                        response.IsSuccessStatusCode ? null : body);
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "FreePBX auto-dial failed. StatusCode={StatusCode}, Body={Body}, FromExtension={FromExtension}, ToNumber={ToNumber}",
                        (int)response.StatusCode,
                        body,
                        lineCode,
                        normalizedPhone);

                    if (entityId > 0)
                    {
                        await _unitOfWork.WorkflowTimelineRep.AddAsync(new WorkflowTimelineEvent
                        {
                            EntityType = entityType,
                            EntityId = entityId,
                            EventCode = WorkflowEventCodes.CallFailed,
                            EventTitle = $"Call failed to {normalizedPhone}",
                            TriggeredBy = userId,
                            TriggeredByName = employee?.FullName ?? employee?.UserName,
                            OccurredAt = DateTime.Now,
                            ErrorMessage = body,
                            MetadataJson = body,
                            CreatedBy = userId,
                            UpdatedBy = userId
                        });
                    }
                }

                return new CallActionResult
                {
                    Success = response.IsSuccessStatusCode,
                    CallLogId = callLogId,
                    ProviderCallId = providerCallId,
                    Message = response.IsSuccessStatusCode ? "Call requested." : null,
                    Error = response.IsSuccessStatusCode ? null : body
                };
            }
            catch (Exception ex)
            {
                if (callLogId > 0)
                {
                    await _unitOfWork.CallLogRep.UpdateProviderResultAsync(
                        callLogId,
                        CallStatuses.Failed,
                        null,
                        null,
                        ex.Message);
                }

                if (entityId > 0)
                {
                    await _unitOfWork.WorkflowTimelineRep.AddAsync(new WorkflowTimelineEvent
                    {
                        EntityType = entityType,
                        EntityId = entityId,
                        EventCode = WorkflowEventCodes.CallFailed,
                        EventTitle = $"Call failed to {normalizedPhone}",
                        TriggeredBy = userId,
                        TriggeredByName = employee?.FullName ?? employee?.UserName,
                        OccurredAt = DateTime.Now,
                        ErrorMessage = ex.Message,
                        CreatedBy = userId,
                        UpdatedBy = userId
                    });
                }

                _logger.LogError(ex, "FreePBX auto-dial request failed. FromExtension={FromExtension}, ToNumber={ToNumber}", lineCode, normalizedPhone);
                return new CallActionResult
                {
                    Success = false,
                    CallLogId = callLogId,
                    Error = ex.Message
                };
            }
        }

        public async Task<List<CallLog>> GetCallHistory(string entityType, int entityId, int limit = 50)
        {
            return await _unitOfWork.CallLogRep.GetByEntityAsync(NormalizeEntityType(entityType), entityId, limit);
        }

        public async Task<bool> UpdateCallOutcome(CallLogOutcomeUpdate request, int userId)
        {
            return await _unitOfWork.CallLogRep.UpdateOutcomeAsync(request, userId);
        }

        public async Task<TelephonyDashboardSnapshot> GetTelephonyDashboardSnapshot()
        {
            var snapshot = await _unitOfWork.CallLogRep.GetDashboardSnapshotAsync();
            await EnrichTelephonySnapshotFromPbx(snapshot);
            return snapshot;
        }

        private string ResolveCallServiceBaseUrl()
        {
            var configuredUrl = _configuration["Telephony:FreePbxServiceBaseUrl"]?.Trim()
                ?? _configuration["Telephony:CallServiceBaseUrl"]?.Trim();
            if (string.IsNullOrWhiteSpace(configuredUrl))
            {
                throw new InvalidOperationException("Telephony:FreePbxServiceBaseUrl is not configured.");
            }

            if (!configuredUrl.EndsWith("/", StringComparison.Ordinal))
            {
                configuredUrl += "/";
            }

            return configuredUrl;
        }

        private void AddInternalApiKey(HttpClient client)
        {
            var apiKey = _configuration["Telephony:ApiKey"]?.Trim();
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            }
        }

        private static string NormalizeEntityType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return WorkflowEntityTypes.Call;
            }

            var normalized = type.Trim().ToUpperInvariant();
            return normalized switch
            {
                "CANDIDATE" or "UNGVIEN" => WorkflowEntityTypes.Candidate,
                "EMPLOYEE" or "NHANVIEN" => WorkflowEntityTypes.Employee,
                "ORDER" or "DONHANG" => WorkflowEntityTypes.Order,
                _ => normalized
            };
        }

        private static string? ExtractProviderCallId(string? responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return null;
            }

            try
            {
                var token = JToken.Parse(responseBody);
                return token.SelectToken("data.callId")?.ToString()
                    ?? token.SelectToken("callId")?.ToString()
                    ?? token.SelectToken("data.id")?.ToString()
                    ?? token.SelectToken("id")?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private async Task EnrichTelephonySnapshotFromPbx(TelephonyDashboardSnapshot snapshot)
        {
            var configuredUrl = _configuration["Telephony:FreePbxServiceBaseUrl"]?.Trim()
                ?? _configuration["Telephony:CallServiceBaseUrl"]?.Trim();
            if (snapshot == null || string.IsNullOrWhiteSpace(configuredUrl))
            {
                return;
            }

            try
            {
                using var client = new HttpClient
                {
                    BaseAddress = new Uri(EnsureTrailingSlash(configuredUrl)),
                    Timeout = TimeSpan.FromSeconds(3)
                };
                AddInternalApiKey(client);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var healthResponse = await client.GetAsync("api/health");
                snapshot.LastHealthCheckAt = DateTime.Now;
                snapshot.PbxHealthy = healthResponse.IsSuccessStatusCode;
                snapshot.HealthMessage = healthResponse.IsSuccessStatusCode ? "PBX online" : $"PBX HTTP {(int)healthResponse.StatusCode}";

                if (healthResponse.IsSuccessStatusCode)
                {
                    await TryApplyStatisticsAsync(client, snapshot);
                }
            }
            catch (Exception ex)
            {
                snapshot.LastHealthCheckAt = DateTime.Now;
                snapshot.PbxHealthy = false;
                snapshot.HealthMessage = ex.Message;
            }
        }

        private static async Task TryApplyStatisticsAsync(HttpClient client, TelephonyDashboardSnapshot snapshot)
        {
            try
            {
                var today = await ReadJsonAsync(client, "api/statistics/today");
                if (today != null)
                {
                    snapshot.TotalToday = ReadInt(today, "data.totalCalls", "totalCalls", "data.total", "total", snapshot.TotalToday);
                    snapshot.SuccessToday = ReadInt(today, "data.answeredCalls", "answeredCalls", "data.success", "success", snapshot.SuccessToday);
                    snapshot.FailedToday = ReadInt(today, "data.failedCalls", "failedCalls", "data.failed", "failed", snapshot.FailedToday);
                    snapshot.MissedToday = ReadInt(today, "data.missedCalls", "missedCalls", "data.missed", "missed", snapshot.MissedToday);
                }

                var extensions = await ReadJsonAsync(client, "api/statistics/extensions");
                if (extensions != null)
                {
                    snapshot.ExtensionOnline = ReadInt(extensions, "data.online", "online", "data.onlineCount", "onlineCount", snapshot.ExtensionOnline);
                    snapshot.AgentBusy = ReadInt(extensions, "data.busy", "busy", "data.busyCount", "busyCount", snapshot.AgentBusy);
                }
            }
            catch
            {
            }
        }

        private static async Task<JToken?> ReadJsonAsync(HttpClient client, string path)
        {
            using var response = await client.GetAsync(path);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var body = await response.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(body) ? null : JToken.Parse(body);
        }

        private static int ReadInt(JToken token, string path1, string path2, string path3, string path4, int fallback)
        {
            foreach (var path in new[] { path1, path2, path3, path4 })
            {
                var value = token.SelectToken(path);
                if (value != null && int.TryParse(value.ToString(), out var parsed))
                {
                    return parsed;
                }
            }

            return fallback;
        }

        private static string EnsureTrailingSlash(string url)
        {
            return url.EndsWith("/", StringComparison.Ordinal) ? url : url + "/";
        }
    }
}
