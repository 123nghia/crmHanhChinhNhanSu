using System.Collections;

namespace VS.Human.Item
{
    public class BaseList
    {
        public int Total { get; set; }
        public IEnumerable? Data { get; set; }
        public BaseList()
        {
            Data = new List<object>();
        }
    }
}
