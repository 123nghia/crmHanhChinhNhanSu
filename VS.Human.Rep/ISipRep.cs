using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface ISipRep
    {
        Task<List<SipServer>> GetServers();
        Task<SipServer?> GetActiveServer();
        Task<SipServer?> GetServerById(int id);
        Task<SipServer> SaveServer(SipServer server);

        Task<List<SipLineViewModel>> GetLines();
        Task<SipLine?> GetLineById(int id);
        Task<SipLine?> GetLineByCode(string lineCode);
        Task<SipLine> SaveLine(SipLine line);
        Task<bool> AssignLine(int lineId, int employeeId, int updatedBy);
        Task<bool> RevokeLine(int lineId, int updatedBy);
        Task<bool> SyncEmployeeLineCode(int employeeId, int updatedBy);

        Task<List<SipEmployeeOption>> GetAssignableEmployees();
        Task<EmployeeSipAccountView?> GetEmployeeSipInfo(int employeeId);
    }
}
