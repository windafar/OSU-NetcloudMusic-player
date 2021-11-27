using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Media.Imaging;
using System.Net;
using System.Threading;
using System.Security.Cryptography;
using System.Collections;
using PlayProjectGame.Helper;
using Microsoft.VisualBasic.Devices;

namespace PlayProjectGame.Data
{
    static class CouldMusicLocalDataGeter//获取数据类
    {
        public static List<UserData> NetClouldMusicData;//云音乐列表

        private static string connStr_Webdb;
        private static string connStr_Library;
        private static string select_web_user_playlist_Str;
        private static string select_web_playlist_track_Str;
        private static string select_web_playlist_Str;
        private static string select_lib_track_Str;
        private static string select_web_cloud_Str;

        private static SQLiteDataReader ExecQuery(string sqlStr, string connStr, out SQLiteConnection connect)
        {
            SQLiteConnection conn = new SQLiteConnection(connStr);
            conn.Open();
            SQLiteCommand comm = conn.CreateCommand();
            comm.CommandText = sqlStr;
            comm.CommandType = System.Data.CommandType.Text;
            SQLiteDataReader reader = comm.ExecuteReader();

            connect = conn;
            return reader;

        }
        static public List<UserData> GetSongList(bool reload=false)
        {
            #region 尝试从XML中加载原始SongList集合
            if (!reload&&File.Exists(GlobalConfigClass.XML_CLOUDMUSIC_SAVEPATH))
            {
                XmlSerializer xs = new XmlSerializer(typeof(List<UserData>));
                try
                {
                    FileStream fs = File.OpenRead(GlobalConfigClass.XML_CLOUDMUSIC_SAVEPATH);
                    NetClouldMusicData = (List<UserData>)xs.Deserialize(fs);
                    fs.Dispose();
                    return NetClouldMusicData;
                }
                catch (Exception e)
                {
                    System.Windows.MessageBox.Show("此时无法从序列化的文件加载歌单：" + e.Message, "xml文件加载");
                    return null;
                }

            }
            #endregion
            string ert = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            connStr_Webdb = SQLiteConnectionString.GetConnectionString(ert + @"\Netease\CloudMusic\Library\webdb.dat");
            connStr_Library = SQLiteConnectionString.GetConnectionString(ert + @"\Netease\CloudMusic\Library\library.dat");
            if (!System.IO.File.Exists(ert + @"\Netease\CloudMusic\Library\webdb.dat"))
                return new List<UserData>();
            select_web_user_playlist_Str = "select * from web_user_playlist";
            select_lib_track_Str = "select * from track";
            select_web_cloud_Str = "SELECT * FROM `web_cloud_track` WHERE `name` LIKE '{0}'";
            SQLiteConnection connect_web_user_playlist;
            SQLiteConnection connect_web_cloud_track;
            SQLiteDataReader DR_web_user_playlist = ExecQuery(select_web_user_playlist_Str, connStr_Webdb, out connect_web_user_playlist);


            #region 连接自Libary数据库的trac表，查询所有记录，然后生成歌曲字典（字典里面包含所有能在本地找到的歌曲信息）
            //2015年8月17日10:38:50:更新，同时生成一本以歌曲名作为键的字典，暂时做测试看是不是ID本身有重复
            SQLiteConnection conect_track;
            SQLiteDataReader DR_track = ExecQuery(select_lib_track_Str, connStr_Library, out conect_track);

            Dictionary<string, SongInfo> LibraryDirc_SongId = new Dictionary<string, SongInfo>();
            char[] separator = new char[1];
            separator[0] = '"';
            var js = new System.Web.Script.Serialization.JavaScriptSerializer();

            while (DR_track.Read())
            {//测试read
                SongInfo songInfo = new SongInfo()
                {
                    SongId = DR_track["tid"].ToString(),
                    SongName = DR_track["title"].ToString(),
                    SongPath = DR_track["file"].ToString(),
                    SongAlbum = DR_track["album"].ToString(),
                    SongArtist = DR_track["artist"].ToString(),
                    SongLengh = (float.Parse(DR_track["duration"].ToString()) / 60000).ToString("00.00"),
                    SongType = 2,
                    files = new List<string>(),
                };
                if (songInfo.SongPath.LastIndexOf(".flac") != -1)
                    songInfo.LocalFlac = true;
                else if (songInfo.SongPath.LastIndexOf(".ncm") != -1)
                {
                    string s = DR_track["track"].ToString();
                    try
                    {
                        var jarrSong = js.Deserialize<Dictionary<string, object>>(s);
                        string realSuffix = jarrSong["realSuffix"] as string;
                        if (realSuffix == "flac")
                            songInfo.LocalFlac = true;
                    }
                    catch
                    {
                        if (s.LastIndexOf("flac") > -1) songInfo.LocalFlac = true;
                        else songInfo.LocalFlac = false;
                    }
                }
                else songInfo.LocalFlac = false;
                if (string.IsNullOrWhiteSpace(songInfo.SongId))
                {
                    string s = DR_track["track"].ToString();
                    string[] sl = null;
                    if (!string.IsNullOrWhiteSpace(s) && (sl = s.Split(separator, 4)).Length > 2)
                    {//1，尝试获取musicId
                        s = sl[2].Remove(0, 1);
                        songInfo.SongId = s.Remove(s.Count() - 1);
                    }
                    else
                    {//2，尝试从云端获取
                        SQLiteDataReader DR_web_cloud_track =
                            ExecQuery(string.Format(select_web_cloud_Str
                                    , songInfo.SongName.Replace("'", "''").Replace("\"", "\"\""))
                                    , connStr_Webdb
                                , out connect_web_cloud_track);
                        while (DR_web_cloud_track.Read())
                        {
                            var _Ab = DR_web_cloud_track["album"].ToString().ToLower();
                            var _Ar = DR_web_cloud_track["artist"].ToString().ToLower();
                            var _obj = songInfo.SongId = DR_web_cloud_track["id"] as string;
                            if (!string.IsNullOrWhiteSpace(_obj))
                            {
                                if (songInfo.SongAlbum.ToLower() == _Ab)
                                {
                                    songInfo.SongId = _obj;
                                    if (songInfo.SongArtist.ToLower() == _Ar)
                                        songInfo.SongId = _obj;
                                }
                            }
                            //else if (string.IsNullOrWhiteSpace(songInfo.SongAlbum.ToLower()) && string.IsNullOrWhiteSpace(songInfo.SongArtist.ToLower()))
                            //{

                            //}
                        }
                    }
                }
                SongInfo tryvalue;
                if (!LibraryDirc_SongId.TryGetValue(songInfo.SongId, out tryvalue))
                    LibraryDirc_SongId.Add(songInfo.SongId, songInfo);
                else
                {
                    //本地完全重复的曲目，只是文件名不一样。pass
                    tryvalue.files.Add(songInfo.SongPath);
                }
            }
            conect_track.Close();
            conect_track.Dispose();
            #endregion

            SQLiteConnection connect_web_playlist_track;
            SQLiteConnection connect_web_track;
            Dictionary<string, SongInfo> WebDB_Dic = new Dictionary<string, SongInfo>();
            #region 连接Web.DB,生成字典
            string select_web_track_Str = "select * from web_track";
            SQLiteDataReader DR_web_track;
            DR_web_track = ExecQuery(select_web_track_Str, connStr_Webdb, out connect_web_track);
            // var js = new System.Web.Script.Serialization.JavaScriptSerializer();
            while (DR_web_track.Read())
            {
                string id = DR_web_track["tid"].ToString();
                bool hasFlac = false;
                //后面的歌id可能会和trackStr里面MusicId的不一样，那时以trackStr里面的Id为准，格式改变。。需重写获取逻辑
                string trackStr = DR_web_track["track"].ToString();
                var f = DR_web_track["version"].ToString().Split(new char[1] { '-' }, StringSplitOptions.RemoveEmptyEntries);

                ///flac存在条件组
                if (f.Count() < 7) hasFlac = false;
                else if (int.Parse(f[5]) > 320) hasFlac = true;//远端是否为999
                else hasFlac = false;

                ///flac权限条件组
                if (f.Count() < 11) hasFlac = false;
                else if (int.Parse(f[7]) > 0//推测为权限，但是好像又不是，看数据库有下载的基本上都是7-1-1
                    && int.Parse(f[8]) > 0
                    && int.Parse(f[9]) > 0) { }
                else hasFlac = false;
                //推测f[6]是试听比特率，
                //唯一确定的是当f5==999&&f789==0时为无权限的sq

                string _SongName = "", _SongAlbum = "", _SongArtist = ",";
                var jarrSong = js.Deserialize<Dictionary<string, object>>(trackStr);
                _SongName = jarrSong["name"] as string;
                id = jarrSong["id"].ToString();
                _SongAlbum = (jarrSong["album"] as Dictionary<string, object>)["name"] as string;
                foreach (var s in (ArrayList)jarrSong["artists"])
                    _SongArtist += ((Dictionary<string, object>)s)["name"] as string + ",";

                SongInfo songInfo = new SongInfo()
                {
                    SongId = id,
                    SongName = _SongName,
                    SongPath = null,
                    SongAlbum = _SongAlbum,
                    SongArtist = _SongArtist,
                    SongType = 2,
                    RemoteFlac = hasFlac
                };
                if (!WebDB_Dic.ContainsKey(id))
                    WebDB_Dic.Add(id, songInfo);
                else
                {

                }
            }
            connect_web_track.Dispose();
            #endregion


            SQLiteDataReader DR_web_playlist_track;
            SQLiteConnection conect_web_playlist;
            SQLiteDataReader DR_web_playlist;
            //string[] CloudMusicDownfiles;//用于最后的搜索机会


            List<UserData> AllUserPlayList = new List<UserData>();

            while (DR_web_user_playlist.Read())//遍历用户
            {
                UserData USDB = new UserData("网易云用户Id-" + DR_web_user_playlist["uid"].ToString(), DR_web_user_playlist["pids"].ToString());//初始化使用者ID和拥有的歌单ID集合

                foreach (PlayListData PLD in USDB.Pids)//遍历歌单
                {
                    #region 查询歌单ID对应的名字
                    select_web_playlist_Str = "select * from web_playlist where pid=" + PLD.PlayListId;
                    DR_web_playlist = ExecQuery(select_web_playlist_Str, connStr_Webdb, out conect_web_playlist);
                    DR_web_playlist.Read();//读取第一行(只有一行)
                    if (DR_web_playlist["pid"].ToString() != PLD.PlayListId)
                        throw new NullReferenceException("DR_web_playlist中指定Pid不存在");
                    string s1 = DR_web_playlist["playlist"].ToString();

                    var jarrSong = js.Deserialize<Dictionary<string, object>>(s1);
                    PLD.PlatListName = (string)jarrSong["name"];
                    PLD.UserId = (string)jarrSong["userId"].ToString();
                    try
                    {
                        PLD.UersName = (string)((Dictionary<string, object>)jarrSong["creator"])["nickname"];
                    }
                    catch (Exception)
                    { }
                    PLD.PlayListInfo = (string)jarrSong["description"];
                    PLD.PlayListNetImageUri = (string)jarrSong["coverImgUrl"];
                    #endregion


                    #region 查询歌单ID为PLD.PlayListId的歌曲集合.***2015年8月17日10:48:48更新：改成从web_track获取，以同时获取歌曲名
                    select_web_playlist_track_Str = "select pid,tid from web_playlist_track where pid=" + PLD.PlayListId;
                    DR_web_playlist_track = ExecQuery(select_web_playlist_track_Str, connStr_Webdb, out connect_web_playlist_track);
                    int index = 0;
                    while (DR_web_playlist_track.Read())
                    {
                        index++;
                        SongInfo songInfo = new SongInfo();
                        string songId = DR_web_playlist_track["tid"].ToString();
                        if (LibraryDirc_SongId.TryGetValue(songId, out songInfo))
                        {
                            try
                            {
                                songInfo.RemoteFlac = WebDB_Dic[songId].RemoteFlac;
                            }
                            catch (KeyNotFoundException)
                            {
                                throw new KeyNotFoundException("在缓存中没有找到本地存储的歌曲信息,请缓存该歌单：" + PLD.PlatListName + ":" + songInfo.SongName);
                            }
                        }
                        else
                        {
                            //Libary库中不存在该SongId时，从来自web.dat的web_track表的字典中获取该ID对应的歌曲信息
                            if (WebDB_Dic.ContainsKey(songId))
                            {
                                songInfo = WebDB_Dic[songId];
                            }
                            else
                            {
                                try
                                {
                                    songInfo.SongId = "songId"; songInfo.SongName += "_id异常";
                                }
                                catch (Exception)
                                {
                                    continue;
                                }
                            }
                        }
                        PLD.Songs.Add(new SongInfoExpend()
                        {
                            SongInfo = songInfo,
                            Source = "netid_" + PLD.PlayListId,
                            SourceName = PLD.PlatListName,
                            SongInfoIndex = index,
                        });//加入单个歌单拥有的歌曲集合
                        //DR_track.Dispose();
                        //conect_track.Close();
                        //conect_track.Dispose();
                    }
                    DR_web_playlist_track.Dispose();
                    connect_web_playlist_track.Close();
                    connect_web_playlist_track.Dispose();
                    DR_web_playlist.Dispose();
                    conect_web_playlist.Close();
                    conect_web_playlist.Dispose();
                    #endregion
                }


                AllUserPlayList.Add(USDB);//playlist包含了各个本地用户的本地歌单
            }


            connect_web_user_playlist.Close();
            connect_web_user_playlist.Dispose();
            NetClouldMusicData = AllUserPlayList;
            if (!File.Exists(GlobalConfigClass.XML_CLOUDMUSIC_SAVEPATH))
                backup();//当本地数据文件不存在，即一次启动时才自动备份
            return AllUserPlayList;

        }

