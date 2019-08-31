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
using System.Xml;
using PlayProjectGame.Helper;

namespace PlayProjectGame.Data
{
    public class OsoDataGeter
    {
        public static List<UserData> OsuListData;

        /// <summary>
        /// 用于序列化通用的osu数据
        /// </summary>
        public struct MusicNameType
        {
            private string md5;
            private string path;
            private string picPath;
            private string songDir;

            public string Md5 { get => md5; set => md5 = value; }
            public string Path { get => path; set => path = value; }
            public string PicPath { get => picPath; set => picPath = value; }
            public string SongDir { get => songDir; set => songDir = value; }
        }
        /// <summary>
        /// 和MusicNameType类似，但只用于收藏夹的osu-hash-map条目
        /// </summary>
        public struct Arr_MD5_Path
        {
            private string md5;
            private string path;
            private string picPath;

            public string Md5 { get => md5; set => md5 = value; }
            public string Path { get => path; set => path = value; }
            public string PicPath { get => picPath; set => picPath = value; }
        }
        /// <summary>
        /// 收藏夹文件结构
        /// </summary>
        public struct ClloNameType
        {
            public string ClloName;
            public Arr_MD5_Path[] arr_md5_path;
        }

        Dictionary<string, MusicNameType> AllSongDict;
        private string[] SongList;
        /// <summary>
        /// 
        /// </summary>
        Dictionary<int, ClloNameType> ClloSongDict;
        private List<UserData> UserDataList;
        /// <summary>
        /// OSU程序目录
        /// </summary>
        string osupath;
        /// <summary>
        /// OSU的收藏夹路径
        /// </summary>
        string osuCllo;

        string osudb;
        public OsoDataGeter(string OsuPath)
        {
            osupath = OsuPath;
        }

