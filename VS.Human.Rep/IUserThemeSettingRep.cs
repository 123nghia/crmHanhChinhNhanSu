using VS.Human.Rep.Model;

namespace VS.Human.Rep
{
    public interface IUserThemeSettingRep
    {
        Task<UserThemeSetting?> GetByUserIdAsync(int userId);
        Task<bool> SaveAsync(UserThemeSetting setting);
    }
}
