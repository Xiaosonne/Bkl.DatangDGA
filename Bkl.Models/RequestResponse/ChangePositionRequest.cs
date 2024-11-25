namespace Bkl.Models
{
    public class ChangePositionRequest
    {
        public string OldPosition { get; set; }
        public string NewPosition { get; set; }

        public long[] ChangeTaskDetailIds { get; set; }
    }
}