        /// <summary>
        /// 读取OSU文件为图片或视频
        /// </summary>
        /// <param name="k">1--BG，2--VD</param>
        /// <param name="Osupath">.osu文件的路径</param>
        /// <returns>根据k值返回文件</returns>
        public static string LoadOsuFileNameAs(int k, string Osupath)
        {
            StreamReader SR = new StreamReader(Osupath);
            string sline = "";
            string Osb_BG = "";
            string Osb_Vd = "";
            while ((sline = SR.ReadLine()) != null)
            {
                if (sline != "" && sline[0] == '[' && sline == "[Events]")
                {
                    if (sline == "[Events]")
                    {
                        while ((sline = SR.ReadLine()) != null && sline != "")
                        {
                            if (sline[0] == '0' && sline[1] == ',')
                            {
                                StringBuilder strbud = new StringBuilder();
                                int i = 2;
                                while (sline[i] != '"') i++;
                                while (sline[++i] != '"')
                                {
                                    strbud.Append(sline[i]);
                                }
                                Osb_BG = strbud.ToString();
                                if (k == 1)
                                {
                                    return Osb_BG == "" ? null : (new FileInfo(Osupath)).DirectoryName + "//" + Osb_BG;
                                }
                            }
                            if (sline[0] == 'V' && sline[1] == 'i')
                            {
                                StringBuilder strbud = new StringBuilder();
                                int i = 4;
                                while (sline[i] != '"') i++;
                                while (sline[++i] != '"')
                                {
                                    strbud.Append(sline[i]);
                                }
                                Osb_Vd = strbud.ToString();
                                if (k == 2)
                                {
                                    return Osb_Vd == "" ? null : (new FileInfo(Osupath)).DirectoryName + "//" + Osb_Vd;
                                }
                            }
                        }
                    }
                }
            }
            return "";
        }
        /// <summary>
        /// 读取Osu文件的艺术家和歌曲名，专辑
        /// </summary>
        /// <param name="OsuFilePath">.Osu文件路径</param>
        /// <returns>string[0]标题，string[1]艺术家，string[2]Source,string[3]音频文件名</returns>
        public static string[] LoadOsuFileAsSongInfo(string OsuFilePath)
        {
            StreamReader SR = new StreamReader(OsuFilePath);
            string[] SongInfos = new string[4];
            string sline;
            string[] SlineSplitTemp;
            while ((sline = SR.ReadLine()) != null)
            {
                if (sline == "[General]")
                {
                    sline = SR.ReadLine();
                    SlineSplitTemp = sline.Split(':');
                    if (SlineSplitTemp[0] == "AudioFilename")
                        SongInfos[3] = SlineSplitTemp[1].TrimStart();
                }

                if (sline == "[Metadata]")
                {
                    while ((sline = SR.ReadLine()) != null)
                    {
                        SlineSplitTemp = sline.Split(':');
                        if (SlineSplitTemp.Count() <= 1) break;
                        else if (SlineSplitTemp[0] == "Title")
                            SongInfos[0] = SlineSplitTemp[1];
                        else if (SlineSplitTemp[0] == "TitleUnicode")
                        {
                            if (SlineSplitTemp[1] != "")

                                SongInfos[0] = SlineSplitTemp[1];
                        }
                        else if (SlineSplitTemp[0] == "Artist")
                            SongInfos[1] = SlineSplitTemp[1];
                        else if (SlineSplitTemp[0] == "ArtistUnicode")
                        {
                            if (SlineSplitTemp[1] != "")

                                SongInfos[1] = SlineSplitTemp[1];
                        }
                        else if (SlineSplitTemp[0] == "Source")
                            SongInfos[2] = SlineSplitTemp[1];
                    }
                    break;
                }
            }
            SR.Dispose();
            return SongInfos;
        }
        void LoadCllo(string cllopath)
        {
            if (File.Exists(cllopath))
            {
                ClloSongDict = new Dictionary<int, ClloNameType>();
                using (var fs = new FileStream(cllopath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var reader = new BinaryReader(fs);
                    int ver = reader.ReadInt32(); //version
                    int count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        string title = (reader.ReadByte() == 0x0b ? reader.ReadString() : "");
                        int itemcount = reader.ReadInt32();
                        var nset = new List<int>();
                        ClloNameType ClloNameNode = new ClloNameType();
                        ClloNameNode.ClloName = title;
                        ClloNameNode.arr_md5_path = new Arr_MD5_Path[itemcount];
                        for (int j = 0; j < itemcount; j++)
                        {
                            string md5 = (reader.ReadByte() == 0x0b ? reader.ReadString() : "");
                            ClloNameNode.arr_md5_path[j].Md5 = md5;
                        }
                        ClloSongDict.Add(i, ClloNameNode);
                    }
                }
            }
        }
        void LoadAllFile(string SongsPath)
        {

            XmlSerializer xs = new XmlSerializer(typeof(List<MusicNameType>));
            FileInfo fileInfo = new FileInfo(GlobalConfigClass.XML_OSU_AllPATH);
            if (fileInfo.Exists)
            {//从缓存加载
                var llist = (List<MusicNameType>)xs.Deserialize(fileInfo.OpenRead());
                AllSongDict = llist.ToDictionary(x => x.Md5);
                return;
            }
            AllSongDict = new Dictionary<string, MusicNameType>();
            SongList = Directory.GetDirectories(SongsPath);

            string song_dir_path;
            string md5_kvlue;
            string[] song_osb_list;
            int song_list_count = SongList.Count();//总的文件目录计数

            for (int i = 0; i < song_list_count; i++)
            {//遍历目录列表，找到.osu文件
                song_dir_path = SongList[i];
                song_osb_list = Directory.GetFiles(SongList[i], "*.osu");
                int song_osb_num = song_osb_list.Count();

                for (int j = 0; j < song_osb_num; j++)
                {//计算找到的osu文件MD5值装入字典中
                    md5_kvlue = GetHash(song_osb_list[j]);
                    MusicNameType OsbNode = new MusicNameType();
                    OsbNode.Md5 = md5_kvlue;
                    OsbNode.Path = song_osb_list[j];
                    OsbNode.SongDir = song_dir_path;

                    ///如果之后没法解决从osudb读取的问题就取消注释在这儿加上歌曲信息读取，改hashfromfile为hsashfromstring,顺便拿信息不增加时间复杂度
                    //string loadstr = LoadOsuFileNameAs(1, song_osb_list[j]);
                    //loadstr = song_dir_path + "\\" + loadstr;
                    //if (File.Exists(loadstr))
                    //    OsbNode.PicPath = loadstr;
                    //else OsbNode.PicPath = "";
                    ///

                    if (!AllSongDict.ContainsKey(md5_kvlue))
                        AllSongDict.Add(md5_kvlue, OsbNode);
                }
            }
            if (File.Exists(GlobalConfigClass.XML_OSU_AllPATH))
                File.Delete(GlobalConfigClass.XML_OSU_AllPATH);
            var uio = AllSongDict.Values.ToList();
            OtherHelper.WriteXMLSerializer(uio, typeof(List<MusicNameType>), GlobalConfigClass.XML_OSU_AllPATH);
        }
        public List<UserData> GetOsuCollList(bool IsUpdate)
        {
            //GetOsuDB(@"I:\osu!test\osu!.db");
            if (IsUpdate && File.Exists(GlobalConfigClass.XML_OSU_AllPATH))
                File.Delete(GlobalConfigClass.XML_OSU_AllPATH);
            #region 如果可以从从XML中加载SongList集合，则直接加载
            if (!IsUpdate && File.Exists(GlobalConfigClass.XML_OSU_SAVEPATH))
            {
                if (File.Exists(osupath + "\\collection.db"))
                {
                    osuCllo = osupath + "\\collection.db";
                    LoadCllo(osuCllo);
                }

                FileInfo file = new FileInfo(GlobalConfigClass.XML_OSU_SAVEPATH);
                XmlSerializer xs = new XmlSerializer(typeof(List<UserData>));
                try
                {
                    FileStream fs = File.OpenRead(GlobalConfigClass.XML_OSU_SAVEPATH);
                    List<UserData> SongList = (List<UserData>)xs.Deserialize(fs);

                    fs.Dispose();
                    if (SongList.Count == 0)
                    {
                        File.Delete(GlobalConfigClass.XML_OSU_SAVEPATH);
                        return GetOsuCollList(true);
                    }
                    OsuListData = SongList;
                    return SongList;
                }
                catch (Exception e)
                {
                    //System.Windows.MessageBox.Show("此时无法从序列化的文件加载osu收藏夹:"+ e.Message, "xml文件加载");
                }

            }
            #endregion
            UserDataList = new List<UserData>();
            if (Directory.Exists(osupath))
            {
                if (Directory.Exists(osupath + "\\Songs"))
                    LoadAllFile(osupath + "\\Songs");
                if (File.Exists(osupath + "\\collection.db"))
                {
                    osuCllo = osupath + "\\collection.db";
                    LoadCllo(osuCllo);
                }
                LoadNewFile();

                UserData UD = new UserData() { Uid = "OSU", Pids = new List<PlayListData>() };
                int index = 0;
                string path = "";
                foreach (ClloNameType cllonode in ClloSongDict.Values)
                {
                    PlayListData PLD = new PlayListData() { PlatListName = cllonode.ClloName };
                    index = 0;
                    foreach (Arr_MD5_Path s1 in cllonode.arr_md5_path)
                    {
                        index++;
                        if (AllSongDict.ContainsKey(s1.Md5))
                        {
                            MusicNameType MNT = AllSongDict[s1.Md5];
                            string OsuFilePath = MNT.Path;
                            string pathtemp = new DirectoryInfo(OsuFilePath).Parent.Name;
                            if (path == pathtemp)//去除重复osu文件
                                continue;
                            else path = pathtemp;
                            string[] xx = LoadOsuFileAsSongInfo(OsuFilePath);
                            string SongPath = (new FileInfo(OsuFilePath)).Directory + "\\" + xx[3];
                            PLD.Songs.Add(new SongInfoExpend()
                            {
                                SongInfo = new SongInfo()
                                {
                                    /*之后所有提前写的行如果解决了读取osubd的问题则全注释掉*/
                                    SongName = xx[0],
                                    SongId = s1.Md5,
                                    SongPath = SongPath,
                                    SongType = 3,
                                    SongArtist = xx[1],
                                    SongAlbum = xx[2],
                                    OsuPath = MNT.Path,
                                },
                                Source = "osuname_" + cllonode.ClloName,
                                SourceName = cllonode.ClloName,
                                SongInfoIndex = index,
                                FileTime = File.GetCreationTime(OsuFilePath).Ticks
                            }
                                );
                        }
                    }
                    PLD.Songs = PLD.Songs.OrderByDescending(x => x.FileTime).ToList();
                    UD.Pids.Add(PLD);
                }

                var _allsongs = new List<SongInfoExpend>();
                var Songlist_all = new PlayListData
                {
                    PlatListName = "全部",
                    PlayListId = "-2",
                    PlayListType = 3,
                    Songs = new List<SongInfoExpend>()
                };
                index = 0;
                foreach (MusicNameType MNT in AllSongDict.Values)
                {
                    index++;
                    string OsuFilePath = MNT.Path;
                    string[] xx = LoadOsuFileAsSongInfo(OsuFilePath);
                    string SongPath = (new FileInfo(OsuFilePath)).Directory + "\\" + xx[3];
                    string pathtemp = new DirectoryInfo(OsuFilePath).Parent.Name;
                    if (path == pathtemp)//去除重复osu文件
                        continue;
                    else path = pathtemp;

                    _allsongs.Add(new SongInfoExpend()
                    {
                        SongInfo = new SongInfo()
                        {
                            SongName = xx[0],
                            SongId = MNT.Md5,
                            SongPath = SongPath,
                            SongType = 3,
                            SongArtist = xx[1],
                            SongAlbum = xx[2],
                            OsuPath = MNT.Path,
                        },
                        Source = "osuname_all",
                        SourceName = "全部",
                        SongInfoIndex = index,
                        FileTime = File.GetCreationTime(OsuFilePath).Ticks

                    });
                }
                foreach (var e in _allsongs.OrderByDescending(x => x.FileTime))
                {
                    Songlist_all.Songs.Add(e);
                }

                UD.Pids.Add(Songlist_all);

                UserDataList.Add(UD);


                OsuListData = UserDataList;
                if (File.Exists(GlobalConfigClass.XML_OSU_SAVEPATH))
                    File.Delete(GlobalConfigClass.XML_OSU_SAVEPATH);
                OtherHelper.WriteXMLSerializer(UserDataList, typeof(List<UserData>), GlobalConfigClass.XML_OSU_SAVEPATH);
                return UserDataList;
            }
            return UserDataList;
        }

