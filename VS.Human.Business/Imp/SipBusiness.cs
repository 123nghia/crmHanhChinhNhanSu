using Microsoft.AspNetCore.Http;
using VS.Human.Business.Common;
using VS.Human.Business.Model;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class SipBusiness : BaseBusiness, ISipBusiness
    {
        public SipBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor contextAccessor)
            : base(unitOfWork, contextAccessor)
        {
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

        public async Task<Result<SipServer>> SaveServer(SipServerSaveRequest request)
        {
            var host = request.Host?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(host))
            {
                return Result<SipServer>.Failure("Vui lòng nhập địa chỉ SIP server.");
            }

            if (request.Port <= 0)
            {
                return Result<SipServer>.Failure("Cổng SIP server không hợp lệ.");
            }

            SipServer server;
            if (request.Id.HasValue && request.Id.Value > 0)
            {
                server = await _unitOfWork.SipRep.GetServerById(request.Id.Value) ?? new SipServer();
                if (server.Id <= 0)
                {
                    return Result<SipServer>.Failure("Không tìm thấy SIP server cần cập nhật.");
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

            server.Name = string.IsNullOrWhiteSpace(request.Name) ? "SIP Server mặc định" : request.Name.Trim();
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
                return Result<SipServer>.Failure("Không thể lưu cấu hình SIP server.");
            }

            return Result<SipServer>.Success(saved, "Đã lưu cấu hình SIP server.");
        }

        public async Task<Result<SipLine>> SaveLine(SipLineSaveRequest request)
        {
            var lineCode = request.LineCode?.Trim() ?? string.Empty;
            var sipUserName = request.SipUserName?.Trim() ?? string.Empty;
            var sipPassword = request.SipPassword?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(lineCode))
            {
                return Result<SipLine>.Failure("Vui lòng nhập line gọi.");
            }

            if (string.IsNullOrWhiteSpace(sipUserName))
            {
                return Result<SipLine>.Failure("Vui lòng nhập tài khoản SIP.");
            }

            if (string.IsNullOrWhiteSpace(sipPassword))
            {
                return Result<SipLine>.Failure("Vui lòng nhập mật khẩu SIP.");
            }

            var duplicateLine = await _unitOfWork.SipRep.GetLineByCode(lineCode);
            if (duplicateLine != null && duplicateLine.Id > 0 && duplicateLine.Id != (request.Id ?? 0))
            {
                return Result<SipLine>.Failure("Line gọi này đã tồn tại.");
            }

            SipLine line;
            if (request.Id.HasValue && request.Id.Value > 0)
            {
                line = await _unitOfWork.SipRep.GetLineById(request.Id.Value) ?? new SipLine();
                if (line.Id <= 0)
                {
                    return Result<SipLine>.Failure("Không tìm thấy line SIP cần cập nhật.");
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
                return Result<SipLine>.Failure("Vui lòng cấu hình SIP server trước khi thêm line.");
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
                return Result<SipLine>.Failure("Không thể lưu line SIP.");
            }

            if (saved.EmployeeId.HasValue && saved.EmployeeId.Value > 0)
            {
                await _unitOfWork.SipRep.SyncEmployeeLineCode(saved.EmployeeId.Value, GetUserId());
            }

            return Result<SipLine>.Success(saved, "Đã lưu line SIP.");
        }

        public async Task<Result> AssignLine(SipAssignRequest request)
        {
            if (request.LineId <= 0)
            {
                return Result.Failure("Line SIP không hợp lệ.");
            }

            if (request.EmployeeId <= 0)
            {
                return Result.Failure("Nhân viên được gán không hợp lệ.");
            }

            var employee = await _unitOfWork.EmployeeRep.GetById(request.EmployeeId);
            if (employee == null || employee.Id <= 0 || employee.Deleted || employee.IsActive <= 0)
            {
                return Result.Failure("Không tìm thấy nhân viên đang hoạt động để gán line.");
            }

            var line = await _unitOfWork.SipRep.GetLineById(request.LineId);
            if (line == null || line.Id <= 0 || line.Deleted || line.IsActive <= 0)
            {
                return Result.Failure("Không tìm thấy line SIP khả dụng.");
            }

            var ok = await _unitOfWork.SipRep.AssignLine(request.LineId, request.EmployeeId, GetUserId());
            return ok
                ? Result.Success("Đã gán line SIP cho nhân viên.")
                : Result.Failure("Không thể gán line SIP cho nhân viên.");
        }

        public async Task<Result> RevokeLine(int lineId)
        {
            if (lineId <= 0)
            {
                return Result.Failure("Line SIP không hợp lệ.");
            }

            var line = await _unitOfWork.SipRep.GetLineById(lineId);
            if (line == null || line.Id <= 0)
            {
                return Result.Failure("Không tìm thấy line SIP.");
            }

            var ok = await _unitOfWork.SipRep.RevokeLine(lineId, GetUserId());
            return ok
                ? Result.Success("Đã thu hồi line SIP.")
                : Result.Failure("Không thể thu hồi line SIP.");
        }
    }
}
