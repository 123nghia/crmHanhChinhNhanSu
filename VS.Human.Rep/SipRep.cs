using Dapper;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class SipRep : RepositoryBase<SipServer>, ISipRep
    {
        public SipRep(IConfiguration configuration) : base(configuration)
        {
            tableName = "SipServers";
        }

        public async Task<List<SipServer>> GetServers()
        {
            const string sql = @"
SELECT *
FROM SipServers
WHERE ISNULL(Deleted, 0) = 0
ORDER BY CASE WHEN ISNULL(IsActive, 0) = 1 THEN 0 ELSE 1 END, UpdateAt DESC, Id DESC;";

            return await GetDataList<SipServer>(sql) ?? new List<SipServer>();
        }

        public async Task<SipServer?> GetActiveServer()
        {
            using var con = GetConnection();
            const string sql = @"
SELECT TOP 1 *
FROM SipServers
WHERE ISNULL(Deleted, 0) = 0
ORDER BY CASE WHEN ISNULL(IsActive, 0) = 1 THEN 0 ELSE 1 END, UpdateAt DESC, Id DESC;";

            return await con.QueryFirstOrDefaultAsync<SipServer>(sql);
        }

        public async Task<SipServer?> GetServerById(int id)
        {
            if (id <= 0)
            {
                return null;
            }

            using var con = GetConnection();
            const string sql = @"
SELECT TOP 1 *
FROM SipServers
WHERE Id = @id
  AND ISNULL(Deleted, 0) = 0;";

            return await con.QueryFirstOrDefaultAsync<SipServer>(sql, new { id });
        }

        public async Task<SipServer> SaveServer(SipServer server)
        {
            using var con = (SqlConnection)GetConnection();
            using var tx = con.BeginTransaction();

            try
            {
                if (server.Id > 0)
                {
                    const string updateSql = @"
UPDATE SipServers
SET Name = @Name,
    Host = @Host,
    Port = @Port,
    Domain = @Domain,
    Transport = @Transport,
    OutboundProxy = @OutboundProxy,
    Note = @Note,
    IsActive = @IsActive,
    UpdatedBy = @UpdatedBy,
    UpdateAt = GETDATE()
WHERE Id = @Id;";

                    await con.ExecuteAsync(updateSql, server, tx);
                }
                else
                {
                    const string insertSql = @"
INSERT INTO SipServers
(
    Name, Host, Port, Domain, Transport, OutboundProxy, Note,
    IsActive, Deleted, CreatedBy, UpdatedBy, CreateAt, UpdateAt
)
VALUES
(
    @Name, @Host, @Port, @Domain, @Transport, @OutboundProxy, @Note,
    @IsActive, 0, @CreatedBy, @UpdatedBy, GETDATE(), GETDATE()
);
SELECT CAST(SCOPE_IDENTITY() AS int);";

                    server.Id = await con.ExecuteScalarAsync<int>(insertSql, server, tx);
                }

                if (server.IsActive > 0)
                {
                    const string deactivateSql = @"
UPDATE SipServers
SET IsActive = 0,
    UpdatedBy = @UpdatedBy,
    UpdateAt = GETDATE()
WHERE Id <> @Id
  AND ISNULL(Deleted, 0) = 0;";

                    await con.ExecuteAsync(deactivateSql, new
                    {
                        server.Id,
                        server.UpdatedBy
                    }, tx);
                }

                tx.Commit();
                return await GetServerById(server.Id) ?? new SipServer { Id = -1 };
            }
            catch
            {
                tx.Rollback();
                return new SipServer { Id = -1 };
            }
        }

        public async Task<List<SipLineViewModel>> GetLines()
        {
            using var con = GetConnection();
            const string sql = @"
SELECT
    l.Id,
    l.CreateAt,
    l.CreatedBy,
    l.UpdateAt,
    l.UpdatedBy,
    CAST(ISNULL(l.Deleted, 0) AS bit) AS Deleted,
    l.SipServerId,
    s.Name AS ServerName,
    s.Host AS ServerHost,
    s.Port AS ServerPort,
    s.Domain AS ServerDomain,
    s.Transport AS ServerTransport,
    s.OutboundProxy AS ServerOutboundProxy,
    l.LineCode,
    l.SipUserName,
    l.SipPassword,
    l.AuthUser,
    l.DisplayName,
    l.EmployeeId,
    e.FullName AS EmployeeName,
    e.UserName AS EmployeeUserName,
    l.AssignedAt,
    l.Note,
    l.IsActive
FROM SipLines l
LEFT JOIN SipServers s ON s.Id = l.SipServerId AND ISNULL(s.Deleted, 0) = 0
LEFT JOIN Employees e ON e.Id = l.EmployeeId AND ISNULL(e.Deleted, 0) = 0
WHERE ISNULL(l.Deleted, 0) = 0
ORDER BY l.LineCode, l.Id DESC;";

            var result = await con.QueryAsync<SipLineViewModel>(sql);
            return result?.ToList() ?? new List<SipLineViewModel>();
        }

        public async Task<SipLine?> GetLineById(int id)
        {
            if (id <= 0)
            {
                return null;
            }

            using var con = GetConnection();
            const string sql = @"
SELECT TOP 1 *
FROM SipLines
WHERE Id = @id
  AND ISNULL(Deleted, 0) = 0;";

            return await con.QueryFirstOrDefaultAsync<SipLine>(sql, new { id });
        }

        public async Task<SipLine?> GetLineByCode(string lineCode)
        {
            if (string.IsNullOrWhiteSpace(lineCode))
            {
                return null;
            }

            using var con = GetConnection();
            const string sql = @"
SELECT TOP 1 *
FROM SipLines
WHERE LineCode = @lineCode
  AND ISNULL(Deleted, 0) = 0
ORDER BY Id DESC;";

            return await con.QueryFirstOrDefaultAsync<SipLine>(sql, new
            {
                lineCode = lineCode.Trim()
            });
        }

        public async Task<SipLine> SaveLine(SipLine line)
        {
            using var con = GetConnection();

            if (line.Id > 0)
            {
                const string updateSql = @"
UPDATE SipLines
SET SipServerId = @SipServerId,
    LineCode = @LineCode,
    SipUserName = @SipUserName,
    SipPassword = @SipPassword,
    AuthUser = @AuthUser,
    DisplayName = @DisplayName,
    Note = @Note,
    IsActive = @IsActive,
    UpdatedBy = @UpdatedBy,
    UpdateAt = GETDATE()
WHERE Id = @Id;";

                await con.ExecuteAsync(updateSql, line);
            }
            else
            {
                const string insertSql = @"
INSERT INTO SipLines
(
    SipServerId, LineCode, SipUserName, SipPassword, AuthUser, DisplayName,
    EmployeeId, AssignedAt, AssignedBy, RevokedAt, RevokedBy, Note,
    IsActive, Deleted, CreatedBy, UpdatedBy, CreateAt, UpdateAt
)
VALUES
(
    @SipServerId, @LineCode, @SipUserName, @SipPassword, @AuthUser, @DisplayName,
    NULL, NULL, NULL, NULL, NULL, @Note,
    @IsActive, 0, @CreatedBy, @UpdatedBy, GETDATE(), GETDATE()
);
SELECT CAST(SCOPE_IDENTITY() AS int);";

                line.Id = await con.ExecuteScalarAsync<int>(insertSql, line);
            }

            return await GetLineById(line.Id) ?? new SipLine { Id = -1 };
        }

        public async Task<bool> AssignLine(int lineId, int employeeId, int updatedBy)
        {
            using var con = (SqlConnection)GetConnection();
            using var tx = con.BeginTransaction();

            try
            {
                const string getLineSql = @"
SELECT TOP 1 *
FROM SipLines
WHERE Id = @lineId
  AND ISNULL(Deleted, 0) = 0;";

                var currentLine = await con.QueryFirstOrDefaultAsync<SipLine>(getLineSql, new { lineId }, tx);
                if (currentLine == null || currentLine.Id <= 0 || currentLine.IsActive <= 0)
                {
                    tx.Rollback();
                    return false;
                }

                var previousEmployeeId = currentLine.EmployeeId;

                const string clearEmployeeAssignmentsSql = @"
UPDATE SipLines
SET EmployeeId = NULL,
    RevokedAt = GETDATE(),
    RevokedBy = @updatedBy,
    UpdatedBy = @updatedBy,
    UpdateAt = GETDATE()
WHERE EmployeeId = @employeeId
  AND Id <> @lineId
  AND ISNULL(Deleted, 0) = 0;";

                await con.ExecuteAsync(clearEmployeeAssignmentsSql, new
                {
                    employeeId,
                    lineId,
                    updatedBy
                }, tx);

                const string assignSql = @"
UPDATE SipLines
SET EmployeeId = @employeeId,
    AssignedAt = CASE WHEN EmployeeId IS NULL OR EmployeeId <> @employeeId THEN GETDATE() ELSE AssignedAt END,
    AssignedBy = @updatedBy,
    RevokedAt = NULL,
    RevokedBy = NULL,
    UpdatedBy = @updatedBy,
    UpdateAt = GETDATE()
WHERE Id = @lineId
  AND ISNULL(Deleted, 0) = 0;";

                await con.ExecuteAsync(assignSql, new
                {
                    employeeId,
                    lineId,
                    updatedBy
                }, tx);

                await SyncEmployeeLineCodeInternal(con, tx, employeeId, updatedBy);
                if (previousEmployeeId.HasValue && previousEmployeeId.Value > 0 && previousEmployeeId.Value != employeeId)
                {
                    await SyncEmployeeLineCodeInternal(con, tx, previousEmployeeId.Value, updatedBy);
                }

                tx.Commit();
                return true;
            }
            catch
            {
                tx.Rollback();
                return false;
            }
        }

        public async Task<bool> RevokeLine(int lineId, int updatedBy)
        {
            using var con = (SqlConnection)GetConnection();
            using var tx = con.BeginTransaction();

            try
            {
                const string getLineSql = @"
SELECT TOP 1 *
FROM SipLines
WHERE Id = @lineId
  AND ISNULL(Deleted, 0) = 0;";

                var currentLine = await con.QueryFirstOrDefaultAsync<SipLine>(getLineSql, new { lineId }, tx);
                if (currentLine == null || currentLine.Id <= 0)
                {
                    tx.Rollback();
                    return false;
                }

                const string revokeSql = @"
UPDATE SipLines
SET EmployeeId = NULL,
    RevokedAt = GETDATE(),
    RevokedBy = @updatedBy,
    UpdatedBy = @updatedBy,
    UpdateAt = GETDATE()
WHERE Id = @lineId
  AND ISNULL(Deleted, 0) = 0;";

                await con.ExecuteAsync(revokeSql, new
                {
                    lineId,
                    updatedBy
                }, tx);

                if (currentLine.EmployeeId.HasValue && currentLine.EmployeeId.Value > 0)
                {
                    await SyncEmployeeLineCodeInternal(con, tx, currentLine.EmployeeId.Value, updatedBy);
                }

                tx.Commit();
                return true;
            }
            catch
            {
                tx.Rollback();
                return false;
            }
        }

        public async Task<bool> SyncEmployeeLineCode(int employeeId, int updatedBy)
        {
            if (employeeId <= 0)
            {
                return false;
            }

            using var con = (SqlConnection)GetConnection();
            using var tx = con.BeginTransaction();

            try
            {
                await SyncEmployeeLineCodeInternal(con, tx, employeeId, updatedBy);
                tx.Commit();
                return true;
            }
            catch
            {
                tx.Rollback();
                return false;
            }
        }

        public async Task<List<SipEmployeeOption>> GetAssignableEmployees()
        {
            using var con = GetConnection();
            const string sql = @"
SELECT
    e.Id,
    e.UserName,
    e.FullName,
    e.DepartmentCode,
    dbo.getDisplayMasterdata(e.DepartmentCode) AS DepartmentText,
    e.LineCode
FROM Employees e
WHERE ISNULL(e.Deleted, 0) = 0
  AND ISNULL(e.IsActive, 0) = 1
ORDER BY e.FullName, e.UserName;";

            var result = await con.QueryAsync<SipEmployeeOption>(sql);
            return result?.ToList() ?? new List<SipEmployeeOption>();
        }

        public async Task<EmployeeSipAccountView?> GetEmployeeSipInfo(int employeeId)
        {
            if (employeeId <= 0)
            {
                return null;
            }

            using var con = GetConnection();
            const string sql = @"
SELECT TOP 1
    CAST(CASE WHEN lineInfo.Id IS NULL THEN 0 ELSE 1 END AS bit) AS HasAssignedLine,
    lineInfo.Id AS LineId,
    lineInfo.LineCode,
    lineInfo.SipUserName,
    lineInfo.SipPassword,
    lineInfo.AuthUser,
    lineInfo.DisplayName,
    lineInfo.AssignedAt,
    lineInfo.Note AS LineNote,
    COALESCE(lineServer.Id, activeServer.Id) AS SipServerId,
    COALESCE(lineServer.Name, activeServer.Name) AS ServerName,
    COALESCE(lineServer.Host, activeServer.Host) AS ServerHost,
    COALESCE(lineServer.Port, activeServer.Port) AS ServerPort,
    COALESCE(lineServer.Domain, activeServer.Domain) AS ServerDomain,
    COALESCE(lineServer.Transport, activeServer.Transport) AS ServerTransport,
    COALESCE(lineServer.OutboundProxy, activeServer.OutboundProxy) AS ServerOutboundProxy,
    COALESCE(lineServer.Note, activeServer.Note) AS ServerNote
FROM Employees e
OUTER APPLY
(
    SELECT TOP 1 *
    FROM SipLines l
    WHERE l.EmployeeId = e.Id
      AND ISNULL(l.Deleted, 0) = 0
    ORDER BY l.AssignedAt DESC, l.UpdateAt DESC, l.Id DESC
) lineInfo
OUTER APPLY
(
    SELECT TOP 1 *
    FROM SipServers s
    WHERE s.Id = lineInfo.SipServerId
      AND ISNULL(s.Deleted, 0) = 0
) lineServer
OUTER APPLY
(
    SELECT TOP 1 *
    FROM SipServers s
    WHERE ISNULL(s.Deleted, 0) = 0
    ORDER BY CASE WHEN ISNULL(s.IsActive, 0) = 1 THEN 0 ELSE 1 END, s.UpdateAt DESC, s.Id DESC
) activeServer
WHERE e.Id = @employeeId
  AND ISNULL(e.Deleted, 0) = 0;";

            return await con.QueryFirstOrDefaultAsync<EmployeeSipAccountView>(sql, new { employeeId });
        }

        private static async Task SyncEmployeeLineCodeInternal(SqlConnection connection, SqlTransaction transaction, int employeeId, int updatedBy)
        {
            const string selectSql = @"
SELECT TOP 1 LineCode
FROM SipLines
WHERE EmployeeId = @employeeId
  AND ISNULL(Deleted, 0) = 0
ORDER BY AssignedAt DESC, UpdateAt DESC, Id DESC;";

            var lineCode = await connection.QueryFirstOrDefaultAsync<string?>(selectSql, new { employeeId }, transaction);

            const string updateEmployeeSql = @"
UPDATE Employees
SET LineCode = @lineCode,
    UpdatedBy = @updatedBy,
    UpdateAt = GETDATE()
WHERE Id = @employeeId
  AND ISNULL(Deleted, 0) = 0;";

            await connection.ExecuteAsync(updateEmployeeSql, new
            {
                employeeId,
                lineCode,
                updatedBy
            }, transaction);
        }
    }
}
