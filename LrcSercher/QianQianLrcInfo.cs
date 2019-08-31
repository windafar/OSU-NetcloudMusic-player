using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AddIn.LrcSercher
{
    public class QianQianLyricsList : ILyricsSercher
    {
        //歌词Id获取地址(Lrc列表地址)
       readonly string SearchPath = "http://ttlrcct2.qianqian.com/dll/lyricsvr.dll?sh?Artist={0}&Title={1}&Flags=0";

        string sercherName;
        public string SercherName
        {
            get
            {
                return sercherName;
            }
        }

        ObservableCollection<ILrcSercherInfoItem> sercherResultList;
        public ObservableCollection<ILrcSercherInfoItem> SercherResultList
        {
            get
            {
                return sercherResultList;
            }
        }

        public QianQianLyricsList() { }

        //把字符串转换为十六进制
        public string GetHexString(string str, Encoding encoding)
        {
            StringBuilder sb = new StringBuilder();
            byte[] bytes = encoding.GetBytes(str);
            foreach (byte b in bytes)
            {
                sb.Append(b.ToString("X").PadLeft(2, '0'));
            }
            return sb.ToString();
        }
        /// <summary>
        /// 从网络中获取歌词列表
        /// </summary>
        /// <param name="artist">艺术家</param>
        /// <param name="title">标题</param>
        /// <param name="filepath">保存文件路径</param>
        /// <param name="musicId">用于直接从网易云音乐Id获取Lrc</param>
        /// <returns></returns>
        ObservableCollection<ILrcSercherInfoItem> ILyricsSercher.GetLrcInfoSercherList(string artist, string title, string filesavepath, string misicid)
        {
            string artistHex = GetHexString(artist, Encoding.Unicode);
            string titleHex = GetHexString(title, Encoding.Unicode);

            string resultUrl = string.Format(SearchPath, artistHex, titleHex);

            XmlDocument doc = new XmlDocument();
            try
            {
                try
                {
                    doc.Load(resultUrl);
                }
                catch (WebException LinkFailExc)
                {
                    System.Windows.Forms.MessageBox.Show("千千静听歌词服务器连接失败:" + LinkFailExc.Message);
                    return null;
                }
                XmlNodeList nodelist = doc.SelectNodes("/result/lrc");
                ObservableCollection<ILrcSercherInfoItem> lrclist = new ObservableCollection<ILrcSercherInfoItem>();
                foreach (XmlNode node in nodelist)
                {
                    XmlElement element = (XmlElement)node;
                    string artistItem = element.GetAttribute("artist");
                    string titleItem = element.GetAttribute("title");
                    string idItem = element.GetAttribute("id");
                    lrclist.Add(new QianQianLrcInfo(idItem, titleItem, artistItem,filesavepath));
                }
                sercherResultList = lrclist;
                sercherName = title;
                return lrclist;
            }
            catch (XmlException)
            {
                return null;
            }

        }
    }

    public class QianQianLrcInfo : ILrcSercherInfoItem
    {
        //歌词下载地址
        private static readonly string DownloadPath = "http://ttlrcct2.qianqian.com/dll/lyricsvr.dll?dl?Id={0}&Code={1}";

        public string FilePath { get; set; }
        public string Id { get; set; }
        public string Artist { get; set; }
        public string Title { get; set; }
        public string LrcUri { get; set; }

        public QianQianLrcInfo(string id, string title, string artist, string filesavepath)
        {
            this.FilePath = filesavepath;
            this.Id = id.Trim();
            this.Title = title;
            this.Artist = artist;
            //算出歌词的下载地址
            this.LrcUri = string.Format(DownloadPath, Id, CreateQianQianCode());
        }
        public bool DownloadLrc( string filepath = null)
        {
            if (filepath == null) filepath = FilePath;
            WebRequest request = WebRequest.Create(LrcUri);

            StringBuilder sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(request.GetResponse().GetResponseStream(), Encoding.UTF8))
                {
                    using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.UTF8))
                    {
                        sw.Write(sr.ReadToEnd());
                    }
                }
                return true;
            }
            catch (WebException)
            {

            }
            return false;
        }


        private string CreateQianQianCode()
        {
            int lrcId = Convert.ToInt32(Id);
            string qqHexStr =(new QianQianLyricsList()).GetHexString(Artist + Title, Encoding.UTF8);
            int length = qqHexStr.Length / 2;
            int[] song = new int[length];
            for (int i = 0; i < length; i++)
            {
                song[i] = int.Parse(qqHexStr.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
            }
            int t1 = 0, t2 = 0, t3 = 0;
            t1 = (lrcId & 0x0000FF00) >> 8;
            if ((lrcId & 0x00FF0000) == 0)
            {
                t3 = 0x000000FF & ~t1;
            }
            else
            {
                t3 = 0x000000FF & ((lrcId & 0x00FF0000) >> 16);
            }

            t3 = t3 | ((0x000000FF & lrcId) << 8);
            t3 = t3 << 8;
            t3 = t3 | (0x000000FF & t1);
            t3 = t3 << 8;
            if ((lrcId & 0xFF000000) == 0)
            {
                t3 = t3 | (0x000000FF & (~lrcId));
            }
            else
            {
                t3 = t3 | (0x000000FF & (lrcId >> 24));
            }

            int j = length - 1;
            while (j >= 0)
            {
                int c = song[j];
                if (c >= 0x80) c = c - 0x100;

                t1 = (int)((c + t2) & 0x00000000FFFFFFFF);
                t2 = (int)((t2 << (j % 2 + 4)) & 0x00000000FFFFFFFF);
                t2 = (int)((t1 + t2) & 0x00000000FFFFFFFF);
                j -= 1;
            }
            j = 0;
            t1 = 0;
            while (j <= length - 1)
            {
                int c = song[j];
                if (c >= 128) c = c - 256;
                int t4 = (int)((c + t1) & 0x00000000FFFFFFFF);
                t1 = (int)((t1 << (j % 2 + 3)) & 0x00000000FFFFFFFF);
                t1 = (int)((t1 + t4) & 0x00000000FFFFFFFF);
                j += 1;
            }

            int t5 = (int)Conv(t2 ^ t3);
            t5 = (int)Conv(t5 + (t1 | lrcId));
            t5 = (int)Conv(t5 * (t1 | t3));
            t5 = (int)Conv(t5 * (t2 ^ lrcId));

            long t6 = (long)t5;
            if (t6 > 2147483648)
                t5 = (int)(t6 - 4294967296);
            return t5.ToString();
        }
        private long Conv(int i)
        {
            long r = i % 4294967296;
            if (i >= 0 && r > 2147483648)
                r = r - 4294967296;

            if (i < 0 && r < 2147483648)
                r = r + 4294967296;
            return r;
        }

    }
}
