using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using VS.Human.Business;
using VS.Human.Rep.Model;

namespace crmHuman.Pages.System.MailGroup
{
    [Authorize]
    public class EditModel : BaseModel2
    {
        private readonly IMailGroupBusiness _mailGroupBusiness;

        [BindProperty]
        public MailGroupEditorInput FormModel { get; set; } = new MailGroupEditorInput();

        public bool IsEditMode => FormModel.Id > 0;

        public EditModel(IMailGroupBusiness mailGroupBusiness)
        {
            _mailGroupBusiness = mailGroupBusiness;
            KeyPage = "MailGroup";
            TitlePage = "Them nhom mail";
        }

        public async Task<IActionResult> OnGet(int? id)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (!CanManage())
            {
                return Redirect("/System/MailGroup");
            }

            if (id.HasValue && id.Value > 0)
            {
                var item = await _mailGroupBusiness.GetById(id.Value);
                if (item == null || item.Id <= 0)
                {
                    return Redirect("/System/MailGroup");
                }

                FormModel = new MailGroupEditorInput
                {
                    Id = item.Id,
                    Name = item.Name ?? string.Empty,
                    Description = item.Description ?? string.Empty,
                    IncludeAllCompanyEmails = item.IncludeAllCompanyEmails,
                    IncludeAllPersonalEmails = item.IncludeAllPersonalEmails,
                    IsBlocked = item.IsBlocked,
                    ManualRecipientEmails = string.Join(Environment.NewLine, item.Recipients.Where(x => !x.IsBlocked).Select(FormatRecipient)),
                    BlockedRecipientEmails = string.Join(Environment.NewLine, item.Recipients.Where(x => x.IsBlocked).Select(FormatRecipient))
                };

                TitlePage = "Chinh sua nhom mail";
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (!CanManage())
            {
                return Redirect("/System/MailGroup");
            }

            TitlePage = FormModel.Id > 0 ? "Chinh sua nhom mail" : "Them nhom mail";

            if (string.IsNullOrWhiteSpace(FormModel.Name))
            {
                ModelState.AddModelError("FormModel.Name", "Vui long nhap ten nhom.");
            }
            else if (FormModel.Name.Length > 200)
            {
                ModelState.AddModelError("FormModel.Name", "Ten nhom toi da 200 ky tu.");
            }

            var invalidEntries = new List<string>();
            var recipients = ParseRecipients(FormModel.ManualRecipientEmails, false, invalidEntries);
            recipients.AddRange(ParseRecipients(FormModel.BlockedRecipientEmails, true, invalidEntries));

            if (invalidEntries.Count > 0)
            {
                ModelState.AddModelError(string.Empty, $"Email khong hop le: {string.Join(", ", invalidEntries.Distinct(StringComparer.OrdinalIgnoreCase))}");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existing = FormModel.Id > 0 ? await _mailGroupBusiness.GetById(FormModel.Id) : null;
            if (FormModel.Id > 0 && (existing == null || existing.Id <= 0))
            {
                return Redirect("/System/MailGroup");
            }

            var item = new MailGroupItem
            {
                Id = FormModel.Id,
                Name = FormModel.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(FormModel.Description) ? null : FormModel.Description.Trim(),
                IncludeAllCompanyEmails = FormModel.IncludeAllCompanyEmails,
                IncludeAllPersonalEmails = FormModel.IncludeAllPersonalEmails,
                IsBlocked = FormModel.IsBlocked,
                IsActive = 1,
                CreatedBy = existing != null && existing.Id > 0 ? existing.CreatedBy : UserData.UserId,
                UpdatedBy = UserData.UserId,
                Recipients = recipients
            };

            var result = await _mailGroupBusiness.Save(item);
            if (!result)
            {
                ModelState.AddModelError(string.Empty, "Khong the luu nhom mail.");
                return Page();
            }

            TempData["SuccessMessage"] = FormModel.Id > 0 ? "Da cap nhat nhom mail." : "Da tao nhom mail.";
            return Redirect("/System/MailGroup");
        }

        private bool CanManage()
        {
            return (Permision.Add ?? false) || (Permision.Edit ?? false) || UserData.RoleCode == "1";
        }

        private static List<MailGroupRecipient> ParseRecipients(string? rawInput, bool isBlocked, List<string> invalidEntries)
        {
            var recipients = new List<MailGroupRecipient>();
            if (string.IsNullOrWhiteSpace(rawInput))
            {
                return recipients;
            }

            var entries = rawInput
                .Split(new[] { '\r', '\n', ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x));

            foreach (var entry in entries)
            {
                try
                {
                    var address = new MailAddress(entry);
                    recipients.Add(new MailGroupRecipient
                    {
                        Email = address.Address,
                        DisplayName = string.IsNullOrWhiteSpace(address.DisplayName) ? null : address.DisplayName,
                        IsBlocked = isBlocked
                    });
                }
                catch
                {
                    invalidEntries.Add(entry);
                }
            }

            return recipients;
        }

        private static string FormatRecipient(MailGroupRecipient recipient)
        {
            if (!string.IsNullOrWhiteSpace(recipient.DisplayName))
            {
                return $"{recipient.DisplayName} <{recipient.Email}>";
            }

            return recipient.Email ?? string.Empty;
        }
    }

    public class MailGroupEditorInput
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IncludeAllCompanyEmails { get; set; }
        public bool IncludeAllPersonalEmails { get; set; }
        public bool IsBlocked { get; set; }
        public string ManualRecipientEmails { get; set; } = string.Empty;
        public string BlockedRecipientEmails { get; set; } = string.Empty;
    }
}
