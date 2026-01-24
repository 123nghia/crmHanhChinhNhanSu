using VS.Human.Rep.Model;

namespace VS.Human.Business
{
    public interface IThemeSettingBusiness
    {
        Task<UserThemeSetting?> GetByUserIdAsync(int userId);
        Task<bool> SaveAsync(UserThemeSetting setting);
    }
}
