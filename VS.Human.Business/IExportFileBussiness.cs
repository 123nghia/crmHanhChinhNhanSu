using VS.Human.Item;

namespace VS.Human.Business
{
    public interface IExportFileBussiness
    {
        Task<string> ExportFileDashboard(OrderRequest requestExport);

    }

}
