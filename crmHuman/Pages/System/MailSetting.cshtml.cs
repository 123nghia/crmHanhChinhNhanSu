using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using VS.Human.Business;
using VS.Human.Rep.Model;

namespace crmHuman.Pages.System
{
    [Authorize]
    public class MailSettingModel : BaseModel2
    {
        private readonly IEmailConfigBusiness _emailConfigBusiness;

        public MailSettingModel(IEmailConfigBusiness emailConfigBusiness)
        {
            _emailConfigBusiness = emailConfigBusiness;
            TitlePage = "Mail setting";
            KeyPage = "MailSetting";
        }

        public EmailSetting? Setting { get; set; }
        public List<EmailTemplate> Templates { get; set; } = new List<EmailTemplate>();

        public async Task OnGetAsync()
        {
            GetInfoUser();
            Setting = await _emailConfigBusiness.GetActiveSetting() ?? new EmailSetting();
            Templates = await _emailConfigBusiness.GetTemplates();
        }

        public async Task<IActionResult> OnPostSaveSettingAsync([FromBody] EmailSetting model)
        {
            GetInfoUser();
            if (!(Permision.Edit ?? false) && !(Permision.Add ?? false))
            {
                return new JsonResult(new { success = false, message = "No permission" });
            }

            var result = await _emailConfigBusiness.SaveSetting(model, UserData.UserId);
            return new JsonResult(new { success = result });
        }

        public async Task<IActionResult> OnPostSaveTemplateAsync([FromBody] EmailTemplate model)
        {
            GetInfoUser();
            if (!(Permision.Edit ?? false) && !(Permision.Add ?? false))
            {
                return new JsonResult(new { success = false, message = "No permission" });
            }

            var result = await _emailConfigBusiness.SaveTemplate(model, UserData.UserId);
            return new JsonResult(new { success = result });
        }
    }
}
