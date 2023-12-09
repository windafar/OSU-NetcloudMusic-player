using Microsoft.VisualBasic.Devices;
using PlayProjectGame.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PlayProjectGame.Data
{
    class LocalMusicGeter
    {
        public LocalMusicGeter(string userName)
        {
            UserName = userName;
        }
        UserData LocalUserData;

        public string UserName { get; }

        public UserData GetLocalUserData ()
        {

            if (File.Exists(GlobalConfigClass.XML_LOCALMUSIC_SAVEPATH))
            {
                LocalUserData = ReadUserDataByXML();
                


            }
            else
            {
                LocalUserData = CreateLocalSongList();

            }
            backup();
            return LocalUserData;
        }

        private UserData ReadUserDataByXML()
        {
            if (File.Exists(GlobalConfigClass.XML_LOCALMUSIC_SAVEPATH))
            {
                XmlSerializer xs = new XmlSerializer(typeof(UserData));
                try
                {
                    FileStream fs = File.OpenRead(GlobalConfigClass.XML_LOCALMUSIC_SAVEPATH);
                    UserData Data = (UserData)xs.Deserialize(fs);
                    fs.Dispose();
                    return Data;
                }
                catch (Exception e)
                {
                    System.Windows.MessageBox.Show("此时无法从序列化的文件加载歌单：" + e.Message, "xml文件加载");
                    return null;
                }
            }
            else return null;
        }

        private UserData CreateLocalSongList()
        {
            UserData userData = ReadUserDataByXML();
            
            if (userData != null) return userData;
            List<string> filesys = new List<string>();
            var DriveNames = ConfigPage.GlobalConfig.MatchSongFromDivce.Split(new char[1] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var driveInfo in DriveInfo.GetDrives())
            {
                if (driveInfo.IsReady)
                    filesys.AddRange(FileSerch.FileSercher.EnumerateFiles(driveInfo));
            }
            filesys = filesys.Where(x =>
            {
                int index = x.LastIndexOf(".");
                
                string Extension = x.Substring(index + 1, x.Length - index - 1);
                if (Extension == "mp3" ||
                Extension == "flac"
                //  Extension == "wav" ||
                // Extension == "ncm";
                ) {
                    FileInfo fileinfo = new FileInfo(x);
                    if (fileinfo.Length < 1024 * 1024 * 2) return false;
                    return true;
                }
                return false;
            }).Distinct().OrderBy(x => new FileInfo(x).LastWriteTime).ToList();

            UserData NewLocalUserData = new UserData() { Pids = new List<PlayListData>(), Username = UserName, Uid = Guid.NewGuid().ToString() };
            NewLocalUserData.Pids.Add(new PlayListData
            {
                PlatListName = "本地歌曲",
                Songs = filesys.Select(x=>new SongInfoExpend {
                    SongInfo=new SongInfo {
                        SongPath=x,
                        SongName=new FileInfo(x).Name.Split('-').Length==2? new FileInfo(x).Name.Split('-')[1]: new FileInfo(x).Name,
                        SongArtist= new FileInfo(x).Name.Split('-').Length == 2 ? new FileInfo(x).Name.Split('-')[0] : "",
                        SongId =DateTime.Now.Ticks.ToString(),
                        SongType=1
                    },
                }).ToList(),
                PlayListInfo = "计算机上的所有歌曲",
                UersName = UserName,
                UserId = UserName,
                PlayListType = 1,
                PlayListId = Guid.NewGuid().ToString()
            });
            return NewLocalUserData;
        }

        public void backup()
        {
            if (LocalUserData == null) throw new InvalidOperationException("备份失败，NetClouldMusicData 对象为空");

            OtherHelper.WriteXMLSerializer(LocalUserData, typeof(UserData), GlobalConfigClass.XML_LOCALMUSIC_SAVEPATH);

            if (File.Exists(GlobalConfigClass.XML_LOCALMUSIC_SAVEPATH))
                File.Delete(GlobalConfigClass.XML_LOCALMUSIC_SAVEPATH);
            OtherHelper.WriteXMLSerializer(LocalUserData, typeof(UserData), GlobalConfigClass.XML_LOCALMUSIC_SAVEPATH);

        }
        static public UserData GetLocalUserDataExited() 
        {
            if (File.Exists(GlobalConfigClass.XML_LOCALMUSIC_SAVEPATH))
            {
                XmlSerializer xs = new XmlSerializer(typeof(List<UserData>));
                try
                {
                    FileStream fs = File.OpenRead(GlobalConfigClass.XML_LOCALMUSIC_SAVEPATH);
                    UserData Data = (UserData)xs.Deserialize(fs);
                    fs.Dispose();
                    return Data;
                }
                catch (Exception e)
                {
                    System.Windows.MessageBox.Show("此时无法从序列化的文件加载歌单：" + e.Message, "xml文件加载");
                    return null;
                }

            }
            else return null;
        }

    }
}
