using ImageBasic;
using PlayProjectGame.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using VisualAttentionDetection.OtherAppy;
namespace PlayProjectGame.Helper
{
    struct MatchBtStream
    {
        public string type;
        public int StartIndex;
        public int EndIndex;
        public byte[] buff;
        public int length;//apic指定的长度或者其他手动确定的长度
        
    }
    
    /// <summary>
    /// 处理歌曲TAG相关的类,不需要数据上下文的定义为静态
    /// </summary>
    class MusicTag : IDisposable
    {
        
        /// <summary>
        /// 用于存放临时数据
        /// </summary>
        MatchBtStream SEstreame;
        WebClient web;
        object lockobj=new object();
        public MusicTag()
        {
            SEstreame.EndIndex = SEstreame.StartIndex = -1;
        }

        /// <summary>
        /// 获取歌曲中前1Mbt中包含的图片，并返回
        /// </summary>
        public BitmapImage GetJPGFromStream(SongInfo songInfo, int PixelWidth = 0, int PixelHeight = 0)
        {
            MemoryStream ms = null;
            try
            {
                BitmapImage image = new BitmapImage();
                byte[] buffer = GetJPGBuffer(songInfo, 10240);
                if (buffer != null)
                    ms = new MemoryStream(buffer);
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                try
                {
                    try
                    {
                        image.EndInit();
                    }
                    catch (NotSupportedException)
                    {
                        MemoryStream saveStream = new MemoryStream();
                        if (!BasicMethodClass.MakeThumbnail(ms, saveStream, 602, 602, "W", "jpg"))
                            return null;
                        saveStream.Seek(0, SeekOrigin.Begin);
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = saveStream;
                        image.EndInit();
                        saveStream.Dispose();
                    }

                }
                catch (Exception)
                {
                    return null;
                }

                if (PixelWidth != 0)
                    image.DecodePixelWidth = PixelWidth;
                if (PixelHeight != 0)
                    image.DecodePixelHeight = PixelHeight;

                return image;
            }
            catch { return null; }
            finally {
                if (ms != null) ms.Dispose();
            }
        }
        /// <summary>
        /// 获取主色调
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static Color GetMajorColor(Bitmap bitmap)
        {
            Rectangle R;
            R = new System.Drawing.Rectangle(0, 0, 256, 256);
            MemoryStream ms_out = new MemoryStream();
            BasicMethodClass.MakeThumbnail(bitmap, ms_out, 600, 200, "W", "jpg");
            var srcBitmap = new Bitmap(ms_out);


            //var DH = new DominantHue(srcBitmap).GetDominantHue(0.5, 0.65, 210);
            var DH = new DominantHue(srcBitmap).GetDominantHue(0.4, 0.55, 50);
            ms_out.Dispose();
            srcBitmap.Dispose();
            return DH;
        }
        /// <summary>
        /// 获取歌曲中前1Mbt中包含的图片，并返回
        /// </summary>
        /// <param name="filepath">音乐文件路径</param>
        /// <returns>流</returns>
        public byte[] GetJPGBuffer(SongInfo songInfo, int firstReadLength = 1050000)
        {
            string path = Core.Cach.CachPool.GetAbPath(songInfo);
            if (GlobalConfigClass._Config.UseAlbumImageCach && File.Exists(path)) {
                lock (MainWindow.CurMainWindowInstence.cachobj)
                {
                    try
                    {
                        return File.ReadAllBytes(path);//这里怎么会有占用异常呢。。。
                    }
                    catch (IOException) 
                    {
                        return new byte[10];
                    }
                }
                // byte[] buffer;
                //using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                //{
                //    buffer = new byte[(int)fs.Length];

                //   var reslut= fs.BeginRead(buffer, 0, buffer.Count(),(obj)=> { },null);
                //    fs.EndRead(reslut);
                   
                //}
                //return buffer;    
             
            }
            else
            {
                if (!File.Exists(songInfo.SongPath))
                {
                    return null;
                }
                byte[] abbuff;
                FileInfo Finfo = new FileInfo(songInfo.SongPath);
                //Debug.Print(Finfo.Extension);
                using (FileStream fs = Finfo.OpenRead())
                {
                    if (Finfo.Extension != ".mp3")
                    {
                        abbuff = GetJPGBufferFromOtherFile(fs);

                    }
                    else
                    {
                        abbuff = GetJPGBufferFromMp3File(fs, 10240);
                        if (abbuff == null)
                        {
                            fs.Seek(0, SeekOrigin.Begin);
                            fs.Seek(0, SeekOrigin.Current);
                            abbuff = GetJPGBufferFromOtherFile(fs);
                        }

                    }
                    if (GlobalConfigClass._Config.UseAlbumImageCach && !File.Exists(path))
                        Core.Cach.CachPool.SetAb(songInfo, abbuff);
                    return abbuff;
                    ///MP3:ID3
                    ///fLAC:http://www.360doc.com/content/14/0828/14/58747_405338807.shtml
                    ///PNG:http://www.360doc.com/content/11/0428/12/1016783_112894280.shtml
                }
            }
        }
        public Bitmap GetBitmapFromBuffer(byte[] buffer)
        {
            var ms = new MemoryStream(buffer);
            try
            {
                var b = new Bitmap(ms);
                //ms.Dispose();
                return b;
            }
            catch { return new Bitmap(128, 128); }

        }


