using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayProjectGame.Data
{
    static class LRCRefClass
    {
        public static Dictionary<string, string> LrcRefDic = null;

    }
    [Serializable]
    public class SongInfo
    {
        public string SongId
        {
            get;
            set;
        }

        string songName;
        public string SongName
        {
            get
            {
                return songName ?? "";
            }
            set
            {
                songName = value;
            }
        }
        private string songArtist;
        public string SongArtist
        {
            get
            {

                return songArtist;
            }
            set
            {
                songArtist = value;
            }
        }
        public string[] GetSongArtists
        {
            get=> SongArtist.Split('/', '&', ',', '、', '(', ')','（','）','，', '・');
        }
        private string songAlbum;
        public string SongAlbum
        {
            get
            {

                return songAlbum;
            }
            set
            {
                songAlbum = value;
            }
        }
        public string SongLengh
        {
            get;
            set;
        }
        private string songPath;
        public string SongPath
        {
            get
            {
                return songPath;
            }
            set { songPath = value; }
        }
        int songty;
        /// <summary>
        /// 1--本地，2--网易云，3--OSU
        /// </summary>
        public int SongType
        {
            get
            {
                return songty;
            }
            set
            {
                songty = value;
            }
        }
        /// <summary>
        /// 该属性用于OSU
        /// </summary>
        public string OsuPath
        { set; get; }

        public string LrcRefPath
        {
            get
            {
                if (LRCRefClass.LrcRefDic == null)
                {
                    if (!File.Exists(GlobalConfigClass.OBJ_LRCDIC_REFPATH))
                    {
                        LRCRefClass.LrcRefDic = new Dictionary<string, string>();
                    }
                    else
                    {
                        var buff = File.ReadAllBytes(GlobalConfigClass.OBJ_LRCDIC_REFPATH);
                        LRCRefClass.LrcRefDic = (Dictionary<string, string>)Helper.OtherHelper.DeserializeObject(buff);
                    }
                }
                if (LRCRefClass.LrcRefDic.ContainsKey(SongId))
                    return LRCRefClass.LrcRefDic[SongId];
                else
                    return null;

            }
            set
            {
                if (LRCRefClass.LrcRefDic != null && SongId != null)
                {
                    LRCRefClass.LrcRefDic[SongId] = value;
                    //byte[] buff = Helper.OtherHelper.SerializeObject(LRCRefClass.LrcRefDic);
                    //File.WriteAllBytes(GlobalConfigClass.XML_LRCDIC_REFPATH, buff);
                }
            }
        }

        public bool LocalFlac { set; get; }

        public bool RemoteFlac { set; get; }

        public List<string> files { set; get; }
    }

    public class SongInfoExpend
    {
        public SongInfo SongInfo { get; set; }
        public int SongInfoIndex { get; set; }
        /// <summary>
        /// 以‘-’格式构,(eg.netcloudId-{id})
        /// </summary>
        public string Source { get; set; }
        public string SourceName { set; get; }
        public long FileTime { set; get; }
        /// <summary>
        /// 指定在跨控件操作时的容器名称
        /// </summary>
        public string DataContainerName { set; get; }
        public string SongNameAndArtist { get => SongInfo.SongName + " - " + SongInfo.SongArtist.Trim(','); }
        public SongInfoExpend()
        {
        }
        public SongInfoExpend(SongInfoExpend songInfoex)
        {
            SongInfo = songInfoex.SongInfo;
            SongInfoIndex = songInfoex.SongInfoIndex;
            Source = songInfoex.Source;
            DataContainerName = songInfoex.DataContainerName;
            FileTime = songInfoex.FileTime;
        }

    }

    public class PlayListData //歌单数据类
    {
        int songty;
        /// <summary>
        /// 1--本地，2--网易云，3--OSU, 4--专辑，5--艺术家
        /// </summary>
        public int PlayListType
        {
            get
            {
                return songty;
            }
            set
            {
                songty = value;
            }
        }
        public string PlatListName
        {
            set;
            get;
        }
        public string PlayListId
        {
            set;
            get;
        }
        public string PlayListNetImageUri
        { get; set; }
        public string UserId
        {
            set;
            get;
        }
        public string UersName
        {
            get;
            set;
        }
        public string PlayListInfo
        {
            get;
            set;
        }
        public List<SongInfoExpend> Songs
        {
            set;
            get;
        }//每个歌单的！歌曲列表！
        public PlayListData()
        {
            Songs = new List<SongInfoExpend>();
        }
    }

    public class UserData //创建者者数据类
    {
        string uid;
        /// <summary>
        /// //本地使用者ID
        /// </summary>
        public string Uid
        {
            get { return uid; }
            set { uid = value; }
        }
        public string Username
        {
            get;
            set;
        }

        List<PlayListData> pids;
        /// <summary>
        /// 该使用者拥有的歌单集合
        /// </summary>
        public List<PlayListData> Pids
        {
            get { return pids; }
            set { pids = value; }
        }

        /// <summary>
        /// 使用一系列的以','分开的多个歌单ID字符串初始化创建者类，该类包含了使用者ID和使用者创建的歌单集合
        /// </summary>
        /// <param name="Uid">使用者ID</param>
        /// <param name="Pids">使用者创建的以'，'分开的歌单字符串集合</param>
        public UserData(string Uid, string Pids)
        {
            this.Uid = Uid;
            this.Pids = new List<PlayListData>();
            string[] platlistsIDs = Pids.Split(',');
            foreach (string platlistsID in platlistsIDs)
            {
                if (platlistsID == null || platlistsID == "") continue;
                this.Pids.Add(new PlayListData() { PlayListId = platlistsID, PlayListType = 2 });//加入播放列表
            }
        }
        public UserData() { }
    }

    public class MainSearchResult
    {
        public string key;
        public List<PlayListData> pld = new List<PlayListData>();
        public List<SongInfoExpend> songs = new List<SongInfoExpend>();
    }
}
