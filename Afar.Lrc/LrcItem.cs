using System;

namespace AddIn.Lrc
{
    /// <summary>
    /// 一句歌词
    /// </summary>
    public class LrcItem
    {
        /// <summary>
        /// 时间
        /// </summary>
        public TimeSpan Time { get; set; }

        /// <summary>
        /// 时间内容
        /// </summary>
        public string TimeString { get; set; }

        /// <summary>
        /// 歌词内容
        /// </summary>
        public string LrcText { get; set; }

        public override string ToString()
        {
            return String.Format("[{0}]{1}", Time.ToString(), LrcText);
        }
    }
}