        private byte[] GetJPGBufferFromMp3File(FileStream fs, int firstReadLength = 1050000)
        {
            byte[] FileBtBuff = new byte[firstReadLength];
            fs.Read(FileBtBuff, 0, firstReadLength);
            int curIndex, length;
            if (Tag_GetIDv2ImageLength(FileBtBuff, out curIndex, out length))
            {
                if (firstReadLength < length + curIndex)
                {
                    FileBtBuff = new byte[length + curIndex];
                    fs.Seek(curIndex, SeekOrigin.Begin);
                    fs.Read(FileBtBuff, 0, length + curIndex);
                }//如果首次读取数量不足再读取
                SEstreame.length = length;
                SEstreame.StartIndex = curIndex;
                return Tag_GetIDv2ImageBuffer(FileBtBuff, SEstreame);
            }
            else return File.ReadAllBytes(@"./AppResource/notfind.png");
            ;

        }
        private byte[] GetJPGBufferFromOtherFile(FileStream fs)
        {
            byte[] FileBtBuff = new byte[2048576];
            fs.Read(FileBtBuff, 0, 2048575);
            int i = 0;
            for (i = 0; i < 10240; i++)
            {
                if (FileBtBuff[i + 1] == 0xd8 &&
                    FileBtBuff[i] == 0xff && FileBtBuff[i + 2] == 0xff &&
                    FileBtBuff[i + 3] == 0xe0)
                {//找Jpg文件头
                    SEstreame.StartIndex = i;
                    SEstreame.type = "jpg";
                    break;
                }
                if (FileBtBuff[i] == 0x89 && FileBtBuff[i + 1] == 0x50 && FileBtBuff[i + 2] == 0x4e)
                {//找PNG文件头
                    SEstreame.StartIndex = i;
                    SEstreame.type = "png";

                    break;
                }
            }

            if (SEstreame.StartIndex == -1) return null;
            if (SEstreame.type == "jpg")
            {
                for (; i < 2048575; i++)
                {
                    if (FileBtBuff[i + 1] == 0xd9 && FileBtBuff[i] == 0xff)
                    {//找Jpg文件尾
                     //if (SEstreame.type != "jpg") continue;
                        SEstreame.EndIndex = i;
                        break;
                    }
                }
            }
            if (SEstreame.type == "png")
                for (; i < 2048575; i++)
                {
                    if (FileBtBuff[i] == 0x49 &&
                            FileBtBuff[i + 1] == 0x44 &&
                            FileBtBuff[i + 2] == 0x41 &&
                            FileBtBuff[i + 3] == 0x54)
                    {
                        var s = FileBtBuff[i - 4] << 24
                 | FileBtBuff[i - 3] << 16
                 | FileBtBuff[i - 2] << 8
                 | FileBtBuff[i - 1];
                        i += s + 11;
                    }
                    if (FileBtBuff[i] == 0x42 && FileBtBuff[i + 1] == 0x60 && FileBtBuff[i + 2] == 0x82)
                    {//找Png文件尾
                     // if (SEstreame.type != "png") continue;
                        SEstreame.EndIndex = i;
                        break;
                    }
                }

            if (SEstreame.EndIndex != -1 && SEstreame.StartIndex != -1)
            {//提出文件
                int j = 0;
                SEstreame.buff = new byte[SEstreame.EndIndex - SEstreame.StartIndex + 1];
                for (i = SEstreame.StartIndex; i <= SEstreame.EndIndex; i++)
                {
                    SEstreame.buff[j++] = FileBtBuff[i];
                }
                return SEstreame.buff;
            }
            else
            {//
                return File.ReadAllBytes(@"./AppResource/notfind.jpg");
            }
        }
        private bool Tag_GetIDv2ImageLength(byte[] FileBtBuff, out int curIndex, out int length)
        {
            curIndex = 0; length = 0;
            var maxlength = FileBtBuff.Count() > 10240 ? 10240 : FileBtBuff.Count();
            if (FileBtBuff[0] == 0x49 && FileBtBuff[1] == 0x44 && FileBtBuff[2] == 0x33)//id3
                for (int i = 0; i < maxlength - 4; i++)
                {
                    if (FileBtBuff[i] == 0x41 &&//A
                        FileBtBuff[i + 1] == 0x50 &&//P
                        FileBtBuff[i + 2] == 0x49 &&//I
                        FileBtBuff[i + 3] == 0x43)//C
                    {
                        var s = FileBtBuff[i + 4] << 24//4字节的大小
                        | FileBtBuff[i + 5] << 16
                        | FileBtBuff[i + 6] << 8
                        | FileBtBuff[i + 7];
                        curIndex = i + 8;
                        length = s;
                        return true;
                    }
                }
            return false;
        }
        private byte[] Tag_GetIDv2ImageBuffer(byte[] FileBtBuff, MatchBtStream mbs) {
            int i = SEstreame.StartIndex + 1;
            //寻找附近的图片文件头
            for (; i < 1024 + SEstreame.StartIndex; i++)
            {
                if (FileBtBuff[i + 1] == 0xd8 &&
                    FileBtBuff[i] == 0xff && FileBtBuff[i + 2] == 0xff &&
                    FileBtBuff[i + 3] == 0xe0)
                {//找Jpg文件头
                    mbs.StartIndex = i;
                    mbs.type = "jpg";
                    break;
                }
                if (FileBtBuff[i] == 0x89 && FileBtBuff[i + 1] == 0x50 && FileBtBuff[i + 2] == 0x4e)
                {//找PNG文件头
                    mbs.StartIndex = i;
                    mbs.type = "png";
                    break;
                }
            }
            if (mbs.StartIndex == -1) return null;
            //寻找文件尾

            if (mbs.type == "jpg")
            {
                i += mbs.length - 16;
                for (; i < FileBtBuff.Count() - 1; i++)
                {
                    if (FileBtBuff[i + 1] == 0xd9 && FileBtBuff[i] == 0xff)
                    {//找Jpg文件尾
                        mbs.EndIndex = i;
                        break;
                    }
                }
            }
            else if (mbs.type == "png")
            {
                i += mbs.length - 20;

                for (; i < FileBtBuff.Count() - 1; i++)
                {
                    //   if (FileBtBuff[i] == 0x49 &&
                    //           FileBtBuff[i + 1] == 0x44 &&
                    //           FileBtBuff[i + 2] == 0x41 &&
                    //           FileBtBuff[i + 3] == 0x54)
                    //   {
                    //       var s = FileBtBuff[i - 4] << 24
                    //| FileBtBuff[i - 3] << 16
                    //| FileBtBuff[i - 2] << 8
                    //| FileBtBuff[i - 1];
                    //       i += s + 11;
                    //   }
                    if (FileBtBuff[i] == 0x42 && FileBtBuff[i + 1] == 0x60 && FileBtBuff[i + 2] == 0x82)
                    {//找Png文件尾
                        mbs.EndIndex = i;
                        break;
                    }
                }
            }
            else
            {
                if (SEstreame.StartIndex != -1)
                {
                    SEstreame.StartIndex = -1;
                    return Tag_GetIDv2ImageBuffer(FileBtBuff, mbs);

                }
            }
            //提出文件
            if (mbs.EndIndex != -1 && mbs.StartIndex != -1)
            {
                int j = 0;
                mbs.buff = new byte[mbs.EndIndex - mbs.StartIndex + 1];
                for (i = mbs.StartIndex; i <= mbs.EndIndex; i++)
                {
                    mbs.buff[j++] = FileBtBuff[i];
                }
                return mbs.buff;
            }
            else
            {//
                return File.ReadAllBytes(@"./AppResource/notfind.jpg");
                return null;
            }

        }

