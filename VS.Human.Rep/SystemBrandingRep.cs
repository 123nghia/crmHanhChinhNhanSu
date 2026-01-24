using Dapper;
using Microsoft.Extensions.Configuration;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class SystemBrandingRep : RepositoryBase<SystemBranding>, ISystemBrandingRep
    {
        public SystemBrandingRep(IConfiguration configuration) : base(configuration)
        {
        }

        public async Task<SystemBranding?> GetActiveAsync()
        {
            using (var con = GetConnection())
            {
                var sql = @"
                    SELECT TOP 1 *
                    FROM SystemBranding
                    WHERE ISNULL(Deleted, 0) = 0 AND ISNULL(IsActive, 0) = 1
                    ORDER BY UpdateAt DESC, Id DESC";
                return await con.QueryFirstOrDefaultAsync<SystemBranding>(sql);
            }
        }

        public async Task<bool> SaveAsync(SystemBranding branding)
        {
            using (var con = GetConnection())
            {
                var existingId = await con.ExecuteScalarAsync<int?>(
                    "SELECT TOP 1 Id FROM SystemBranding WHERE ISNULL(Deleted, 0) = 0 ORDER BY UpdateAt DESC, Id DESC");

                if (existingId.HasValue && existingId.Value > 0)
                {
                    branding.Id = existingId.Value;
                    var sql = @"
                        UPDATE SystemBranding
                        SET LogoPath = @LogoPath,
                            UpdatedBy = @UpdatedBy,
                            UpdateAt = GETDATE(),
                            IsActive = 1,
                            Deleted = 0
                        WHERE Id = @Id";
                    var affected = await con.ExecuteAsync(sql, branding);
                    return affected > 0;
                }
                else
                {
                    var sql = @"
                        INSERT INTO SystemBranding
                        (LogoPath, IsActive, Deleted, CreatedBy, UpdatedBy, CreateAt, UpdateAt)
                        VALUES
                        (@LogoPath, 1, 0, @CreatedBy, @UpdatedBy, GETDATE(), GETDATE());
                        SELECT CAST(SCOPE_IDENTITY() as int);";
                    var newId = await con.ExecuteScalarAsync<int>(sql, branding);
                    return newId > 0;
                }
            }
        }
    }
}
