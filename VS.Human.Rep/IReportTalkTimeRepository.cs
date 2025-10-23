using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IReportTalkTimeRepository
    {

        Task<int> Add(ReportTalkTime entity);
        Task<ReportTalkTimeReponse> GetALl(ReportTalkTimeRequest request);
        Task<List<ReportTalkTimeIndexModel>> GetAllDeleted();
        Task<int> DeleteAll();
        Task<int> UpdateFileDeleted(string filePath);
        Task<int> DeleteAllRangeFromTo(DateTime from, DateTime to);
        Task<DateTime?> GetMaxLinked(string type);
        Task<IEnumerable<ReportQuerryNewTaltimeIndex>> HandlelFileRecording(HandlelFileRecordingRequest request);
        Task<IEnumerable<ReportQuerryNewTaltimeIndex>> ProcessingCal(HandlelFileRecordingRequest request);
        public Task<IEnumerable<ReportQuerryNewTaltimeIndex>> HandlelFileRecordingServeAutoCall(HandlelFileRecordingRequest request);
        Task<IEnumerable<ReportQuerryNewTaltimeIndex>> HandlelFileRecordingServe3(HandlelFileRecordingRequest request);
        Task<IEnumerable<ReportQuerryNewTaltimeIndex>> HandlelFileRecordingServe4(HandlelFileRecordingRequest request);
        Task<ReportQuerryRecordingFileIndex> GetInfomationRecording(string likiedId);


    }
}
