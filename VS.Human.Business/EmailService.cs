using System.Text;
using System.Linq;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Utils;
using VS.Human.Business.Helpers;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public class EmailService : IEmailService
    {
        private static readonly Regex ImageSourceRegex = new(
            "(<img\\b[^>]*?\\bsrc\\s*=\\s*[\"'])(?<src>[^\"']+)([\"'][^>]*>)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHostEnvironment _hostEnvironment;

        public EmailService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, IHostEnvironment hostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<bool> SendTemplateAsync(string templateCode, string toEmail, IDictionary<string, string> tokens, int? managerId = null, int? senderEmployeeId = null)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                return false;
            }

            return await SendTemplateAsync(templateCode, new[] { toEmail }, tokens, null, null, managerId, senderEmployeeId);
        }

        public async Task<bool> SendTemplateAsync(string templateCode, IEnumerable<string> toEmails, IDictionary<string, string> tokens, IEnumerable<string>? ccEmails = null, IEnumerable<string>? bccEmails = null, int? managerId = null, int? senderEmployeeId = null)
        {
            var result = await SendTemplateWithErrorAsync(templateCode, toEmails, tokens, ccEmails, bccEmails, managerId, senderEmployeeId);
            return result.Success;
        }

        public async Task<(bool Success, string? Error)> SendTemplateWithErrorAsync(string templateCode, IEnumerable<string> toEmails, IDictionary<string, string> tokens, IEnumerable<string>? ccEmails = null, IEnumerable<string>? bccEmails = null, int? managerId = null, int? senderEmployeeId = null)
        {
            var normalizedTo = NormalizeEmails(toEmails).ToList();
            var normalizedCcInput = NormalizeEmails(ccEmails).ToList();
            var normalizedBccInput = NormalizeEmails(bccEmails).ToList();
            var log = CreateEmailSentLog(templateCode, normalizedTo, normalizedCcInput, normalizedBccInput, managerId, senderEmployeeId);

            if (string.IsNullOrWhiteSpace(templateCode) || toEmails == null)
            {
                return await FinalizeEmailResultAsync(log, false, "Template code or recipient is missing");
            }

            if (normalizedTo.Count == 0)
            {
                return await FinalizeEmailResultAsync(log, false, "Recipient email is missing");
            }

            var setting = await _unitOfWork.EmailConfigRep.GetActiveSetting();
            if (setting == null || setting.IsActive <= 0 || string.IsNullOrWhiteSpace(setting.SmtpHost))
            {
                return await FinalizeEmailResultAsync(log, false, "SMTP is not configured or inactive");
            }

            setting.HrSignature = EmailSignatureHtmlNormalizer.NormalizeSignatureHtml(setting.HrSignature);
            setting.EmployeeSignature = EmailSignatureHtmlNormalizer.NormalizeSignatureHtml(setting.EmployeeSignature);

            var template = await _unitOfWork.EmailConfigRep.GetTemplateByCode(templateCode);
            if (template == null || template.IsActive <= 0)
            {
                return await FinalizeEmailResultAsync(log, false, "Template not found or inactive");
            }

            log.TemplateId = template.Id > 0 ? template.Id : null;
            log.TemplateCode = TrimToLength(template.Code, 50);

            var senderEmployee = await ResolveSenderEmployeeAsync(senderEmployeeId);
            log.SenderEmployeeId ??= senderEmployee?.Id;
            if (string.IsNullOrWhiteSpace(log.TriggeredByUserName))
            {
                log.TriggeredByUserName = TrimToLength(senderEmployee?.UserName, 200);
            }
            if (string.IsNullOrWhiteSpace(log.TriggeredByFullName))
            {
                log.TriggeredByFullName = TrimToLength(senderEmployee?.FullName, 200);
            }

            var senderProfile = ResolveSenderProfile(setting, template, senderEmployee);
            var fromEmail = senderProfile.Email;
            if (string.IsNullOrWhiteSpace(fromEmail))
            {
                return await FinalizeEmailResultAsync(log, false, "From email is missing");
            }

            log.SenderType = TrimToLength(senderProfile.SenderType, 20) ?? EmailSenderTypes.Hr;
            log.FromEmail = TrimToLength(fromEmail, 200);
            log.FromName = TrimToLength(senderProfile.Name, 200);

            var templateTokens = new Dictionary<string, string>(tokens ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase)
            {
                ["SenderType"] = senderProfile.SenderType,
                ["SenderTypeText"] = EmailSenderTypes.GetDisplayText(senderProfile.SenderType),
                ["SenderName"] = senderProfile.Name ?? string.Empty,
                ["SenderEmail"] = fromEmail
            };

            var signatureHtml = ApplyTokens(senderProfile.Signature ?? string.Empty, templateTokens);
            templateTokens["SenderSignature"] = signatureHtml;

            var subject = ApplyTokens(template.Subject, templateTokens);
            var hasSignatureToken = ContainsSenderSignatureToken(template.Body);
            var body = ApplyTokens(template.Body, templateTokens);
            if (!hasSignatureToken)
            {
                body = AppendSignature(body, signatureHtml);
            }

            body = EmailSignatureHtmlNormalizer.NormalizeSignatureHtml(body) ?? string.Empty;
            log.Subject = TrimToLength(subject, 500);
            log.BodyHtml = body;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderProfile.Name ?? string.Empty, fromEmail));
            var envelopeSender = BuildEnvelopeSender(setting, senderProfile);
            if (!string.Equals(envelopeSender.Address, fromEmail, StringComparison.OrdinalIgnoreCase))
            {
                message.ReplyTo.Add(new MailboxAddress(senderProfile.Name ?? string.Empty, fromEmail));
            }
            message.Subject = subject;
            message.MessageId = MimeUtils.GenerateMessageId();
            log.MessageId = TrimToLength(message.MessageId, 255);

            var bodyBuilder = new BodyBuilder();
            bodyBuilder.HtmlBody = InlineLocalImages(bodyBuilder, body);
            message.Body = bodyBuilder.ToMessageBody();

            var usedRecipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddRecipients(message.To, normalizedTo, usedRecipients);

            if (template.CcManager && managerId.HasValue && managerId.Value > 0)
            {
                var manager = await _unitOfWork.EmployeeRep.GetById(managerId.Value);
                var managerEmail = GetPreferredEmail(manager);
                AddRecipients(message.Cc, new[] { managerEmail ?? string.Empty }, usedRecipients);
            }

            AddRecipients(message.Cc, ParseEmails(template.CcEmails), usedRecipients);
            AddRecipients(message.Cc, normalizedCcInput, usedRecipients);
            AddRecipients(message.Bcc, ParseEmails(template.BccEmails), usedRecipients);
            AddRecipients(message.Bcc, normalizedBccInput, usedRecipients);

            log.ToEmails = JoinRecipientAddresses(message.To);
            log.CcEmails = JoinRecipientAddresses(message.Cc);
            log.BccEmails = JoinRecipientAddresses(message.Bcc);

            if (message.To.Count == 0)
            {
                return await FinalizeEmailResultAsync(log, false, "Recipient email is missing");
            }

            try
            {
                using var client = new SmtpClient();
                client.Timeout = 15000;

                var port = setting.SmtpPort > 0 ? setting.SmtpPort : 25;
                var socketOptions = ResolveSocketOptions(setting);
                await client.ConnectAsync(setting.SmtpHost, port, socketOptions);

                if (!string.IsNullOrWhiteSpace(setting.SmtpUser))
                {
                    await client.AuthenticateAsync(setting.SmtpUser, setting.SmtpPassword ?? string.Empty);
                }

                var recipients = message.To.Mailboxes
                    .Concat(message.Cc.Mailboxes)
                    .Concat(message.Bcc.Mailboxes)
                    .ToList();

                await client.SendAsync(message, envelopeSender, recipients);
                await client.DisconnectAsync(true);
            }
            catch (MailKit.Security.AuthenticationException ex)
            {
                var error = BuildError("SMTP authentication error", ex);
                return await FinalizeEmailResultAsync(log, error.Success, error.Error);
            }
            catch (MailKit.Net.Smtp.SmtpCommandException ex)
            {
                var error = BuildError($"SMTP command error ({ex.StatusCode})", ex);
                return await FinalizeEmailResultAsync(log, error.Success, error.Error);
            }
            catch (MailKit.Net.Smtp.SmtpProtocolException ex)
            {
                var error = BuildError("SMTP protocol error", ex);
                return await FinalizeEmailResultAsync(log, error.Success, error.Error);
            }
            catch (Exception ex)
            {
                var error = BuildError(ex.GetType().Name, ex);
                return await FinalizeEmailResultAsync(log, error.Success, error.Error);
            }

            return await FinalizeEmailResultAsync(log, true, null);
        }

        private static IEnumerable<string> NormalizeEmails(IEnumerable<string>? emails)
        {
            if (emails == null)
            {
                yield break;
            }

            foreach (var item in emails)
            {
                foreach (var email in ParseEmails(item))
                {
                    yield return email;
                }
            }
        }

        private static void AddRecipients(InternetAddressList collection, IEnumerable<string> emails, HashSet<string> usedRecipients)
        {
            foreach (var email in emails)
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    continue;
                }

                var trimmed = email.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                if (MailboxAddress.TryParse(trimmed, out var mailbox) && usedRecipients.Add(mailbox.Address))
                {
                    collection.Add(mailbox);
                }
            }
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
                var value = item.Value ?? string.Empty;
                result = result.Replace("{{" + item.Key + "}}", value, StringComparison.OrdinalIgnoreCase);
                result = result.Replace("{{ " + item.Key + " }}", value, StringComparison.OrdinalIgnoreCase);
                result = result.Replace("{" + item.Key + "}", value, StringComparison.OrdinalIgnoreCase);
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

        private static bool ContainsSenderSignatureToken(string? templateBody)
        {
            if (string.IsNullOrWhiteSpace(templateBody))
            {
                return false;
            }

            return templateBody.Contains("{{SenderSignature}}", StringComparison.OrdinalIgnoreCase)
                || templateBody.Contains("{{ SenderSignature }}", StringComparison.OrdinalIgnoreCase)
                || templateBody.Contains("{SenderSignature}", StringComparison.OrdinalIgnoreCase);
        }

        private static string AppendSignature(string body, string? signatureHtml)
        {
            if (string.IsNullOrWhiteSpace(signatureHtml))
            {
                return body ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                return signatureHtml;
            }

            return $"{body}<br/><br/>{signatureHtml}";
        }

        private string InlineLocalImages(BodyBuilder bodyBuilder, string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return html ?? string.Empty;
            }

            var webRootPath = GetWebRootPath();
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                return html;
            }

            var normalizedWebRoot = Path.GetFullPath(webRootPath);
            var contentIdsByPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var rewrittenHtml = ImageSourceRegex.Replace(html, match =>
            {
                var originalSource = WebUtility.HtmlDecode(match.Groups["src"].Value ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(originalSource))
                {
                    return match.Value;
                }

                if (originalSource.StartsWith("cid:", StringComparison.OrdinalIgnoreCase))
                {
                    return match.Value;
                }

                if (originalSource.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    if (!contentIdsByPath.TryGetValue(originalSource, out var inlineContentId))
                    {
                        if (!TryAddDataUriLinkedResource(bodyBuilder, originalSource, out inlineContentId))
                        {
                            return match.Value;
                        }

                        contentIdsByPath[originalSource] = inlineContentId;
                    }

                    return match.Value.Replace(match.Groups["src"].Value, $"cid:{inlineContentId}", StringComparison.Ordinal);
                }

                if (Uri.TryCreate(originalSource, UriKind.Absolute, out var absoluteUri)
                    && !absoluteUri.IsFile)
                {
                    return match.Value;
                }

                var imagePath = ResolveImagePath(originalSource, normalizedWebRoot);
                if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                {
                    return match.Value;
                }

                if (!contentIdsByPath.TryGetValue(imagePath, out var contentId))
                {
                    var resource = bodyBuilder.LinkedResources.Add(imagePath);
                    contentId = MimeUtils.GenerateMessageId();
                    resource.ContentId = contentId;
                    resource.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
                    contentIdsByPath[imagePath] = contentId;
                }

                return match.Value.Replace(match.Groups["src"].Value, $"cid:{contentId}", StringComparison.Ordinal);
            });

            return rewrittenHtml;
        }

        private static bool TryAddDataUriLinkedResource(BodyBuilder bodyBuilder, string source, out string contentId)
        {
            contentId = string.Empty;
            var separatorIndex = source.IndexOf(',');
            if (separatorIndex <= 5)
            {
                return false;
            }

            var header = source.Substring(5, separatorIndex - 5);
            if (!header.Contains(";base64", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var mediaType = header.Split(';', 2)[0].Trim();
            if (!mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(source.Substring(separatorIndex + 1));
            }
            catch
            {
                return false;
            }

            var fileName = $"inline_{Guid.NewGuid():N}{GetImageExtension(mediaType)}";
            var resource = bodyBuilder.LinkedResources.Add(fileName, bytes);
            contentId = MimeUtils.GenerateMessageId();
            resource.ContentId = contentId;
            resource.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
            return true;
        }

        private static string GetImageExtension(string mediaType)
        {
            return mediaType.ToLowerInvariant() switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/bmp" => ".bmp",
                "image/svg+xml" => ".svg",
                _ => ".bin"
            };
        }

        private string? ResolveImagePath(string source, string normalizedWebRoot)
        {
            if (Uri.TryCreate(source, UriKind.Absolute, out var absoluteUri))
            {
                if (!absoluteUri.IsFile)
                {
                    return null;
                }

                var filePath = absoluteUri.LocalPath;
                return File.Exists(filePath) ? filePath : null;
            }

            var normalizedSource = source.Trim();
            var separatorIndex = normalizedSource.IndexOfAny(new[] { '?', '#' });
            if (separatorIndex >= 0)
            {
                normalizedSource = normalizedSource.Substring(0, separatorIndex);
            }

            if (normalizedSource.StartsWith("~/", StringComparison.Ordinal))
            {
                normalizedSource = normalizedSource.Substring(2);
            }
            else if (normalizedSource.StartsWith("/", StringComparison.Ordinal) || normalizedSource.StartsWith("\\", StringComparison.Ordinal))
            {
                normalizedSource = normalizedSource.Substring(1);
            }

            if (string.IsNullOrWhiteSpace(normalizedSource))
            {
                return null;
            }

            var relativePath = normalizedSource
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            var fullPath = Path.GetFullPath(Path.Combine(normalizedWebRoot, relativePath));
            if (!fullPath.StartsWith(normalizedWebRoot, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return fullPath;
        }

        private string? GetWebRootPath()
        {
            if (string.IsNullOrWhiteSpace(_hostEnvironment.ContentRootPath))
            {
                return null;
            }

            var webRootPath = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot");
            return Directory.Exists(webRootPath) ? webRootPath : null;
        }

        private static EmailSenderProfile ResolveSenderProfile(EmailSetting setting, EmailTemplate template, Employee? senderEmployee)
        {
            var fallbackProfile = ResolveSenderProfile(setting, template);
            if (senderEmployee == null || senderEmployee.Id <= 0)
            {
                return fallbackProfile;
            }

            var senderEmail = GetPreferredEmail(senderEmployee);
            if (string.IsNullOrWhiteSpace(senderEmail))
            {
                return fallbackProfile;
            }

            return new EmailSenderProfile
            {
                SenderType = fallbackProfile.SenderType,
                Email = senderEmail,
                Name = FirstNonEmpty(fallbackProfile.Name, senderEmployee.FullName, senderEmployee.UserName),
                Signature = EmailSignatureHtmlNormalizer.NormalizeSignatureHtml(
                    FirstNonEmpty(senderEmployee.MailSignature, fallbackProfile.Signature))
            };
        }

        private static EmailSenderProfile ResolveSenderProfile(EmailSetting setting, EmailTemplate template)
        {
            var hrProfile = new EmailSenderProfile
            {
                SenderType = EmailSenderTypes.Hr,
                Email = FirstValidEmail(setting.HrFromEmail, setting.FromEmail, setting.SmtpUser),
                Name = FirstNonEmpty(setting.HrFromName, setting.FromName),
                Signature = FirstNonEmpty(setting.HrSignature)
            };

            var employeeProfile = new EmailSenderProfile
            {
                SenderType = EmailSenderTypes.Employee,
                Email = FirstValidEmail(setting.EmployeeFromEmail, setting.FromEmail, setting.HrFromEmail, setting.SmtpUser),
                Name = FirstNonEmpty(setting.EmployeeFromName, setting.FromName, setting.HrFromName),
                Signature = FirstNonEmpty(setting.EmployeeSignature, setting.HrSignature)
            };

            var requestedSenderType = EmailSenderTypes.Normalize(template.SenderType);
            var selectedProfile = requestedSenderType == EmailSenderTypes.Employee ? employeeProfile : hrProfile;
            var fallbackProfile = requestedSenderType == EmailSenderTypes.Employee ? hrProfile : employeeProfile;

            return new EmailSenderProfile
            {
                SenderType = requestedSenderType,
                Email = FirstValidEmail(selectedProfile.Email, fallbackProfile.Email, setting.FromEmail, setting.SmtpUser),
                Name = FirstNonEmpty(selectedProfile.Name, fallbackProfile.Name, setting.FromName),
                Signature = EmailSignatureHtmlNormalizer.NormalizeSignatureHtml(
                    FirstNonEmpty(selectedProfile.Signature, fallbackProfile.Signature))
            };
        }

        private async Task<Employee?> ResolveSenderEmployeeAsync(int? senderEmployeeId)
        {
            var effectiveUserId = senderEmployeeId.GetValueOrDefault();
            if (effectiveUserId <= 0)
            {
                effectiveUserId = ResolveCurrentUserId();
            }

            if (effectiveUserId <= 0)
            {
                return null;
            }

            var employee = await _unitOfWork.EmployeeRep.GetById(effectiveUserId);
            return employee != null && employee.Id > 0 ? employee : null;
        }

        private int ResolveCurrentUserId()
        {
            var identity = _httpContextAccessor.HttpContext?.User?.Identity as ClaimsIdentity;
            var userIdText = identity?.Claims.FirstOrDefault(x => x.Type == "userId")?.Value;
            return int.TryParse(userIdText, out var userId) ? userId : 0;
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return null;
        }

        private static string? FirstValidEmail(params string?[] values)
        {
            foreach (var value in values)
            {
                var normalized = NormalizeEmail(value);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    return normalized;
                }
            }

            return null;
        }

        private static MailboxAddress BuildEnvelopeSender(EmailSetting setting, EmailSenderProfile senderProfile)
        {
            var envelopeEmail = FirstValidEmail(setting.SmtpUser, setting.FromEmail, setting.HrFromEmail, senderProfile.Email)
                ?? senderProfile.Email
                ?? string.Empty;

            return new MailboxAddress(string.Empty, envelopeEmail);
        }

        private static SecureSocketOptions ResolveSocketOptions(EmailSetting setting)
        {
            if (!setting.EnableSsl)
            {
                return SecureSocketOptions.None;
            }

            if (setting.SmtpPort == 465)
            {
                return SecureSocketOptions.SslOnConnect;
            }

            return SecureSocketOptions.StartTls;
        }

        private EmailSentLog CreateEmailSentLog(string templateCode, IEnumerable<string> toEmails, IEnumerable<string> ccEmails, IEnumerable<string> bccEmails, int? managerId, int? senderEmployeeId)
        {
            var (userId, userName, fullName) = ResolveCurrentUserSnapshot();
            var effectiveUserId = senderEmployeeId.GetValueOrDefault() > 0 ? senderEmployeeId.Value : userId;

            return new EmailSentLog
            {
                TemplateCode = TrimToLength(templateCode, 50),
                ToEmails = JoinEmails(toEmails),
                CcEmails = JoinEmails(ccEmails),
                BccEmails = JoinEmails(bccEmails),
                TriggeredByUserId = effectiveUserId > 0 ? effectiveUserId : null,
                TriggeredByUserName = TrimToLength(userName, 200),
                TriggeredByFullName = TrimToLength(fullName, 200),
                SenderEmployeeId = senderEmployeeId.GetValueOrDefault() > 0 ? senderEmployeeId : (effectiveUserId > 0 ? effectiveUserId : null),
                ManagerId = managerId,
                CreatedBy = effectiveUserId > 0 ? effectiveUserId : 0,
                UpdatedBy = effectiveUserId > 0 ? effectiveUserId : 0,
                IsActive = 1
            };
        }

        private async Task<(bool Success, string? Error)> FinalizeEmailResultAsync(EmailSentLog log, bool success, string? error)
        {
            await TryPersistEmailLogAsync(log, success, error);
            return (success, error);
        }

        private async Task TryPersistEmailLogAsync(EmailSentLog? log, bool success, string? error)
        {
            if (log == null)
            {
                return;
            }

            try
            {
                log.SendSuccess = success;
                log.ErrorMessage = error;
                if (log.TriggeredByUserId.GetValueOrDefault() > 0)
                {
                    if (log.CreatedBy <= 0)
                    {
                        log.CreatedBy = log.TriggeredByUserId.Value;
                    }

                    log.UpdatedBy = log.TriggeredByUserId.Value;
                }

                await _unitOfWork.EmailSentLogRep.InsertAsync(log);
            }
            catch
            {
            }
        }

        private static (bool Success, string? Error) BuildError(string prefix, Exception ex)
        {
            var errorMessage = $"{prefix}: {ex.Message}";
            if (!string.IsNullOrWhiteSpace(ex.InnerException?.Message))
            {
                errorMessage += $" | Inner: {ex.InnerException.Message}";
            }

            return (false, errorMessage);
        }

        private static string? GetPreferredEmail(Employee employee)
        {
            if (employee == null || employee.Id <= 0)
            {
                return null;
            }

            var companyEmail = NormalizeEmail(employee.Email);
            if (!string.IsNullOrWhiteSpace(companyEmail))
            {
                return companyEmail;
            }

            var personalEmail = NormalizeEmail(employee.PersonalEmail);
            if (!string.IsNullOrWhiteSpace(personalEmail))
            {
                return personalEmail;
            }

            return null;
        }

        private static string? NormalizeEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var trimmed = email.Trim();
            return MailboxAddress.TryParse(trimmed, out var mailbox) ? mailbox.Address : null;
        }

        private (int UserId, string? UserName, string? FullName) ResolveCurrentUserSnapshot()
        {
            var identity = _httpContextAccessor.HttpContext?.User?.Identity as ClaimsIdentity;
            var userIdText = identity?.Claims.FirstOrDefault(x => x.Type == "userId")?.Value;
            var userName = identity?.Claims.FirstOrDefault(x => x.Type == "UserName")?.Value;
            var fullName = identity?.Claims.FirstOrDefault(x => x.Type == "FullName")?.Value;
            var userId = int.TryParse(userIdText, out var parsedUserId) ? parsedUserId : 0;
            return (userId, userName, fullName);
        }

        private static string JoinEmails(IEnumerable<string>? emails)
        {
            if (emails == null)
            {
                return string.Empty;
            }

            return string.Join("; ", emails
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase));
        }

        private static string JoinRecipientAddresses(InternetAddressList addresses)
        {
            return string.Join("; ", addresses.Mailboxes.Select(x => x.Address));
        }

        private static string? TrimToLength(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed.Substring(0, maxLength);
        }

        private sealed class EmailSenderProfile
        {
            public string SenderType { get; set; } = EmailSenderTypes.Hr;
            public string? Email { get; set; }
            public string? Name { get; set; }
            public string? Signature { get; set; }
        }
    }
}
