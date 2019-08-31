using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AddIn.Lrc
{
    public class LrcInfo
    {
        private LrcInfo()
        {
        }

        public LrcInfo(string pFilePath)
        {
            this.FilePath = pFilePath;
            this.BuildLrcCollection();
        }

        private static readonly IdentifyEncoding ENCODER = new IdentifyEncoding();
        private static readonly Regex REGEX = new Regex(@"^\[(?<time>\d+:.+\d)\](?<txt>.*)", RegexOptions.IgnoreCase);

        #region Properties.....

        /// <summary>
        /// 所在路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 歌曲标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 艺术家
        /// </summary>
        public string Artist { get; set; }

        /// <summary>
        /// 专辑
        /// </summary>
        public string Album { get; set; }

        private LinkedList<LrcItem> _Items = new LinkedList<LrcItem>();
        /// <summary>
        /// 歌曲的歌词列表，按时间排序
        /// </summary>
        public LinkedList<LrcItem> Items
        {
            get { return _Items; }
            set { _Items = value; }
        }

        #endregion Properties.....

        /// <summary>
        /// 根据文件内容，生成 Lrc 对象实例集合
        /// <para>异常：</para>
        /// <para>FileNotFoundException，无法获取指定的歌词文件</para>
        /// </summary>
        private void BuildLrcCollection()
        {
            List<LrcItem> unOrderLrcList = new List<LrcItem>();
            FileInfo f = new FileInfo(FilePath);
            if (f.Exists && f.Length > 10)
            {
                string ec = ENCODER.GetEncodingName(f);
                StreamReader sr = new StreamReader(f.FullName, ENCODER.GetEncoding(ec));
                while (!sr.EndOfStream)
                {
                    string lineString = sr.ReadLine();
                    unOrderLrcList.AddRange(_ParseLine(lineString));
                }
            }
            else if(f.Exists && f.Length < 10)
            {
                throw new FileLoadException("文件长度太短");
            }
            else
            {
                //歌词文件不存在
                throw new FileNotFoundException("未找到歌词文件。");
            }

            this.Items = new LinkedList<LrcItem>(unOrderLrcList.OrderBy(pp => pp.Time.Ticks));
        }

        /// <summary>
        /// 根据内容， 解析并生成Lrc实例
        /// </summary>
        /// <param name="pLrcText">文本内容</param>
        /// <returns></returns>
        public static LrcInfo Parse(string pLrcText)
        {
            LrcInfo result = new LrcInfo();
            List<LrcItem> unOrderLrcList = new List<LrcItem>();

            var textLines = pLrcText.Split('\r', '\n');

            foreach (var iLine in textLines)
            {
                unOrderLrcList.AddRange(result._ParseLine(iLine));
            }

            result.Items = new LinkedList<LrcItem>(unOrderLrcList.OrderBy(pp => pp.Time.Ticks));

            return result;
        }

        /// <summary>
        /// 利用正则表达式解析每行具体语句
        /// 并在解析完该语句后，将解析出来的信息设置在LrcInfo对象中
        /// </summary>
        /// <param name="pStr">一行内容</param>
        /// <returns> 改行转换成的歌词实例 </returns>
        private List<LrcItem> _ParseLine(string pStr)
        {
            List<LrcItem> result = new List<LrcItem>();

            pStr = pStr.Trim();
            if (pStr.StartsWith("["))
            {
                //判断第二个字符为数字，就认为是时间标签
                if (pStr.Length > 2 && Char.IsNumber(pStr, 1))
                {
                    // 通过正则取得每句歌词信息
                    Regex reg = new Regex(@"^\[(?<time>\d+:.+\d)\](?<txt>.*)", RegexOptions.IgnoreCase);
                    Match match = reg.Match(pStr);
                    MatchCollection mc = reg.Matches(pStr);

                    Group gTime = match.Groups[1];  //标签内的时间组
                    Group gText = match.Groups[2];  //歌词主内容

                    #region 将正则结果转换为歌词对象组

                    string[] timeSplit = new string[] { "][" };
                    string[] times = gTime.Value.Split(timeSplit, StringSplitOptions.RemoveEmptyEntries);
                    //获取单个Time
                    foreach (var aTime in times)
                    {
                        //Lrc的三种可用时间【 分钟:秒.毫秒 | 分钟:秒 | 分钟:秒:毫秒 】
                        char[] timeCutSpit = new char[] { ':', '.' };
                        string[] timeCuts = aTime.Split(timeCutSpit, StringSplitOptions.RemoveEmptyEntries);
                        if (timeCuts.Length < 2)
                        {
                            // 时间标签有问题，不符合lrc标准
                        }
                        else
                        {
                            // 解析时间标签
                            LrcItem lrcItem = new LrcItem { TimeString = aTime, LrcText = gText.Value };

                            if (timeCuts.Length == 3)
                            {
                                // 分钟:秒.毫秒 | 分钟:秒:毫秒
                                int lMinute, lSeconds, lMillisecond;
                                Int32.TryParse(timeCuts[0], out lMinute);
                                Int32.TryParse(timeCuts[1], out lSeconds);
                                Int32.TryParse(timeCuts[2], out lMillisecond);

                                lrcItem.Time = new TimeSpan(0, 0, lMinute, lSeconds, lMillisecond);
                                result.Add(lrcItem);
                            }
                            else if (timeCuts.Length == 2)
                            {
                                // 分钟:秒
                                int lMinute, lSeconds;
                                Int32.TryParse(timeCuts[0], out lMinute);
                                Int32.TryParse(timeCuts[1], out lSeconds);

                                lrcItem.Time = new TimeSpan(0, 0, lMinute, lSeconds, 0);
                                result.Add(lrcItem);
                            }
                            else
                            {
                                // 时间标签有3个以上，不符合lrc标准
                            }
                        }
                    }

                    #endregion 将正则结果转换为歌词对象组
                }
                else
                {
                    // 取得歌曲名信息
                    if (pStr.StartsWith("[ti:"))
                    {
                        this.Title = pStr.TrimEnd(']').Substring(4);
                    }// 取得歌手信息
                    else if (pStr.StartsWith("[ar:"))
                    {
                        this.Artist = pStr.TrimEnd(']').Substring(4);
                    }// 取得专辑信息
                    else if (pStr.StartsWith("[al:"))
                    {
                        this.Album = pStr.TrimEnd(']').Substring(4);
                    }
                }
            }
            return result;
        }
    }
}