using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AddIn.LrcSercher;
using System.IO;
using System.Text.RegularExpressions;

namespace AddIn.LrcSercher
{
   public class LocalLyricsList : ILyricsSercher
    {
        string sercherName;
        public string SercherName
        {
            get
            {
                return sercherName;
            }
        }
        /// <summary>
        /// 初始化本地搜索对象
        /// </summary>
        /// <param name="localLrcFileSercherPath">搜索的文件夹</param>
        public LocalLyricsList(string localLrcFileSercherPath)
        {
            this.localLrcFileSercherPath = localLrcFileSercherPath;
            sercherResultList = new ObservableCollection<ILrcSercherInfoItem>();
        }

        ObservableCollection<ILrcSercherInfoItem> sercherResultList;
        public ObservableCollection<ILrcSercherInfoItem> SercherResultList
        {
            get
            {
                return sercherResultList;
            }
        }

        /// <summary>
        /// 用指定字符串搜索目录中的.lrc文件
        /// </summary>
        /// <param name="dirpath">文件夹路径</param>
        /// <param name="sercherStr">匹配字符</param>
        /// <returns></returns>
        public List<string> US_FileByDir(string dirpath, string sercherStr)
        {
            if (dirpath == null) return new List<string>();
            string[] filenames = Directory.GetFiles(dirpath, "*.lrc");
            string pattern = sercherStr;
            List<string> listtemp = new List<string>();
            foreach (string filepath in filenames)
            {
                MatchCollection mc;
                try
                {
                    mc = System.Text.RegularExpressions.Regex.Matches(filepath, pattern, RegexOptions.IgnoreCase);
                }
                catch (Exception)
                {
                    return listtemp;
                }
                if (mc.Count != 0)
                    foreach (Match m in mc)
                        listtemp.Add(filepath);
            }
            if (listtemp.Count != 0)
                return listtemp;
            else
            {//通过颠倒歌曲名和艺术家重新查找

                string[] pT = pattern.Split('-');
                if (pT.Count() == 1) return null;
                int inser = pT[1].IndexOf('(');
                if (inser != -1)
                    pattern = pT[1].Remove(inser);//移出括号
                else pattern = pT[1];
                pattern = pattern.Trim();//移出空白
                pattern=pattern.Replace('*', ' ');
                foreach (string filepath in filenames)
                {
                    MatchCollection mc = System.Text.RegularExpressions.Regex.Matches(filepath, pattern, RegexOptions.IgnoreCase);
                    if (mc.Count != 0)
                        foreach (Match m in mc)
                        {
                            if (listtemp.IndexOf(filepath) == -1)
                                listtemp.Add(filepath);
                        }
                }
                if (listtemp.Count != 0)
                    return listtemp;
                else
                {
                    int inser2 = pT[0].IndexOf('(');
                    if (inser2 != -1)
                        pattern = pT[0].Remove(inser2);//移出括号
                    else pattern = pT[0];
                    pattern = pattern.Trim();//移出空白
                    foreach (string filepath in filenames)
                    {
                        MatchCollection mc = System.Text.RegularExpressions.Regex.Matches(filepath, pattern, RegexOptions.IgnoreCase);
                        if (mc.Count != 0)
                            foreach (Match m in mc)
                            {
                                if (listtemp.IndexOf(filepath) == -1)
                                    listtemp.Add(filepath);
                            }
                    }
                    // if (listtemp.Count != 0)
                    return listtemp;
                }
            }
        }

        string localLrcFileSercherPath;
        public ObservableCollection<ILrcSercherInfoItem> GetLrcInfoSercherList(string artist, string title, string filesavepath, string misicid = null)
        {
            sercherName = artist + "-" + title;
            List<string> list = US_FileByDir(localLrcFileSercherPath, sercherName);
            sercherResultList.Clear();
            foreach (string s in list)
            {
                sercherResultList.Add(new LocalLrcInfo()
                {
                    LrcUri = s,
                    Artist = null,
                    FilePath = s,
                    Title = s
                });
            }
            return sercherResultList;
        }
    }
    class LocalLrcInfo : ILrcSercherInfoItem

    {
        public string Artist
        {
            get;

            set;
        }

        public string FilePath
        {
            get;

            set;
        }

        public string Id
        {
            get;
            set;
        }

        public string LrcUri
        {
            get;

            set;
        }

        public string Title
        {
            get;

            set;
        }

        public bool DownloadLrc(string downLoadPath = null)
        {
            if (this.LrcUri != null)
                return true;
            else return false;
        }
    }
}
