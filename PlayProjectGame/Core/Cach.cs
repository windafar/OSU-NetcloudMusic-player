using PlayProjectGame.Data;
using PlayProjectGame.Helper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PlayProjectGame.Core
{
    class Cach
    {
        static Cach() {
            if (!Directory.Exists("cach\\")) {
                Directory.CreateDirectory("cach");
            }

        }

        MD5 md5 = MD5.Create();
        static Cach cach;

        HashSet<string> caching = new HashSet<string>();

        static public Cach CachPool
        {
            get
            {
                if (cach == null)
                    return cach = new Cach();
                else return cach;
            }
        }

        public void SongDataIndex()
        {

        }

        #region 本地运行缓存，文件
        public string GetAbPath(SongInfo info)
        {
            string path = "cach\\" + OtherHelper.ReplaceValidFileName(info.SongAlbum + info.SongArtist)+ "_A";
            return path;
        }
        public string GetSDRImagePath(SongInfo info)
        {
            string path = "cach\\" + OtherHelper.ReplaceValidFileName(info.SongAlbum + info.SongArtist)+ "_S";
            return path;
        }
        public string GetPath(SongInfo info) {
            string path = "cach\\" + OtherHelper.ReplaceValidFileName(info.SongAlbum + info.SongArtist);
            return path;
        }
        public string GetAbPath(string path)
        {
            return path + "_A";
        }
        public string GetSDRImagePath(string path)
        {
            return path + "_S";
        }

        //显著图也加上缓存配置
        public void SetSDRImageSource(SongInfo info,Bitmap bitmap)
        {
            string path = GetSDRImagePath(info);
            if (!File.Exists(path))
                if (!caching.Contains(path))
                {
                    try
                    {
                        caching.Add(path);

                        bitmap.Save(path);
                    }
                    catch
                    {
                        if (caching.Contains(path))
                            caching.Remove(path);
                        return;
                    }
                    caching.Remove(path);
                }
        }
        public void SetAb(SongInfo info, byte[] buff)
        {
            string path = GetAbPath(info);
            if (!File.Exists(path))
                if (!caching.Contains(path))
                {
                    try
                    {
                        caching.Add(path);

                        File.WriteAllBytes(path, buff);
                    }
                    catch {
                        //滤掉残留的文件占用异常
                        if(caching.Contains(path))
                            caching.Remove(path);
                        return;
                    }
                    caching.Remove(path);
                }
        }
        #endregion

        #region 网络缓存，文件
        public void SetSongListImage(long Pid) {

        }
        #endregion
    }
}
