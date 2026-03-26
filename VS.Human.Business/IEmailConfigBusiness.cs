using System.Collections.Generic;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface IEmailConfigBusiness
    {
        Task<EmailSetting?> GetActiveSetting();
        Task<List<EmailTemplate>> GetTemplates();
        Task EnsureDefaultTemplates(int userId);
        Task<bool> SaveSetting(EmailSetting setting, int userId);
        Task<bool> SaveTemplate(EmailTemplate template, int userId);
    }
}
