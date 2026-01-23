using System.Net;
using System.Net.Mail;
using System.Text;
using System.Linq;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public class EmailService : IEmailService
    {
        private readonly IUnitOfWork _unitOfWork;

        public EmailService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> SendTemplateAsync(string templateCode, string toEmail, IDictionary<string, string> tokens, int? managerId = null)
        {
            if (string.IsNullOrWhiteSpace(templateCode) || string.IsNullOrWhiteSpace(toEmail))
            {
                return false;
            }

            var setting = await _unitOfWork.EmailConfigRep.GetActiveSetting();
            if (setting == null || setting.IsActive <= 0 || string.IsNullOrWhiteSpace(setting.SmtpHost))
            {
                return false;
            }

            var template = await _unitOfWork.EmailConfigRep.GetTemplateByCode(templateCode);
            if (template == null || template.IsActive <= 0)
            {
                return false;
            }

            var subject = ApplyTokens(template.Subject, tokens);
            var body = ApplyTokens(template.Body, tokens);

            var fromEmail = ResolveFromEmail(setting);
            if (string.IsNullOrWhiteSpace(fromEmail))
            {
                return false;
            }

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, setting.FromName ?? string.Empty),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };

            message.To.Add(toEmail);

            foreach (var email in ParseEmails(template.CcEmails))
            {
                if (!IsSameEmail(email, toEmail))
                {
                    message.CC.Add(email);
                }
            }

            if (template.CcManager && managerId.HasValue && managerId.Value > 0)
            {
                var manager = await _unitOfWork.EmployeeRep.GetById(managerId.Value);
                var managerEmail = GetPreferredEmail(manager);
                if (!string.IsNullOrWhiteSpace(managerEmail) && !IsSameEmail(managerEmail, toEmail))
                {
                    message.CC.Add(managerEmail);
                }
            }

            foreach (var email in ParseEmails(template.BccEmails))
            {
                if (!IsSameEmail(email, toEmail))
                {
                    message.Bcc.Add(email);
                }
            }

            try
            {
                using var client = new SmtpClient(setting.SmtpHost, setting.SmtpPort > 0 ? setting.SmtpPort : 25);
                client.EnableSsl = setting.EnableSsl;
                if (!string.IsNullOrWhiteSpace(setting.SmtpUser))
                {
                    client.Credentials = new NetworkCredential(setting.SmtpUser, setting.SmtpPassword);
                    client.UseDefaultCredentials = false;
                }
                else
                {
                    client.UseDefaultCredentials = true;
                }

                await client.SendMailAsync(message);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static string ApplyTokens(string template, IDictionary<string, string> tokens)
        {
            if (string.IsNullOrEmpty(template) || tokens == null)
            {
                return template ?? string.Empty;
            }

            var result = template;
            foreach (var item in tokens)
            {
                var key = "{" + item.Key + "}";
                result = result.Replace(key, item.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            }
            return result;
        }

        private static IEnumerable<string> ParseEmails(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return Array.Empty<string>();
            }

            return input
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static string? ResolveFromEmail(EmailSetting setting)
        {
            if (!string.IsNullOrWhiteSpace(setting.FromEmail))
            {
                return setting.FromEmail;
            }

            if (!string.IsNullOrWhiteSpace(setting.SmtpUser) && setting.SmtpUser.Contains("@"))
            {
                return setting.SmtpUser;
            }

            return null;
        }

        private static bool IsSameEmail(string? left, string? right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            {
                return false;
            }

            return string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static string? GetPreferredEmail(Employee employee)
        {
            if (employee == null || employee.Id <= 0)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(employee.Email))
            {
                return employee.Email;
            }

            if (!string.IsNullOrWhiteSpace(employee.PersonalEmail))
            {
                return employee.PersonalEmail;
            }

            return null;
        }
    }
}