        private Task<byte[]> GetCurMusicFileJPGBufferTask;
        public Task<byte[]> GetCurrentMusicFileJPGBufferAsync(SongInfo song, bool ShouldReset = false) {
            if (ShouldReset || GetCurMusicFileJPGBufferTask == null || GetCurMusicFileJPGBufferTask.Result == null)
            {
                GetCurMusicFileJPGBufferTask = Task.Run<byte[]>(() =>
                {
                    if (song.SongType == 3)
                    {
                        var osubgPath = OsuLocalDataGeter.LoadOsuFileNameAs(1, song.OsuPath);
                        if (osubgPath == "") return null;
                        return File.ReadAllBytes(OsuLocalDataGeter.LoadOsuFileNameAs(1, song.OsuPath));
                    }
                    else
                        return GetJPGBuffer(song);
                });
            }
            return GetCurMusicFileJPGBufferTask;
        }

        private Task<Color> GetCurrentColorTask;
        public Task<Color> GetCurrentColorAsync(SongInfo song, bool ResetClor = false, bool ResetAll = false)
        {
            if (ResetAll || ResetClor || GetCurrentColorTask == null)
            {
                GetCurrentColorTask = Task.Run<Color>(() =>
                {
                    Color MajorColor;
                    if (song.SongType == 3)
                    {
                        try
                        {
                            var src = new Bitmap(OsuLocalDataGeter.LoadOsuFileNameAs(1, song.OsuPath));
                            //会出现不合法地址的情况，待改。

                            MajorColor = GetMajorColor(src);
                            src.Dispose();
                        }
                        catch { Debug.Print("加载OSU背景图片失败"); MajorColor = Color.Black; }
                    }
                    else
                    {
                        GetCurrentMusicFileJPGBufferAsync(song, ResetAll).Wait();
                        if (GetCurMusicFileJPGBufferTask.Result == null)
                            return Color.Black;
                        Bitmap sorc = GetBitmapFromBuffer(
                                GetCurMusicFileJPGBufferTask.Result);
                        MajorColor = GetMajorColor(sorc);
                        sorc.Dispose();
                    }

                    return MajorColor;
                });
            }
            return GetCurrentColorTask;
        }



