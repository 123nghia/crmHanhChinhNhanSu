using VS.Human.Business.Common;
using VS.Human.Business.Model;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface ISipBusiness
    {
        Task<List<SipServer>> GetServers();
        Task<SipServer?> GetActiveServer();
        Task<List<SipLineViewModel>> GetLines();
        Task<List<SipEmployeeOption>> GetAssignableEmployees();
        Task<EmployeeSipAccountView?> GetEmployeeSipInfo(int employeeId);
        Task<Result<SipServiceHealthResult>> CheckServiceHealth();

        Task<Result<SipServer>> SaveServer(SipServerSaveRequest request);
        Task<Result<SipLine>> SaveLine(SipLineSaveRequest request);
        Task<Result> AssignLine(SipAssignRequest request);
        Task<Result> RevokeLine(int lineId);
    }
}
