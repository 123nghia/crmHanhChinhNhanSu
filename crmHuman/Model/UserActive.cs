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
                var itemUser = new UserItem
                {
                    LastUpdated = DateTime.Now,
                    UserId = UserId,
                    UserName = userName,
                    FullName = fullName
                };

                var itemUserData = DataUsser.FirstOrDefault(x => x.UserId == UserId);
                if (itemUserData == null)
                {
                    DataUsser.Add(itemUser);
                }
                else
                {
                    itemUserData.LastUpdated = DateTime.Now;
                }
            }
        }


        public int GetCountUserOnline()
        {
            lock (lockObject)
            {
                var datetiemDiff = DateTime.Now.AddMinutes(-3);
                return DataUsser.Count(x => x.LastUpdated > datetiemDiff);
            }
        }

        public List<UserItem> GetListUser(int UserId)
        {
            lock (lockObject)
            {
                if (UserId == 17)
                    return new List<UserItem>(DataUsser);

                if (UserId == 37)
                {
                    var tempCondition = new List<string> { "41", "48", "49", "50", "51", "37" };
                    return DataUsser
                        .Where(x => x.StatusOnline == "Online" && tempCondition.Contains(x.UserId ?? ""))
                        .ToList();
                }

                if (UserId == 38)
                {
                    var tempCondition = new List<string> { "39", "40", "43", "44", "45", "46", "47", "38" };
                    return DataUsser
                        .Where(x => tempCondition.Contains(x.UserId ?? ""))
                        .ToList();
                }

                return new List<UserItem>();
            }
        }
    }

    public class UserItem
    {
        public string? UserName { get; set; }

        public string? FullName { get; set; }

        public DateTime? LastUpdated { get; set; }

        public string? UserId { get; set; }

        public string StatusOnline
        {
            get
            {
                var datecompare = DateTime.Now.AddMinutes(-3);

                if (LastUpdated >= datecompare)
                {
                    return "Online";
                }
                return "Off";

            }
        }
    }
}