        static public List<UserData> AppendNewSongListToXML(List<UserData> newSource) 
        {
            if (newSource == null) newSource = new List<UserData>();
            List<UserData> destData = new List<UserData>();
            #region 尝试从XML中加载原始SongList集合
            if (File.Exists(GlobalConfigClass.XML_CLOUDMUSIC_SAVEPATH))
            {
                XmlSerializer xs = new XmlSerializer(typeof(List<UserData>));
                try
                {
                    FileStream fs = File.OpenRead(GlobalConfigClass.XML_CLOUDMUSIC_SAVEPATH);
                    destData = (List<UserData>)xs.Deserialize(fs);
                    fs.Dispose();
                }
                catch (Exception e)
                {//数据源加载失败则直接返回，否则会覆盖源数据
                    System.Windows.MessageBox.Show("此时无法从序列化的文件加载歌单：" + e.Message, "xml文件加载");
                    return null;
                }

            }
            #endregion

           // destData = newSource.Union(destData, Helper.Comparer.UserIdComparer).ToList();
            //不能加进来去重，不能加进来做并集，因为所有依赖单一比较器的都会删掉不同子对象的相同父对象
            foreach (var su in newSource) 
            {
                var du = destData.FirstOrDefault(x => x.Uid == su.Uid);
                if (du == null) destData.Insert(0,su);
                else 
                    foreach (var sp in su.Pids) 
                    {
                        var dp = du.Pids.FirstOrDefault(x => x.PlayListId == sp.PlayListId);
                        if (dp == null) du.Pids.Insert(0,sp);
                        else
                            dp.Songs = sp.Songs.Union(dp.Songs, Helper.Comparer.SongPathEqualityComparer).ToList();
                        //这个Union生成的集合会以第一个为原始集合，即f,s相同时取f
                    }
            }
            NetClouldMusicData = destData;
            backup();
            return destData;
        }

        static public void backup() 
        {
            if (NetClouldMusicData == null) throw new InvalidOperationException("备份失败，NetClouldMusicData 对象为空");
            //Task.Run(() =>
            //{
            OtherHelper.WriteXMLSerializer(NetClouldMusicData, typeof(List<UserData>), GlobalConfigClass.XML_CLOUDMUSIC_SAVEPATH+"_back");

            if (File.Exists(GlobalConfigClass.XML_CLOUDMUSIC_SAVEPATH))
                File.Delete(GlobalConfigClass.XML_CLOUDMUSIC_SAVEPATH);
            Computer MyComputer = new Computer();
            MyComputer.FileSystem.RenameFile(new FileInfo(GlobalConfigClass.XML_CLOUDMUSIC_SAVEPATH).FullName + "_back",  GlobalConfigClass.XML_CLOUDMUSIC_SAVEPATH);
            // });
        }
    }

    static class SQLiteConnectionString
    {

        public static string GetConnectionString(string path)
        {
            return GetConnectionString(path, null);
        }

        public static string GetConnectionString(string path, string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return "Data Source=" + path;
            }
            else
            {
                return "Data Source=" + path + ";Password=" + password;
            }
        }

    }

}
