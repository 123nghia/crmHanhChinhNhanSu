using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using VS.Human.Business.Imp;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public class EmailConfigBusiness : BaseBusiness, IEmailConfigBusiness
    {
        public EmailConfigBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor contextAccessor)
            : base(unitOfWork, contextAccessor)
        {
        }

        public async Task<EmailSetting?> GetActiveSetting()
        {
            return await _unitOfWork.EmailConfigRep.GetActiveSetting();
        }

        public async Task<List<EmailTemplate>> GetTemplates()
        {
            return await _unitOfWork.EmailConfigRep.GetTemplates();
        }

        public async Task<bool> SaveSetting(EmailSetting setting, int userId)
        {
            if (setting == null)
            {
                return false;
            }

            if (setting.Id > 0)
            {
                var existing = await _unitOfWork.EmailConfigRep.GetSettingById(setting.Id);
                if (existing != null && string.IsNullOrWhiteSpace(setting.SmtpPassword))
                {
                    setting.SmtpPassword = existing.SmtpPassword;
                }
            }

            setting.UpdatedBy = userId;
            if (setting.Id <= 0)
            {
                setting.CreatedBy = userId;
            }

            return await _unitOfWork.EmailConfigRep.SaveSetting(setting);
        }

        public async Task<bool> SaveTemplate(EmailTemplate template, int userId)
        {
            if (template == null || string.IsNullOrWhiteSpace(template.Code))
            {
                return false;
            }

            template.UpdatedBy = userId;
            if (template.Id <= 0)
            {
                template.CreatedBy = userId;
            }

            return await _unitOfWork.EmailConfigRep.SaveTemplate(template);
        }
    }
}
