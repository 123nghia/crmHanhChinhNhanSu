namespace crmHuman.Model
{
    /// <summary>
    /// Singleton pattern for managing group member data
    /// Thread-safe implementation
    /// </summary>
    public sealed class GlobalGroupMember
    {
        private static GlobalGroupMember? instance;
        private static readonly object lockObject = new();

        private readonly List<SelectDisplay> GroupEm;
        private readonly List<SelectDisplay> MemberGroups;

        private GlobalGroupMember()
        {
            GroupEm = new List<SelectDisplay>();
            MemberGroups = new List<SelectDisplay>();
        }

        public static GlobalGroupMember GlobalData
        {
            get
            {
                if (instance == null)
                {
                    lock (lockObject)
                    {
                        instance ??= new GlobalGroupMember();
                    }
                }
                return instance;
            }
        }

        public void AddGroup(SelectDisplay item)
        {
            lock (lockObject)
            {
                var existingItem = GroupEm.FirstOrDefault(x => x.LinkId == item.LinkId);
                if (existingItem != null)
                {
                    // Update existing item properties instead of reassigning temp
                    var index = GroupEm.IndexOf(existingItem);
                    GroupEm[index] = item;
                }
                else
                {
                    GroupEm.Add(item);
                }
            }
        }

        public void AddMember(SelectDisplay item)
        {
            lock (lockObject)
            {
                var existingItem = MemberGroups.FirstOrDefault(x => x.LinkId == item.LinkId);
                if (existingItem != null)
                {
                    var index = MemberGroups.IndexOf(existingItem);
                    MemberGroups[index] = item;
                }
                else
                {
                    MemberGroups.Add(item);
                }
            }
        }

        public List<SelectDisplay> GetAllMemberByGroupId(int groupId)
        {
            lock (lockObject)
            {
                if (groupId == -1)
                    return new List<SelectDisplay>(MemberGroups);

                return MemberGroups.Where(x => x.RelId == groupId).ToList();
            }
        }

        public List<SelectDisplay> GetAllGroup()
        {
            lock (lockObject)
            {
                return new List<SelectDisplay>(GroupEm);
            }
        }
    }
}
