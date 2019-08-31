using Afar.Cloud163;
using PlayProjectGame.Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PlayProjectGame.Data
{
    /// <summary>
    /// 网易云音乐网路数据源
    /// </summary>
    class NetCloudMusicSource
    {
        static NetCloudMusicSource netCloudMusicSource = null;
        static NeteaseMusicAPI neteaseMusicAPI = null;
        NetCloudMusicSource() {
            neteaseMusicAPI = new NeteaseMusicAPI();
        }
        public static NetCloudMusicSource GetCloudMusicSource() {
            if (netCloudMusicSource == null)
                netCloudMusicSource = new NetCloudMusicSource();
            return netCloudMusicSource;
        }


        public byte[] GetSongListImage(string Imageurl) {
            Playlist Playlist=new Playlist();
            try
            {
                var web = new WebClient();
                var byt=web.DownloadData(Imageurl);
                web.Dispose();
                return byt;
            }
            catch (Exception e) {
                Debug.Print(e.Message);
            }
            return null;
        }
    }
}
