using Dapper;
using Microsoft.Extensions.Configuration;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class UserThemeSettingRep : RepositoryBase<UserThemeSetting>, IUserThemeSettingRep
    {
        public UserThemeSettingRep(IConfiguration configuration) : base(configuration)
        {
        }

        public async Task<UserThemeSetting?> GetByUserIdAsync(int userId)
        {
            using (var con = GetConnection())
            {
                var sql = @"
                    SELECT TOP 1 *
                    FROM UserThemeSettings
                    WHERE UserId = @userId AND ISNULL(Deleted, 0) = 0
                    ORDER BY UpdateAt DESC, Id DESC";
                return await con.QueryFirstOrDefaultAsync<UserThemeSetting>(sql, new { userId });
            }
        }

        public async Task<bool> SaveAsync(UserThemeSetting setting)
        {
            using (var con = GetConnection())
            {
                var existingId = await con.ExecuteScalarAsync<int?>(
                    "SELECT TOP 1 Id FROM UserThemeSettings WHERE UserId = @UserId AND ISNULL(Deleted, 0) = 0",
                    new { setting.UserId });

                if (existingId.HasValue && existingId.Value > 0)
                {
                    setting.Id = existingId.Value;
                    var sql = @"
                        UPDATE UserThemeSettings
                        SET PrimaryColor = @PrimaryColor,
                            ButtonColor = @ButtonColor,
                            BackgroundColor = @BackgroundColor,
                            UpdatedBy = @UpdatedBy,
                            UpdateAt = GETDATE(),
                            IsActive = 1,
                            Deleted = 0
                        WHERE Id = @Id";
                    var affected = await con.ExecuteAsync(sql, setting);
                    return affected > 0;
                }
                else
                {
                    var sql = @"
                        INSERT INTO UserThemeSettings
                        (UserId, PrimaryColor, ButtonColor, BackgroundColor, IsActive, Deleted, CreatedBy, UpdatedBy, CreateAt, UpdateAt)
                        VALUES
                        (@UserId, @PrimaryColor, @ButtonColor, @BackgroundColor, 1, 0, @CreatedBy, @UpdatedBy, GETDATE(), GETDATE());
                        SELECT CAST(SCOPE_IDENTITY() as int);";
                    var newId = await con.ExecuteScalarAsync<int>(sql, setting);
                    return newId > 0;
                }
            }
        }
    }
}
