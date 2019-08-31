using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace PlayProjectGame
{

    public class GlobalConfigClass : INotifyPropertyChanged
    {
        public static string XML_CLOUDMUSIC_SAVEPATH = "CloudMusicSongList.xml";
        public static string XML_OSU_SAVEPATH = "OsuClloSongList.xml";
        public static string OBJ_LRCDIC_REFPATH = "LrcRefDic.dic";
        public static string XML_OSU_AllPATH = "OsuAllSongList.xml";
        /// <summary>
        /// 用户定义的Lrc目录
        /// </summary>
        private string lrcDir;
        /// <summary>
        /// 用户定义的网易云音乐本地下载路径
        /// </summary>
        private string netCloudMusicLocalPath;
        /// <summary>
        /// 本地音乐使用目录
        /// </summary>
        private string localMusicDir;
        /// <summary>
        /// 是否开启水纹动画
        /// </summary>
        private bool isWaterAdmin;
        /// <summary>
        /// 是否从网易云音乐网站获取歌单图片
        /// </summary>
        private bool isPlayListPicFromNet;
        /// <summary>
        /// 决定加入播放列表的方式--“1,next”OR“0,last”
        /// </summary>
        private int addIndex;
        /// <summary>
        /// 是否自动加载歌词
        /// </summary>
        private bool isAtuoLrc;
        /// <summary>
        /// 指定显示翻译还是当前语言（暂时）
        /// </summary>
        private bool lrcMode=false;
        /// <summary>
        /// LRC背景色
        /// </summary>
        private string lrcBkColor="#aaffffff";
        /// <summary>
        /// 定义Osu程序的目录
        /// </summary>
        private string osuDir;
        /// <summary>
        /// 网易云音乐的本地曲目库
        /// </summary>
        private string clouldMusicLibDir;
        /// <summary>
        /// 定义播放列表项是唯一的还是直接加入
        /// </summary>
        private bool isUniquePlayListItem;
        /// <summary>
        /// 定义Web开始搜索的延时
        /// </summary>
        private double delayTimeWebLoad;
        /// <summary>
        /// 输出模式
        /// </summary>
        private string OutMode;
        /// <summary>
        /// 正在播放列表的加载线程数目
        /// </summary>
        private int playlistThreadNum;
        /// <summary>
        /// 正在播放列表的允许的最大项数
        /// </summary>
        private int playlistItemNum;
        /// <summary>
        /// 播放进程的优先级
        /// </summary>
        private int playThreadPriority;
        /// <summary>
        /// 桌面歌词宽度
        /// </summary>
        private double deskLRCWindowSize_X;
        /// <summary>
        /// 桌面歌词高度
        /// </summary>
        private double deskLRCWindowSize_Y;
        /// <summary>
        /// 桌面歌词X轴位置
        /// </summary>
        private double deskLrcWindowIndex_X;
        /// <summary>
        /// 桌面歌词Y轴位置
        /// </summary>
        private double deskLrcWindowIndex_Y;

        /// <summary>
        /// 是否缓存专辑图片
        /// </summary>
        private bool useAlbumImageCach;
        /// <summary>
        /// 是否使用显著图的缓存
        /// </summary>
        private bool useSRDImageCach;
        /// <summary>
        /// 全局颜色饱和度（影响调色板和自动取色）
        /// </summary>
        private bool globalValueOfSaturation;
        /// <summary>
        /// 全局颜色亮度（影响调色板和自动取色）
        /// </summary>
        private bool globalValueOfLight;
        /// <summary>
        /// 是否使用歌单背景
        /// </summary>
        private bool useSongListPageBackground;
        /// <summary>
        /// 是否使用网络上的歌单图片
        /// </summary>
        private bool useCouldMusicSongListCover;
        /// <summary>
        /// 当前使用的驱动
        /// </summary>
        private string deviceName="";
        /// <summary>
        /// 当前音量
        /// </summary>
        private int currentVolume = 50;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
        public static GlobalConfigClass _Config;

        private GlobalConfigClass()
        {
        }
        /// <summary>
        /// 加载Config
        /// </summary>
        /// <returns></returns>
        public static GlobalConfigClass LoadConfig()
        {
            if (_Config == null)
            {
                #region 如果可以从从XML中加载Config，则直接加载
                if (File.Exists("Config.xml"))
                {
                    FileInfo file = new FileInfo("Config.xml");
                    XmlSerializer xs = new XmlSerializer(typeof(GlobalConfigClass));
                    try
                    {
                        FileStream fs = File.OpenRead("Config.xml");
                        GlobalConfigClass Config = (GlobalConfigClass)xs.Deserialize(fs);
                        fs.Dispose();
                        _Config = Config;
                    }
                    catch (Exception)
                    {
                        System.Windows.MessageBox.Show("加载Config失败", "xml文件加载");
                    }
                }

                #endregion
                else
                {
                    //此处创建
                    _Config = new GlobalConfigClass()
                    {
                        AddIndex = 1,
                        NetCloudMusicLocalPath = "此处键入云音乐下载路径",
                        IsAtuoLrc = false,
                        IsPlayListPicFromNet = false,
                        IsUniquePlayListItem = false,
                        IsWaterAdmin = false,
                        LocalMusicDir = "LocalMusicDir",
                        LrcDir = "Lyrics",
                        OsuDir = "OsuDir",
                        DelayTimeWebLoad = 2.0,
                        DeskLRCWindowSize_X = 500,
                        DeskLRCWindowSize_Y = 100,

                        DeskLrcWindowIndex_X = 100,
                        DeskLrcWindowIndex_Y = 100,

                        PlaylistItemNum = 16,
                        PlaylistThreadNum = 12,

                        UseSongListPageBackground = false,
                        UseCouldMusicSongListCover = false,
                        UseAlbumImageCach = true,
                        UseSRDImageCach = true
                    };
                }
                if (!Directory.Exists(_Config.LrcDir)) Directory.CreateDirectory(_Config.LrcDir);
                if (!Directory.Exists(_Config.OsuDir)) Directory.CreateDirectory(_Config.OsuDir);
                if (!Directory.Exists(_Config.localMusicDir)) Directory.CreateDirectory(_Config.localMusicDir);
                return _Config;
            }
            else
            {
                return _Config;
            }

        }

        public string LrcDir
        {
            get
            {
                return lrcDir;
            }
            set
            {
                lrcDir = value; OnPropertyChanged(new PropertyChangedEventArgs("LrcDir"));
            }
        }
        public string NetCloudMusicLocalPath
        {
            get
            {
                return netCloudMusicLocalPath;
            }
            set
            {
                netCloudMusicLocalPath = value; OnPropertyChanged(new PropertyChangedEventArgs("NetCloudMusicLocalPath"));
            }
        }
        public string LocalMusicDir
        {
            get
            {
                return localMusicDir;
            }
            set
            {
                localMusicDir = value; OnPropertyChanged(new PropertyChangedEventArgs("LocalMusicDir"));
            }
        }
        public bool IsWaterAdmin
        {
            get
            {
                return isWaterAdmin;
            }
            set
            {
                isWaterAdmin = value; OnPropertyChanged(new PropertyChangedEventArgs("IsWaterAdmin"));
            }
        }
        public bool IsPlayListPicFromNet
        {
            get
            {
                return isPlayListPicFromNet;
            }
            set
            {
                isPlayListPicFromNet = value; OnPropertyChanged(new PropertyChangedEventArgs("IsPlayListPicFromNet"));
            }
        }
        /// <summary>
        /// 决定加入播放列表的方式--“1,next”OR“0,last”
        /// </summary>
        public int AddIndex
        {
            get
            {
                return addIndex;
            }
            set
            {
                addIndex = value; OnPropertyChanged(new PropertyChangedEventArgs("AddIndex"));
            }
        }
        public bool IsAtuoLrc
        {
            get
            {
                return isAtuoLrc;
            }
            set
            {
                isAtuoLrc = value; OnPropertyChanged(new PropertyChangedEventArgs("IsAtuoLrc"));
            }
        }
        public string OsuDir
        {
            get
            {
                return osuDir;
            }
            set
            {
                osuDir = value; OnPropertyChanged(new PropertyChangedEventArgs("OsuDir"));
            }
        }
        public string ClouldMusicLibDir
        {
            get
            {
                return clouldMusicLibDir;
            }
            set
            {
                clouldMusicLibDir = value; OnPropertyChanged(new PropertyChangedEventArgs("ClouldMusicLibDir"));
            }
        }
        public bool IsUniquePlayListItem
        {
            get
            {
                return isUniquePlayListItem;
            }
            set
            {
                isUniquePlayListItem = value; OnPropertyChanged(new PropertyChangedEventArgs("IsUniquePlayListItem"));
            }
        }
        public double DelayTimeWebLoad
        {
            get
            {
                return delayTimeWebLoad;
            }
            set
            {
                delayTimeWebLoad = value; OnPropertyChanged(new PropertyChangedEventArgs("DelayTimeWebLoad"));
            }
        }
        public double DeskLRCWindowSize_X
        {
            get
            {
                return deskLRCWindowSize_X;
            }
            set
            {
                deskLRCWindowSize_X = value; OnPropertyChanged(new PropertyChangedEventArgs("DeskLRCWindowSize_X"));
            }
        }
        public double DeskLRCWindowSize_Y
        {
            get
            {
                return deskLRCWindowSize_Y;
            }
            set
            {
                deskLRCWindowSize_Y = value; OnPropertyChanged(new PropertyChangedEventArgs("DeskLRCWindowSize_Y"));
            }
        }
        public double DeskLrcWindowIndex_X
        {
            get
            {
                return deskLrcWindowIndex_X;
            }
            set
            {
                deskLrcWindowIndex_X = value; OnPropertyChanged(new PropertyChangedEventArgs("DeskLrcWindowIndex_X"));
            }
        }
        public double DeskLrcWindowIndex_Y
        {
            get
            {
                return deskLrcWindowIndex_Y;
            }
            set
            {
                deskLrcWindowIndex_Y = value; OnPropertyChanged(new PropertyChangedEventArgs("DeskLrcWindowIndex_Y"));
            }
        }
        public string OutMode1 { get => OutMode; set { OutMode = value; OnPropertyChanged(new PropertyChangedEventArgs("OutMode1")); } }
        public int PlaylistThreadNum { get => playlistThreadNum; set { playlistThreadNum = value; OnPropertyChanged(new PropertyChangedEventArgs("PlaylistThreadNum")); } }
        public int PlaylistItemNum { get => playlistItemNum; set { playlistItemNum = value; OnPropertyChanged(new PropertyChangedEventArgs("PlaylistItemNum")); } }
        public int PlayThreadPriority { get => playThreadPriority; set { playThreadPriority = value; OnPropertyChanged(new PropertyChangedEventArgs("PlayThreadPriority")); } }
        public bool UseAlbumImageCach { get => useAlbumImageCach; set { useAlbumImageCach = value; OnPropertyChanged(new PropertyChangedEventArgs("UseAlbumImageCach")); } }
        public bool GlobalValueOfSaturation { get => globalValueOfSaturation; set {  globalValueOfSaturation = value;; OnPropertyChanged(new PropertyChangedEventArgs("GlobalValueOfSaturation"));} }
        public bool GlobalValueOfLight { get => globalValueOfLight; set {  globalValueOfLight = value;; OnPropertyChanged(new PropertyChangedEventArgs("GlobalValueOfLight"));} }
        public bool UseSRDImageCach { get => useSRDImageCach; set { useSRDImageCach = value; OnPropertyChanged(new PropertyChangedEventArgs("UseSRDImageCach")); } }
        public bool UseSongListPageBackground { get => useSongListPageBackground; set { useSongListPageBackground = value; OnPropertyChanged(new PropertyChangedEventArgs("UseSongListPageBackground")); } }
        public bool UseCouldMusicSongListCover{get => useCouldMusicSongListCover; set{useCouldMusicSongListCover = value;OnPropertyChanged(new PropertyChangedEventArgs("UseCouldMusicSongListCover"));}}

        public string DeviceName { get => deviceName; set{ deviceName = value; ; OnPropertyChanged(new PropertyChangedEventArgs("DeviceName")); } }

        public string OpenMethodsStr { get { return deviceName.Split('-').Count() == 2 ? deviceName.Split('-')[0]:""; } }
        public string DeviceStr { get { return deviceName.Split('-').Count() == 2 ? deviceName.Split('-')[1] : ""; } }

        public int CurrentVolume { get => currentVolume; set { currentVolume = value; OnPropertyChanged(new PropertyChangedEventArgs("CurrentVolume")); } }

        public bool LrcMode { get => lrcMode; set { lrcMode = value; OnPropertyChanged(new PropertyChangedEventArgs("LrcMode")); } }

        public string LrcBkColor { get => lrcBkColor; set { lrcBkColor = value; OnPropertyChanged(new PropertyChangedEventArgs("LrcBkColor")); } }
    }

}
