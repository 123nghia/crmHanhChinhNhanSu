using VS.Human.Item;

namespace VS.Human.Rep
{
    public interface IReportTalkTimeGroupByDay
    {


        Task<GetAllRecordGroupByLineCodeReponse> ProcessCalReportGroupByDay(GetAllRecordGroupByLineCodeRequest request);
        Task<GetAllRecordGroupByLineCodeReponse> GetAll(GetAllRecordGroupByLineCodeRequest entity);
        Task<GetOverViewDashboardReponse> GetOverViewDashBoard(GetOverViewDashboard entity);
        Task<List<T>> GetDataList<T>(string sql = "",
            object parameter = null)
            where T : class, new();




    }
}
