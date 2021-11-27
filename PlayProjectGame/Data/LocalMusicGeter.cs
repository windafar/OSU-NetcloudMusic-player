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
        public LocalMusicGeter(IEnumerable<SongInfoExpend> songInfoExpends, string userName, string songListinfo, string songListName)
        {
            SongInfoExpends = songInfoExpends;
            UserName = userName;
            SongListinfo = songListinfo;
            SongListName = songListName;
        }
        UserData LocalUserData;
        public IEnumerable<SongInfoExpend> SongInfoExpends { get; }
        public string UserName { get; }
        public string SongListinfo { get; }
        public string SongListName { get; }

        public UserData GetLocalUserData ()
        {

            if (File.Exists(GlobalConfigClass.XML_LOCALMUSIC_SAVEPATH))
            {
                LocalUserData = ReadUserDataByXML();
                LocalUserData.Pids.Insert(0, CreateLocalSongList().Pids.First());


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

        private UserData CreateLocalSongList()
        {
            UserData NewLocalUserData = new UserData() { Pids = new List<PlayListData>(), Username = UserName, Uid = Guid.NewGuid().ToString() };
            NewLocalUserData.Pids.Add(new PlayListData
            {
                PlatListName = SongListName,
                Songs = SongInfoExpends.ToList(),
                PlayListInfo = SongListinfo,
                UersName = UserName,
                UserId = NewLocalUserData.Uid,
                PlayListType = 1,
                PlayListId = Guid.NewGuid().ToString()
            });
            return NewLocalUserData;
        }

        public void backup()
        {
            if (LocalUserData == null) throw new InvalidOperationException("备份失败，NetClouldMusicData 对象为空");

            OtherHelper.WriteXMLSerializer(LocalUserData, typeof(UserData), GlobalConfigClass.XML_LOCALMUSIC_SAVEPATH + "_back");

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
