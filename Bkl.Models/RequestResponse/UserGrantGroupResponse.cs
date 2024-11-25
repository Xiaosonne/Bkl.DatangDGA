using System.Collections.Generic;

namespace Bkl.Models
{
    public class UserGrantGroupResponse
    {
        public object GroupKey { get; set; }
        public object GroupTitle { get; set; }
        public List<object> Children { get; set; }
    }
}