        /// <summary>
        /// 【弃用】获取FFT采样数据，返回512个浮点采样数据
        /// </summary>
        /// <returns></returns>
        public static float[] GetFFTData(int stream)
        {
            float[] fft = new float[512];
            //   Bass.BASS_ChannelGetData(stream, fft, (int)BASSData.BASS_DATA_FFT1024);
            return fft;
        }

        public TagLib.Tag GetAudioTrack(string audioFilePath)
        {
            /*
             // assume you've got to the point where you have a StorageFile 
            // via a file picker or something similar

            var fileStream = await (StorageFile)file.OpenStreamForReadAsync();

            var tagFile = TagLib.File.Create(new StreamFileAbstraction(file.Name,
				             fileStream, fileStream);

            var tags = tagFile.GetTag(TagTypes.Id3v2);

            Debug.WriteLine(tags.Title);
             */
            FileStream fileStream = new FileStream(audioFilePath,FileMode.Open);
            var tagFile = TagLib.File.Create(new TagLib.StreamFileAbstraction(audioFilePath,
                 fileStream, fileStream));

            var tags = tagFile.GetTag(TagLib.TagTypes.Id3v2);

            return tags;
        }

        /// <summary>
        /// 异步获取网络图片，使用时务必重写BitmapImage参数的完成事件
        /// </summary>
        /// <param name="uri">网络uri</param>
        /// <param name="FunImageDownloadCompleted">完成时运行的函数</param>
        /// <returns></returns>
        public string GetAlbumImageSource(string uri, System.ComponentModel.AsyncCompletedEventHandler FunImageDownloadCompleted)
        {

            Uri uri2 = new Uri(uri, UriKind.Absolute);
            string filename = uri2.Segments[uri2.Segments.Count() - 1];
            web = new WebClient();
            web.DownloadFileCompleted += FunImageDownloadCompleted;
            web.DownloadFileAsync(uri2, filename);
            return filename;
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
                    web.Dispose();
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                // TODO: 将大型字段设置为 null。

                disposedValue = true;
            }
        }

        // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
        // ~MusicTag() {
        //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
        //   Dispose(false);
        // }

        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
            // GC.SuppressFinalize(this);
        }
        #endregion
    }

}

