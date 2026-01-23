using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IEmailConfigRep
    {
        Task<EmailSetting?> GetActiveSetting();
        Task<EmailSetting?> GetSettingById(int id);
        Task<bool> SaveSetting(EmailSetting setting);

        Task<List<EmailTemplate>> GetTemplates();
        Task<EmailTemplate?> GetTemplateById(int id);
        Task<EmailTemplate?> GetTemplateByCode(string code);
        Task<bool> SaveTemplate(EmailTemplate template);
    }
}
