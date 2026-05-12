using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using VS.Human.Business.Common;
using VS.Human.Business.Model;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class SipBusiness : BaseBusiness, ISipBusiness
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SipBusiness> _logger;

        public SipBusiness(
            IUnitOfWork unitOfWork,
            IHttpContextAccessor contextAccessor,
            IConfiguration configuration,
            ILogger<SipBusiness> logger)
            : base(unitOfWork, contextAccessor)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<SipServer>> GetServers()
        {
            return await _unitOfWork.SipRep.GetServers();
        }

        public async Task<SipServer?> GetActiveServer()
        {
            return await _unitOfWork.SipRep.GetActiveServer();
        }

        public async Task<List<SipLineViewModel>> GetLines()
        {
            return await _unitOfWork.SipRep.GetLines();
        }

        public async Task<List<SipEmployeeOption>> GetAssignableEmployees()
        {
            return await _unitOfWork.SipRep.GetAssignableEmployees();
        }

        public async Task<EmployeeSipAccountView?> GetEmployeeSipInfo(int employeeId)
        {
            return await _unitOfWork.SipRep.GetEmployeeSipInfo(employeeId);
        }

        public async Task<Result<SipServiceHealthResult>> CheckServiceHealth()
        {
            var checkedAt = DateTime.Now;
            string baseUrl;

            try
            {
                baseUrl = ResolveFreePbxServiceBaseUrl();
            }
            catch (Exception ex)
            {
                var missingConfig = new SipServiceHealthResult
                {
                    Url = "Telephony:FreePbxServiceBaseUrl",
                    IsOnline = false,
                    StatusText = "Chua cau hinh link",
                    CheckedAt = checkedAt,
                    Error = ex.Message
                };

                return Result<SipServiceHealthResult>.Success(missingConfig, "Chua cau hinh link tong dai.");
            }

            var stopwatch = Stopwatch.StartNew();
            try
            {
                using var client = new HttpClient
                {
                    BaseAddress = new Uri(baseUrl),
                    Timeout = TimeSpan.FromSeconds(4)
                };
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("x-user-id", GetUserId().ToString());
                client.DefaultRequestHeaders.Add("x-user-role", "admin");

                var apiKey = _configuration["Telephony:ApiKey"]?.Trim();
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    client.DefaultRequestHeaders.Add("x-api-key", apiKey);
                }

                using var response = await client.GetAsync(string.Empty);
                stopwatch.Stop();

                var statusCode = (int)response.StatusCode;
                var reason = string.IsNullOrWhiteSpace(response.ReasonPhrase)
                    ? response.StatusCode.ToString()
                    : response.ReasonPhrase;
                var statusText = response.IsSuccessStatusCode
                    ? "Dang hoat dong"
                    : $"Co phan hoi HTTP {statusCode} {reason}";

                var health = new SipServiceHealthResult
                {
                    Url = baseUrl.TrimEnd('/'),
                    IsOnline = true,
                    StatusCode = statusCode,
                    StatusText = statusText,
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    CheckedAt = checkedAt
                };

                return Result<SipServiceHealthResult>.Success(health, statusText);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(ex, "SIP service health check failed. Url={Url}", baseUrl);

                var health = new SipServiceHealthResult
                {
                    Url = baseUrl.TrimEnd('/'),
                    IsOnline = false,
                    StatusText = "Khong ket noi duoc",
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    CheckedAt = checkedAt,
                    Error = ex.Message
                };

                return Result<SipServiceHealthResult>.Success(health, health.StatusText);
            }
        }

        public async Task<Result<SipServer>> SaveServer(SipServerSaveRequest request)
        {
            var host = request.Host?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(host))
            {
                return Result<SipServer>.Failure("Vui long nhap dia chi SIP server.");
            }

            if (request.Port <= 0)
            {
                return Result<SipServer>.Failure("Cong SIP server khong hop le.");
            }

            SipServer server;
            if (request.Id.HasValue && request.Id.Value > 0)
            {
                server = await _unitOfWork.SipRep.GetServerById(request.Id.Value) ?? new SipServer();
                if (server.Id <= 0)
                {
                    return Result<SipServer>.Failure("Khong tim thay SIP server can cap nhat.");
                }
            }
            else
            {
                server = new SipServer
                {
                    CreatedBy = GetUserId(),
                    CreateAt = DateTime.Now
                };
            }

            server.Name = string.IsNullOrWhiteSpace(request.Name) ? "SIP Server mac dinh" : request.Name.Trim();
            server.Host = host;
            server.Port = request.Port;
            server.Domain = request.Domain?.Trim();
            server.Transport = request.Transport?.Trim();
            server.OutboundProxy = request.OutboundProxy?.Trim();
            server.Note = request.Note?.Trim();
            server.IsActive = request.IsActive > 0 ? 1 : 0;
            server.UpdatedBy = GetUserId();
            server.UpdateAt = DateTime.Now;

            var saved = await _unitOfWork.SipRep.SaveServer(server);
            if (saved.Id <= 0)
            {
                return Result<SipServer>.Failure("Khong the luu cau hinh SIP server.");
            }

            return Result<SipServer>.Success(saved, "Da luu cau hinh SIP server.");
        }

        public async Task<Result<SipLine>> SaveLine(SipLineSaveRequest request)
        {
            var lineCode = request.LineCode?.Trim() ?? string.Empty;
            var sipUserName = request.SipUserName?.Trim() ?? string.Empty;
            var sipPassword = request.SipPassword?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(lineCode))
            {
                return Result<SipLine>.Failure("Vui long nhap line goi.");
            }

            if (string.IsNullOrWhiteSpace(sipUserName))
            {
                return Result<SipLine>.Failure("Vui long nhap tai khoan SIP.");
            }

            if (string.IsNullOrWhiteSpace(sipPassword))
            {
                return Result<SipLine>.Failure("Vui long nhap mat khau SIP.");
            }

            var duplicateLine = await _unitOfWork.SipRep.GetLineByCode(lineCode);
            if (duplicateLine != null && duplicateLine.Id > 0 && duplicateLine.Id != (request.Id ?? 0))
            {
                return Result<SipLine>.Failure("Line goi nay da ton tai.");
            }

            SipLine line;
            if (request.Id.HasValue && request.Id.Value > 0)
            {
                line = await _unitOfWork.SipRep.GetLineById(request.Id.Value) ?? new SipLine();
                if (line.Id <= 0)
                {
                    return Result<SipLine>.Failure("Khong tim thay line SIP can cap nhat.");
                }
            }
            else
            {
                line = new SipLine
                {
                    CreatedBy = GetUserId(),
                    CreateAt = DateTime.Now
                };
            }

            var serverId = request.SipServerId;
            if (!serverId.HasValue || serverId.Value <= 0)
            {
                if (line.SipServerId.HasValue && line.SipServerId.Value > 0)
                {
                    serverId = line.SipServerId;
                }
                else
                {
                    serverId = (await _unitOfWork.SipRep.GetActiveServer())?.Id;
                }
            }

            if (!serverId.HasValue || serverId.Value <= 0)
            {
                return Result<SipLine>.Failure("Vui long cau hinh SIP server truoc khi them line.");
            }

            var server = await _unitOfWork.SipRep.GetServerById(serverId.Value);
            if (server == null || server.Id <= 0)
            {
                return Result<SipLine>.Failure("Khong tim thay SIP server duoc chon.");
            }

            if (string.IsNullOrWhiteSpace(server.Host))
            {
                return Result<SipLine>.Failure("Vui long cap nhat Host cho SIP server truoc khi them line.");
            }

            line.SipServerId = serverId;
            line.LineCode = lineCode;
            line.SipUserName = sipUserName;
            line.SipPassword = sipPassword;
            line.AuthUser = request.AuthUser?.Trim();
            line.DisplayName = request.DisplayName?.Trim();
            line.Note = request.Note?.Trim();
            line.IsActive = request.IsActive > 0 ? 1 : 0;
            line.UpdatedBy = GetUserId();
            line.UpdateAt = DateTime.Now;

            var saved = await _unitOfWork.SipRep.SaveLine(line);
            if (saved.Id <= 0)
            {
                return Result<SipLine>.Failure("Khong the luu line SIP.");
            }

            if (saved.EmployeeId.HasValue && saved.EmployeeId.Value > 0)
            {
                await _unitOfWork.SipRep.SyncEmployeeLineCode(saved.EmployeeId.Value, GetUserId());
            }

            await SyncManagedLineToFreePbx(saved);

            return Result<SipLine>.Success(saved, "Da luu line SIP.");
        }

        public async Task<Result> AssignLine(SipAssignRequest request)
        {
            if (request.LineId <= 0)
            {
                return Result.Failure("Line SIP khong hop le.");
            }

            if (request.EmployeeId <= 0)
            {
                return Result.Failure("Nhan vien duoc gan khong hop le.");
            }

            var employee = await _unitOfWork.EmployeeRep.GetById(request.EmployeeId);
            if (employee == null || employee.Id <= 0 || employee.Deleted || employee.IsActive <= 0)
            {
                return Result.Failure("Khong tim thay nhan vien dang hoat dong de gan line.");
            }

            var line = await _unitOfWork.SipRep.GetLineById(request.LineId);
            if (line == null || line.Id <= 0 || line.Deleted || line.IsActive <= 0)
            {
                return Result.Failure("Khong tim thay line SIP kha dung.");
            }

            var ok = await _unitOfWork.SipRep.AssignLine(request.LineId, request.EmployeeId, GetUserId());
            if (ok)
            {
                var assignedLine = await _unitOfWork.SipRep.GetLineById(request.LineId);
                await SyncLineAssignmentToFreePbx(assignedLine, employee);
            }

            return ok
                ? Result.Success("Da gan line SIP cho nhan vien.")
                : Result.Failure("Khong the gan line SIP cho nhan vien.");
        }

        public async Task<Result> RevokeLine(int lineId)
        {
            if (lineId <= 0)
            {
                return Result.Failure("Line SIP khong hop le.");
            }

            var line = await _unitOfWork.SipRep.GetLineById(lineId);
            if (line == null || line.Id <= 0)
            {
                return Result.Failure("Khong tim thay line SIP.");
            }

            var lineCode = line.LineCode;
            var ok = await _unitOfWork.SipRep.RevokeLine(lineId, GetUserId());
            if (ok && !string.IsNullOrWhiteSpace(lineCode))
            {
                await UnassignLineInFreePbx(lineCode);
            }

            return ok
                ? Result.Success("Da thu hoi line SIP.")
                : Result.Failure("Khong the thu hoi line SIP.");
        }

        private async Task SyncManagedLineToFreePbx(SipLine line)
        {
            var extension = line.LineCode?.Trim();
            if (string.IsNullOrWhiteSpace(extension))
            {
                return;
            }

            var payload = new
            {
                extension,
                displayName = string.IsNullOrWhiteSpace(line.DisplayName) ? extension : line.DisplayName,
                endpointTech = "PJSIP",
                status = line.IsActive > 0 ? "active" : "inactive",
                note = line.Note,
                createdByUserId = GetUserId().ToString()
            };

            var response = await SendFreePbxJsonAsync(HttpMethod.Post, "api/sip/create", payload);
            if (response?.StatusCode == HttpStatusCode.Conflict)
            {
                await SendFreePbxJsonAsync(HttpMethod.Put, $"api/sip/{Uri.EscapeDataString(extension)}", new
                {
                    displayName = payload.displayName,
                    endpointTech = payload.endpointTech,
                    status = payload.status,
                    metadata = new
                    {
                        note = payload.note,
                        source = "crmHuman"
                    }
                });
            }
        }

        private async Task SyncLineAssignmentToFreePbx(SipLine? line, Employee employee)
        {
            var extension = line?.LineCode?.Trim();
            if (string.IsNullOrWhiteSpace(extension) || line == null)
            {
                return;
            }

            await SyncManagedLineToFreePbx(line);

            await SendFreePbxJsonAsync(HttpMethod.Post, $"api/sip/{Uri.EscapeDataString(extension)}/assign", new
            {
                employeeId = employee.Id.ToString(),
                employeeName = employee.FullName ?? employee.UserName,
                employeeCode = employee.UserName,
                assignedByUserId = GetUserId().ToString()
            });
        }

        private async Task UnassignLineInFreePbx(string lineCode)
        {
            var extension = lineCode.Trim();
            if (string.IsNullOrWhiteSpace(extension))
            {
                return;
            }

            await SendFreePbxJsonAsync(HttpMethod.Post, $"api/sip/{Uri.EscapeDataString(extension)}/unassign", new { });
        }

        private async Task<HttpResponseMessage?> SendFreePbxJsonAsync(HttpMethod method, string path, object payload)
        {
            using var client = new HttpClient
            {
                BaseAddress = new Uri(ResolveFreePbxServiceBaseUrl())
            };
            client.DefaultRequestHeaders.Add("x-user-id", GetUserId().ToString());
            client.DefaultRequestHeaders.Add("x-user-role", "admin");
            var apiKey = _configuration["Telephony:ApiKey"]?.Trim();
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            }
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var request = new HttpRequestMessage(method, path)
            {
                Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json")
            };

            try
            {
                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Conflict)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning(
                        "FreePBX SIP sync failed. Method={Method}, Path={Path}, StatusCode={StatusCode}, Body={Body}",
                        method.Method,
                        path,
                        (int)response.StatusCode,
                        body);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FreePBX SIP sync request failed. Method={Method}, Path={Path}", method.Method, path);
                return null;
            }
        }

        private string ResolveFreePbxServiceBaseUrl()
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
    }
}
