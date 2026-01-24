namespace crmHuman.Model
{
    /// <summary>
    /// Singleton pattern for tracking active users
    /// Thread-safe implementation
    /// </summary>
    public sealed class UserActive
    {
        private static UserActive? instance;
        private static readonly object lockObject = new();
        private readonly List<UserItem> DataUsser;
        internal static readonly TimeSpan OnlineWindow = TimeSpan.FromMinutes(3);

        private UserActive()
        {
            DataUsser = new List<UserItem>();
        }

        public static UserActive DataActiveOnline
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        instance ??= new UserActive();
                    }
                }
                return instance;
            }
        }

        public void AddOrUpdate(string UserId, string userName, string fullName)
        {
            lock (lockObject)
            {
                var itemUserData = GetOrCreateUser(UserId, userName, fullName);
                itemUserData.LastUpdated = DateTime.Now;
                itemUserData.IsOnline = true;
                if (!itemUserData.LastLogin.HasValue)
                {
                    itemUserData.LastLogin = itemUserData.LastUpdated;
                }
            }
        }

        public void MarkLogin(string userId, string? userName, string? fullName)
        {
            lock (lockObject)
            {
                var itemUserData = GetOrCreateUser(userId, userName, fullName);
                var now = DateTime.Now;
                itemUserData.LastLogin = now;
                itemUserData.LastUpdated = now;
                itemUserData.IsOnline = true;
            }
        }

        public void MarkLogout(string userId, string? userName, string? fullName)
        {
            lock (lockObject)
            {
                var itemUserData = GetOrCreateUser(userId, userName, fullName);
                var now = DateTime.Now;
                itemUserData.LastLogout = now;
                itemUserData.LastUpdated = now;
                itemUserData.IsOnline = false;
            }
        }


        public int GetCountUserOnline()
        {
            lock (lockObject)
            {
                var datetiemDiff = DateTime.Now.Subtract(OnlineWindow);
                return DataUsser.Count(x => x.IsOnline && x.LastUpdated.HasValue && x.LastUpdated > datetiemDiff);
            }
        }

        public List<UserItem> GetListUser(string? roleCode, int userId)
        {
            lock (lockObject)
            {
                if (string.IsNullOrEmpty(roleCode) || roleCode == "2")
                {
                    return new List<UserItem>();
                }

                return DataUsser
                    .OrderByDescending(x => x.LastUpdated ?? DateTime.MinValue)
                    .ToList();
            }
        }

        private UserItem GetOrCreateUser(string userId, string? userName, string? fullName)
        {
            var itemUserData = DataUsser.FirstOrDefault(x => x.UserId == userId);
            if (itemUserData == null)
            {
                itemUserData = new UserItem
                {
                    UserId = userId
                };
                DataUsser.Add(itemUserData);
            }

            if (!string.IsNullOrWhiteSpace(userName))
            {
                itemUserData.UserName = userName;
            }

            if (!string.IsNullOrWhiteSpace(fullName))
            {
                itemUserData.FullName = fullName;
            }

            return itemUserData;
        }
    }

    public class UserItem
    {
        public string? UserName { get; set; }

        public string? FullName { get; set; }

        public DateTime? LastUpdated { get; set; }

        public DateTime? LastLogin { get; set; }

        public DateTime? LastLogout { get; set; }

        public string? UserId { get; set; }

        public bool IsOnline { get; set; }

        private bool IsOnlineNow()
        {
            if (!IsOnline || !LastUpdated.HasValue)
            {
                return false;
            }

            var datecompare = DateTime.Now.Subtract(UserActive.OnlineWindow);
            return LastUpdated >= datecompare;
        }

        public string StatusOnline
        {
            get
            {
                return IsOnlineNow() ? "Online" : "Off";
            }
        }
    }
}
