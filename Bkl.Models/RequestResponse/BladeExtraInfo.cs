namespace Bkl.Models
{
    public class BladeExtraInfo
    {
        /// <summary>
        /// 叶根
        /// </summary>
        public int StartIndex { get; set; }
        /// <summary>
        /// 叶尖
        /// </summary>
        public int EndIndex { get; set; }
        public int OverLap { get; set; }
        public string Except { get; set; }

        public string Position { get; set; }
        public string Direction { get; set; }
        public string Angle { get; set; }
    }
}
