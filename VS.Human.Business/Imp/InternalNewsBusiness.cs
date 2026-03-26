using Microsoft.AspNetCore.Http;
using VS.Human.Item;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class InternalNewsBusiness : BaseBusiness, IInternalNewsBusiness
    {
        private const string OldDashboardNoticeText = "Cài đặt hiển thị thông báo mới ngay giao diện màn hình chính khi đăng nhập";
        private const string NewDashboardNoticeText = "Cài đặt hiển thị thông báo mới ngay trên màn hình chính khi đăng nhập";

        public InternalNewsBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {
        }

        public Task<BaseList> GetAll(InternalNewsRequest request)
        {
            return _unitOfWork.InternalNewsRep.GetAll(request);
        }

        public async Task<InternalNewsItem> GetById(int id)
        {
            var item = await _unitOfWork.InternalNewsRep.GetById(id);
            NormalizeContent(item);
            return item;
        }

        public Task<bool> AddOrUpdate(InternalNewsItem item)
        {
            NormalizeContent(item);
            return _unitOfWork.InternalNewsRep.AddOrUpdate(item);
        }

        public Task<bool> Delete(int id)
        {
            return _unitOfWork.InternalNewsRep.Delete(id);
        }

        private static void NormalizeContent(InternalNewsItem? item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Content))
            {
                return;
            }

            item.Content = item.Content.Replace(OldDashboardNoticeText, NewDashboardNoticeText, StringComparison.Ordinal);
        }
    }
}
