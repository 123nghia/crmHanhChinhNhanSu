using Microsoft.AspNetCore.Http;
using VS.Human.Rep;
using VS.Human.Rep.Model;

namespace VS.Human.Business.Imp
{
    public class ThemeSettingBusiness : BaseBusiness, IThemeSettingBusiness
    {
        public ThemeSettingBusiness(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor)
            : base(unitOfWork, httpContextAccessor)
        {
        }

        public async Task<UserThemeSetting?> GetByUserIdAsync(int userId)
        {
            if (userId < 1)
            {
                return null;
            }

            try
            {
                return await _unitOfWork.UserThemeSettingRep.GetByUserIdAsync(userId);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> SaveAsync(UserThemeSetting setting)
        {
            if (setting == null || setting.UserId < 1)
            {
                return false;
            }

            try
            {
                if (setting.CreatedBy < 1)
                {
                    setting.CreatedBy = setting.UserId;
                }

                if (setting.UpdatedBy < 1)
                {
                    setting.UpdatedBy = setting.UserId;
                }

                return await _unitOfWork.UserThemeSettingRep.SaveAsync(setting);
            }
            catch
            {
                return false;
            }
        }
    }
}
