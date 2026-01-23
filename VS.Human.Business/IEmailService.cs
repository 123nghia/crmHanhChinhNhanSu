using System.Collections.Generic;

namespace VS.Human.Business
{
    public interface IEmailService
    {
        Task<bool> SendTemplateAsync(string templateCode, string toEmail, IDictionary<string, string> tokens, int? managerId = null);
    }
}