        void LoadNewFile()
        {
            var dirs = new DirectoryInfo(osupath + "\\Songs").GetDirectories().OrderByDescending(x => x.LastWriteTime);
            foreach (var dir in dirs)
            {
                var files = dir.GetFiles("*.osu");
                foreach (var file in files)
                {
                    var h = GetHash(file.FullName);
                    if (!AllSongDict.ContainsKey(h))
                    {
                        AllSongDict.Add(h, new MusicNameType
                        {
                            Md5 = h,
                            Path = file.FullName,
                            SongDir = dir.FullName
                        });
                    }
                    else return;

                }

            }
        }

        public void GetOsuDB(string path)
        {
            List<string> list = new List<string>();
            if (File.Exists(path))
            {
                byte[] buff = File.ReadAllBytes(path);
                for (int i = 0; i < buff.Length; i++)
                {

                    //if (buff[i] == 0x00 && buff[i+1] == 0x0b)
                    //{
                    //    list.Clear();
                    //}

                    //每结开头
                    if (buff[i] == 0x0b)
                    {
                        i += 1;
                        int nodeLength = buff[i];
                        if (nodeLength != 0)
                        {
                            var nodebuff = new byte[nodeLength];
                            for (int j = 0; j < nodeLength; j++)
                            {
                                nodebuff[j] = buff[++i];
                            }
                            list.Add(UTF8Encoding.UTF8.GetString(nodebuff));
                        }
                    }

                    ////所有节结束
                    //if (buff[i] == 0x33 && buff[i + 1] == 0x33 && buff[i + 2] == 0x3F)
                    //{
                    //    list.Clear();
                    //    i += 2;
                    //}
                }
            }
            File.WriteAllLines("111", list.ToArray());
        }
    }
}
