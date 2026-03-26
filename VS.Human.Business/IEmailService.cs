using System.Collections.Generic;

namespace VS.Human.Business
{
    public interface IEmailService
    {
        Task<bool> SendTemplateAsync(string templateCode, string toEmail, IDictionary<string, string> tokens, int? managerId = null, int? senderEmployeeId = null);
        Task<bool> SendTemplateAsync(string templateCode, IEnumerable<string> toEmails, IDictionary<string, string> tokens, IEnumerable<string>? ccEmails = null, IEnumerable<string>? bccEmails = null, int? managerId = null, int? senderEmployeeId = null);
        Task<(bool Success, string? Error)> SendTemplateWithErrorAsync(string templateCode, IEnumerable<string> toEmails, IDictionary<string, string> tokens, IEnumerable<string>? ccEmails = null, IEnumerable<string>? bccEmails = null, int? managerId = null, int? senderEmployeeId = null);
    }
}
