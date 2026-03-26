using Dapper;
using Microsoft.Extensions.Configuration;
using System.Linq;
using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public class EmailConfigRep : RepositoryBase<EmailSetting>, IEmailConfigRep
    {
        public EmailConfigRep(IConfiguration configuration) : base(configuration)
        {
        }

        public async Task<EmailSetting?> GetActiveSetting()
        {
            using (var con = GetConnection())
            {
                var sql = @"
                    SELECT TOP 1 *
                    FROM EmailSettings
                    WHERE ISNULL(Deleted, 0) = 0 AND ISNULL(IsActive, 0) = 1
                    ORDER BY UpdateAt DESC, Id DESC";
                return await con.QueryFirstOrDefaultAsync<EmailSetting>(sql);
            }
        }

        public async Task<EmailSetting?> GetSettingById(int id)
        {
            using (var con = GetConnection())
            {
                var sql = "SELECT TOP 1 * FROM EmailSettings WHERE Id = @id AND ISNULL(Deleted,0) = 0";
                return await con.QueryFirstOrDefaultAsync<EmailSetting>(sql, new { id });
            }
        }

        public async Task<bool> SaveSetting(EmailSetting setting)
        {
            using (var con = GetConnection())
            {
                if (setting.Id > 0)
                {
                    var sql = @"
                        UPDATE EmailSettings
                        SET SmtpHost = @SmtpHost,
                            SmtpPort = @SmtpPort,
                            EnableSsl = @EnableSsl,
                            SmtpUser = @SmtpUser,
                            SmtpPassword = @SmtpPassword,
                            FromEmail = @FromEmail,
                            FromName = @FromName,
                            HrFromEmail = @HrFromEmail,
                            HrFromName = @HrFromName,
                            HrSignature = @HrSignature,
                            EmployeeFromEmail = @EmployeeFromEmail,
                            EmployeeFromName = @EmployeeFromName,
                            EmployeeSignature = @EmployeeSignature,
                            IsActive = @IsActive,
                            UpdatedBy = @UpdatedBy,
                            UpdateAt = GETDATE()
                        WHERE Id = @Id";
                    var affected = await con.ExecuteAsync(sql, setting);
                    if (setting.IsActive > 0)
                    {
                        await con.ExecuteAsync("UPDATE EmailSettings SET IsActive = 0 WHERE Id <> @Id", new { setting.Id });
                    }
                    else
                    {
                        await con.ExecuteAsync("UPDATE EmailSettings SET IsActive = 0 WHERE ISNULL(Deleted,0) = 0");
                    }
                    return affected > 0;
                }
                else
                {
                    var sql = @"
                        INSERT INTO EmailSettings
                        (SmtpHost, SmtpPort, EnableSsl, SmtpUser, SmtpPassword, FromEmail, FromName, HrFromEmail, HrFromName, HrSignature, EmployeeFromEmail, EmployeeFromName, EmployeeSignature, IsActive, Deleted, CreatedBy, UpdatedBy, CreateAt, UpdateAt)
                        VALUES
                        (@SmtpHost, @SmtpPort, @EnableSsl, @SmtpUser, @SmtpPassword, @FromEmail, @FromName, @HrFromEmail, @HrFromName, @HrSignature, @EmployeeFromEmail, @EmployeeFromName, @EmployeeSignature, @IsActive, 0, @CreatedBy, @UpdatedBy, GETDATE(), GETDATE());
                        SELECT CAST(SCOPE_IDENTITY() as int);";
                    var newId = await con.ExecuteScalarAsync<int>(sql, setting);
                    if (setting.IsActive > 0 && newId > 0)
                    {
                        await con.ExecuteAsync("UPDATE EmailSettings SET IsActive = 0 WHERE Id <> @Id", new { Id = newId });
                    }
                    else if (newId > 0)
                    {
                        await con.ExecuteAsync("UPDATE EmailSettings SET IsActive = 0 WHERE ISNULL(Deleted,0) = 0");
                    }
                    return newId > 0;
                }
            }
        }

        public async Task<List<EmailTemplate>> GetTemplates()
        {
            using (var con = GetConnection())
            {
                var sql = @"
                    SELECT *
                    FROM EmailTemplates
                    WHERE ISNULL(Deleted, 0) = 0
                    ORDER BY Code";
                var result = await con.QueryAsync<EmailTemplate>(sql);
                return result.ToList();
            }
        }

        public async Task<EmailTemplate?> GetTemplateById(int id)
        {
            using (var con = GetConnection())
            {
                var sql = "SELECT TOP 1 * FROM EmailTemplates WHERE Id = @id AND ISNULL(Deleted,0) = 0";
                return await con.QueryFirstOrDefaultAsync<EmailTemplate>(sql, new { id });
            }
        }

        public async Task<EmailTemplate?> GetTemplateByCode(string code)
        {
            using (var con = GetConnection())
            {
                var sql = @"
                    SELECT TOP 1 *
                    FROM EmailTemplates
                    WHERE Code = @code AND ISNULL(Deleted, 0) = 0 AND ISNULL(IsActive, 0) = 1";
                return await con.QueryFirstOrDefaultAsync<EmailTemplate>(sql, new { code });
            }
        }

        public async Task<bool> SaveTemplate(EmailTemplate template)
        {
            using (var con = GetConnection())
            {
                if (template.Id > 0)
                {
                    var sql = @"
                        UPDATE EmailTemplates
                        SET Code = @Code,
                            Name = @Name,
                            Subject = @Subject,
                            Body = @Body,
                            SenderType = @SenderType,
                            CcManager = @CcManager,
                            CcEmails = @CcEmails,
                            BccEmails = @BccEmails,
                            IsActive = @IsActive,
                            UpdatedBy = @UpdatedBy,
                            UpdateAt = GETDATE()
                        WHERE Id = @Id";
                    var affected = await con.ExecuteAsync(sql, template);
                    return affected > 0;
                }
                else
                {
                    var sql = @"
                        INSERT INTO EmailTemplates
                        (Code, Name, Subject, Body, SenderType, CcManager, CcEmails, BccEmails, IsActive, Deleted, CreatedBy, UpdatedBy, CreateAt, UpdateAt)
                        VALUES
                        (@Code, @Name, @Subject, @Body, @SenderType, @CcManager, @CcEmails, @BccEmails, @IsActive, 0, @CreatedBy, @UpdatedBy, GETDATE(), GETDATE())";
                    var affected = await con.ExecuteAsync(sql, template);
                    return affected > 0;
                }
            }
        }
    }
}
