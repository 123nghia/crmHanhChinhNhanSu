using System;
using System.Globalization;

namespace VS.Human.Item
{
    public class ContractRequest : BaseRequest
    {
        public int? EmployeeId { get; set; }
        public string? Status { get; set; }
        public string? ContractTypeCode { get; set; }

        public ContractRequest() : base()
        {
            Limit = 20;
        }
    }

    public class ContractIndexModel : BaseIndexModel
    {
        public int EmployeeId { get; set; }
        public string? FullName { get; set; }
        public string? UserName { get; set; }
        public string? ContractTypeCode { get; set; }
        public string? ContractTypeName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        public string? FileUrl { get; set; }
        public string? Note { get; set; }

        public string StartDateDisplay
        {
            get
            {
                if (StartDate.HasValue)
                {
                    return StartDate.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                }
                return string.Empty;
            }
        }

        public string EndDateDisplay
        {
            get
            {
                if (EndDate.HasValue)
                {
                    return EndDate.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
                }
                return string.Empty;
            }
        }
    }

    public class ContractHistoryIndexModel : BaseIndexModel
    {
        public int ContractId { get; set; }
        public int EmployeeId { get; set; }
        public string? Action { get; set; }
        public string? ContractTypeCode { get; set; }
        public string? ContractTypeName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        public string? FileUrl { get; set; }
        public string? Note { get; set; }

        public string CreateAtDisplay
        {
            get
            {
                if (CreateAt == default)
                {
                    return string.Empty;
                }
                return CreateAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            }
        }
    }
}
