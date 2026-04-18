using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using VS.Human.Business;
using VS.Human.Item;
using VS.Human.Rep.Model;

namespace crmHuman.Pages.InternalNews
{
    [Authorize]
    public class DetailModel : BaseModel2
    {
        private readonly IInternalNewsBusiness _newsBusiness;

        public InternalNewsItem NewsItem { get; set; }
        public List<InternalNewsIndexModel> RelatedNews { get; set; } = new List<InternalNewsIndexModel>();
        public bool CanEdit => (Permision.Edit ?? false) || (UserData?.RoleCode == "1");
        public bool CanDelete => (Permision.Delete ?? false) || (UserData?.RoleCode == "1");

        public DetailModel(IInternalNewsBusiness newsBusiness)
        {
            _newsBusiness = newsBusiness;
            TitlePage = "Chi tiết tin nội bộ";
            KeyPage = "InternalNews";
            NewsItem = new InternalNewsItem();
        }

        public async Task<IActionResult> OnGet(int id)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            var canView = (Permision.View ?? false) || (UserData?.RoleCode == "1");
            if (!canView)
            {
                return Redirect("/");
            }

            if (id <= 0)
            {
                return Redirect("/InternalNews");
            }

            NewsItem = await _newsBusiness.GetById(id);
            if (NewsItem == null || NewsItem.Id <= 0)
            {
                return Redirect("/InternalNews");
            }

            await LoadRelatedNews(id);
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Redirect("/Login");
            }

            GetInfoUser();
            if (!CanDelete)
            {
                return Redirect("/InternalNews");
            }

            if (id > 0)
            {
                await _newsBusiness.Delete(id);
            }

            return Redirect("/InternalNews");
        }

        private async Task LoadRelatedNews(int currentId)
        {
            var request = new InternalNewsRequest
            {
                Page = 1,
                Limit = 6
            };

            var latestList = await _newsBusiness.GetAll(request);
            if (latestList?.Data == null)
            {
                return;
            }

            foreach (var item in latestList.Data)
            {
                if (item is InternalNewsIndexModel newsItem && newsItem.Id != currentId)
                {
                    RelatedNews.Add(newsItem);
                }
            }

            if (RelatedNews.Count > 5)
            {
                RelatedNews = RelatedNews.Take(5).ToList();
            }
        }
    }
}
