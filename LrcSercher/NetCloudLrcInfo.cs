using mshtml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.ObjectModel;
using Afar.Cloud163;

namespace AddIn.LrcSercher
{
    public class NetCloudLyricsList : ILyricsSercher,IDisposable
    {
        private readonly string DownloadPath = "http://music.163.com/api/song/lyric?os=pc&id={0}&lv={1}&kv={2}&tv={3}";
        //private readonly string SercherMusicIdPath = "http://music.163.com/#/search/m/?s={0}&type=1";
        public ObservableCollection<ILrcSercherInfoItem> SercherResultList { get; }

        public string SercherName { get; private set; }
        /// <summary>
        /// 初始化网易云LRC的搜索
        /// </summary>
        /// <param name="delytime">指定开始搜索前的延时，用于加载Iffram元素</param>
        public NetCloudLyricsList( double delytime)
        {
            SercherResultList = new ObservableCollection<ILrcSercherInfoItem>();
        }
        /// <summary>
        /// 获取网易云音乐的歌曲Id集合，并返回到SercherResultList集合中
        /// </summary>
        /// <param name="SercherKeys">通过歌曲名在网络上搜索Id</param>
        /// <param name="Id">如果已经拥有歌曲ID</param>
        /// <returns></returns>
        private void GetMusicIds(string SercherKeys, string Id = null)
        {
            var CloudMusicApi= new NeteaseMusicAPI();
            var res = CloudMusicApi.Search(SercherKeys, type: NeteaseMusicAPI.SearchType.Song).Result;
            if (res.Songs== null) return;
            var songs=res.Songs.Select(x => {
                return new NetCloudLrcInfoz
                {
                    Artist = string.Join(",", x.Ar.Select(y => y.Name)),
                    Id = x.Id.ToString(),
                    Title = x.Name,
                    LrcUri = GetLrcUri(x.Id.ToString())
                };
            });
            foreach (var i in songs) {
                   SercherResultList.Add(i);
               }
        }
        private string GetLrcUri(string MusicId, int lv = -1, int kv = -1, int tv = -1)
        {
            return string.Format(DownloadPath, MusicId, lv, kv, tv);
        }
        /// <summary>
        /// 异步返回lrcinfo集合
        /// </summary>
        /// <param name="artist"></param>
        /// <param name="title"></param>
        /// <param name="filesavepath"></param>
        /// <param name="misicid"></param>
        /// <returns></returns>
        public ObservableCollection<ILrcSercherInfoItem> GetLrcInfoSercherList(string artist, string title, string filesavepath, string misicid = null)
        {
            SercherResultList.Clear();
            SercherName = title;
            if (misicid != null)
            {
                SercherResultList.Add(
                    new LrcSercher.NetCloudLrcInfoz()
                    {
                        Id = misicid,
                        Title = title,
                        LrcUri = GetLrcUri(misicid),
                        Artist = artist,
                        FilePath = filesavepath
                    });
            }
            else
            {
                try
                {
                    GetMusicIds(artist + " - " + title);
                }
                catch (WebException ex) {
                    MessageBox.Show(ex.Message);
                }
                }
            return SercherResultList;
        }

        public void Dispose()
        {
            
        }
    }

    public class NetCloudLrcInfoz:ILrcSercherInfoItem
    {
        public string Artist
        {
            get;set;
        }

        public string FilePath
        {
            get;set;
        }

        public string Id
        {
            get;set;
        }

        public string LrcUri
        {
            get;set;
        }

        public string Title
        {
            get;set;
        }

        public bool DownloadLrc(string filepath = null)
        {
            //下载路径
            if (filepath == null) filepath = FilePath;
            //创建请求
            WebRequest request = WebRequest.Create(LrcUri);
            request.Headers.Add("Cookie", "appver=1.5.0.75771");
            ((HttpWebRequest)request).Referer = "http://music.163.com/";
            ((HttpWebRequest)request).ContentType = "application/x-www-form-urlencoded";
            //下载
            StringBuilder sb = new StringBuilder();
            try
            {
                using (StreamReader sr = new StreamReader(request.GetResponse().GetResponseStream(), Encoding.UTF8))
                {
                    using (StreamWriter sw = new StreamWriter(filepath, false, Encoding.UTF8))
                    {
                        string text = sr.ReadToEnd();
                        var js = new System.Web.Script.Serialization.JavaScriptSerializer();
                        var jarrSong = js.Deserialize<Dictionary<string, object>>(text);
                        if (jarrSong.ContainsKey("lrc")&& ((Dictionary<string, object>)jarrSong["lrc"]).ContainsKey("lyric"))
                        {
                            text = (string)((Dictionary<string, object>)jarrSong["lrc"])["lyric"] + "\r\n";
                            if (jarrSong.ContainsKey("tlyric")) {
                                var gys = ((Dictionary<string, object>)jarrSong["tlyric"]);
                                if (gys.ContainsKey("lyric")) {
                                    text += (string)((Dictionary<string, object>)jarrSong["tlyric"])["lyric"];
                                }
                            }
                        }
                        else
                            text = "可惜哦~没有歌词";
                        sw.Write(text);//.Replace("\\n","\r\n")
                    }
                }
                return true;
            }
            catch (WebException ex)
            {
                File.WriteAllText(filepath, ex.Message); 
            }
            return false;

            //HttpWebResponse response2 = (HttpWebResponse)request.GetResponse();
            //StreamReader sr2 = new StreamReader(response2.GetResponseStream(), Encoding.UTF8);
            //string text2 = sr2.ReadToEnd();
            //return text2;

        }
    }
}
