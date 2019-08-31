using PlayProjectGame.Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PlayProjectGame.Data
{
    class OsuLocalDataGeter:IDisposable
    {
        class Favorite
        {
           public string name;
           public FavoriteNode[] favoriteNode;
        }
        class FavoriteNode
        {
           public string md5;
        }
        [Serializable]
        class OsuDBElem
        {
            private string osuFilePath;
            private SongInfo songInfo;
            private long creatTime;

            public string OsuFilePath { get => osuFilePath; set => osuFilePath = value; }
            public SongInfo SongInfo { get => songInfo; set => songInfo = value; }
            public long CreatTime { get => creatTime; set => creatTime = value; }
        }
        MD5 md5Hash = MD5.Create();
        Dictionary<string, OsuDBElem> OsuDB = new Dictionary<string, OsuDBElem>();
        string FavoriteFilePath;
        string OsuSongListDirPath;
        string DataFilePath="OSUDATA.dll";
        private Dictionary<int, Favorite> Favorites;

        public OsuLocalDataGeter(string osupath)
        {
            FavoriteFilePath = osupath + "\\collection.db";
            OsuSongListDirPath= osupath + "\\Songs";

        }

        public Task<List<UserData>> GetOsuSongList(bool IsReload)
        {
            return Task.Run<List<UserData>>(() =>
            {
                var task = GetOsuSongListData(IsReload);
                task.Start();
                LoadFavorite(FavoriteFilePath);
                task.Wait();
                List<UserData> userDatas = new List<UserData>();
                UserData UD = new UserData() { Username = "OSU", Uid = "OSU", Pids = new List<PlayListData>() };
                userDatas.Add(UD);
                foreach (var favorite in Favorites.Values)
                {
                    int index = 0;
                    PlayListData pld = new PlayListData();
                    pld.PlatListName = favorite.name;
                    pld.UersName = UD.Username;
                    pld.PlayListType = 3;
                    HashSet<string> table = new HashSet<string>();
                    foreach (var node in favorite.favoriteNode)
                    {
                        index++;
                        OsuDBElem osuDBelem;
                        if (!OsuDB.TryGetValue(node.md5, out osuDBelem))
                            continue;
                        if (table.Contains(osuDBelem.SongInfo.SongPath))
                            continue;//remove duplicate song
                        SongInfoExpend songInfoExpend = new SongInfoExpend()
                        {
                            SongInfo = osuDBelem.SongInfo,
                            Source = "osuname_" + favorite.name,
                            SourceName = favorite.name,
                            SongInfoIndex = index,
                            FileTime = osuDBelem.CreatTime
                        };
                        table.Add(osuDBelem.SongInfo.SongPath);
                        
                        pld.Songs.Add(songInfoExpend);
                    }
                    pld.Songs.Sort(Comparer.FileCreateTimeEqualityComparer);
                    UD.Pids.Add(pld);
                }
                //addpend favorite for all osusong
                UD.Pids.Add(new PlayListData()
                {
                    PlatListName = "OSU曲库",
                    PlayListId = "-2",
                    PlayListType = 3,
                    Songs = OsuDB.Values.GroupBy(x=>x.SongInfo.SongPath)
                                    .Select((x, y) => new SongInfoExpend()
                                    {
                                        SongInfo = x.First().SongInfo,
                                        SongInfoIndex = y,
                                        Source = "osuname_All",
                                        SourceName = "OSU曲库",
                                        FileTime=x.First().CreatTime
                                    }).OrderByDescending(x=>x.FileTime).ToList()
                });

                return userDatas;
            });

        }

        Task<Dictionary<string, OsuDBElem>> GetOsuSongListData(bool reload=false)
        {
            return new Task<Dictionary<string, OsuDBElem>>(() =>
            {
                if (!reload && File.Exists(DataFilePath))
                    OsuDB = Helper.OtherHelper.DeserializeObject(File.ReadAllBytes(DataFilePath)) as Dictionary<string, OsuDBElem>;
                else reload = true;

                var dirs = new DirectoryInfo(OsuSongListDirPath).GetDirectories().OrderByDescending(x => x.CreationTime);
                bool IsReturn = false;
                foreach (var dir in dirs)
                {
                    var files = dir.GetFiles("*.osu");
                    foreach (var file in files)
                    {
                        var osufileHash = GetHash(file.FullName);
                        if (!OsuDB.ContainsKey(osufileHash))
                        {
                            SongInfo songInfo = LoadOsuFileAsSongInfo(file.FullName);
                            songInfo.SongId = osufileHash;
                            OsuDB.Add(osufileHash, new OsuDBElem()
                            {
                                OsuFilePath = file.FullName,
                                CreatTime = file.CreationTime.Ticks,
                                SongInfo = songInfo
                            });
                        }
                        else
                        {
                            IsReturn = true&& !reload;
                            break;
                        }
                    }
                    if (IsReturn) break;
                }
                Task.Run(() => File.WriteAllBytes(DataFilePath,
                                Helper.OtherHelper.SerializeObject(OsuDB))) ;
                //mabey this is a pit when obj disposed, of couse,, but i do't know, 
                return OsuDB;
            });
        }

        void LoadFavorite(string cllopath)
        {
            if (File.Exists(cllopath))
            {
                Favorites = new Dictionary<int, Favorite>();
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
                        Favorite favorite = new Favorite();
                        favorite.name = title;
                        favorite.favoriteNode = new FavoriteNode[itemcount];
                        for (int j = 0; j < itemcount; j++)
                        {
                            string md5 = (reader.ReadByte() == 0x0b ? reader.ReadString() : "");
                            favorite.favoriteNode[j] = new FavoriteNode() {md5=md5 };
                        }
                        Favorites.Add(i, favorite);
                    }
                }
            }
        }

        string GetHash(string osbpath)
        {
            string strHashData = "";
                using (FileStream fs = new FileStream(osbpath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    Byte[] arrHashValue = md5Hash.ComputeHash(fs);
                    strHashData = BitConverter.ToString(arrHashValue);
                    strHashData = strHashData.Replace("-", "");
                    strHashData = strHashData.ToLower();
                }
            return strHashData;
        }

        SongInfo LoadOsuFileAsSongInfo(string OsuFilePath)
        {
            StreamReader SR = new StreamReader(OsuFilePath);
            string sline;
            string[] SlineSplitTemp;
            SongInfo songInfo = new SongInfo()
            { SongType=3,OsuPath=OsuFilePath,
            SongAlbum="",SongArtist="",SongName="",SongPath=""
            };
            while ((sline = SR.ReadLine()) != null)
            {
                if (sline == "[General]")
                {
                    sline = SR.ReadLine();
                    SlineSplitTemp = sline.Split(':');
                    if (SlineSplitTemp[0] == "AudioFilename")
                        songInfo.SongPath = Directory.GetParent(OsuFilePath).FullName+"\\"+
                            SlineSplitTemp[1].TrimStart();
                }

                if (sline == "[Metadata]")
                {
                    while ((sline = SR.ReadLine()) != null)
                    {
                        SlineSplitTemp = sline.Split(':');
                        if (SlineSplitTemp.Count() <= 1) break;
                        else if (SlineSplitTemp[0] == "Title")
                            songInfo.SongName = SlineSplitTemp[1];
                        else if (SlineSplitTemp[0] == "TitleUnicode")
                        {
                            if (SlineSplitTemp[1] != "")

                                songInfo.SongName = SlineSplitTemp[1];
                        }
                        else if (SlineSplitTemp[0] == "Artist")
                            songInfo.SongArtist = SlineSplitTemp[1];
                        else if (SlineSplitTemp[0] == "ArtistUnicode")
                        {
                            if (SlineSplitTemp[1] != "")

                                songInfo.SongArtist = SlineSplitTemp[1];
                        }
                        else if (SlineSplitTemp[0] == "Source")
                            songInfo.SongAlbum = SlineSplitTemp[1];
                    }
                    break;
                }
            }
            SR.Dispose();

            return songInfo;
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

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)。
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                md5Hash.Dispose();
                // TODO: 将大型字段设置为 null。
                OsuDB = null;
                Favorites = null;

                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        ~OsuLocalDataGeter()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(false);
        }

        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
             GC.SuppressFinalize(this);
        }
        #endregion

    }
}
