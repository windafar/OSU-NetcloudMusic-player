using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

namespace AddIn.Audio
{
    public class NAudioPlayer : MarshalByRefObject,IAudioPlayer
    {
        static public IAudioPlayer GetNAudioPlayer()
        {

            NAudioPlayer nap = new NAudioPlayer();
            nap.audioPlayer = nap;
            return nap;
        }
         NAudioPlayer()
        {
        }

         NAudioPlayer(string pFilePath) : this()
        {
            _filePath = pFilePath;
        }

        ~NAudioPlayer()
        {
            Dispose(false);
        }

        private string _filePath;
        private IWavePlayer wavePlayer;
        private WaveStream reader;
        private SampleChannel sampleChannel;
        public TimeSpan Position
        {
            get
            {
                return (null == wavePlayer || wavePlayer.PlaybackState == PlaybackState.Stopped || reader == null)
                    ? TimeSpan.Zero
                    //:TimeSpan.FromSeconds(((WaveOutEvent)wavePlayer).GetPosition()/ reader.WaveFormat.AverageBytesPerSecond);
                    : reader.CurrentTime;
            }
            set
            {
                if (null!=reader)
                {
                    
                    reader.CurrentTime = value;
                }
            }
        }

        //private System.Threading.Timer timer;


        private int _Volume=100;
        /// <summary>
        /// 音量大小百分比
        /// </summary>
        public int Volume
        {
            get
            {
               // return 100;
                return null == reader ? _Volume : (int)(sampleChannel.Volume * 100);
            }
            set
            {
                _Volume = value;
                if (null!=reader)
                {
                    sampleChannel.Volume = value / 100f;
                }
            }
        }
        IAudioPlayer audioPlayer;
        public IAudioPlayer AudioPlayer
        {
            get
            {
                return audioPlayer;
            }
        }

        public string CurrentPlayFileName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public TimeSpan TotalTime
        {
            get
            {
                if (reader == null)
                    return new TimeSpan();
                else
                   return reader.TotalTime;
            }
        }


        public bool IsPause
        {
            get
            {
                if (wavePlayer != null)
                    return wavePlayer.PlaybackState == PlaybackState.Paused;
                else return false;
            }
        }

        private void InitializePlayback()
        {
            wavePlayer = new WaveOutEvent() { DesiredLatency = 200 };
             //wavePlayer = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 200);

            //DesiredLatency用于处理新的buffer的加入，但过大则会有明显的延迟问题
            //1，可以使用另外的进程来实现以杜绝其他线程的抢占问题
            try
            {
                reader = new AudioFileReader(_filePath);//性能问题：0.2s-2.0s，加载文件时出现的问题，应该是首次的加载缓存时
                //reader = new StreamMediaFoundationReader(new MemoryStream(File.ReadAllBytes(_filePath)));
                //sampleChannel = new SampleChannel(reader, false);
            }
            catch
            {
                try
                {
                    if(reader!=null)reader.Dispose();

                    reader = new StreamMediaFoundationReader(new MemoryStream(File.ReadAllBytes(_filePath)));

                }
                catch (Exception)
                {
                    if (reader != null) reader.Dispose();

                    //MessageBox.Show("不支持此格式");
                    //throw new NotSupportedException();
                    return;
                }
            }
            //   reader.Volume = Volume;
            try
            {
                wavePlayer.Init(reader);
                //wavePlayer.PlaybackStopped += WavePlayer_PlaybackStopped;
            }
            catch (Exception ex)
            {
               // MessageBox.Show(ex.Message);
            }
        }

        private void WavePlayer_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            //PlaybackStopped(audioPlayer);
        }

        //private void InitializePlayback(Stream stream)
        //{
        //    // wavePlayer = new WaveOutEvent() { DesiredLatency = 200 };
        //    // wavePlayer = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Exclusive, 100);
        //    wavePlayer = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 200);
        //    //DesiredLatency用于处理新的buffer的加入，但过大则会有明显的延迟问题
        //    //1，可以使用另外的进程来实现以杜绝其他线程的抢占问题
        //    try
        //    {
        //        //reader = new AudioFileReader(_filePath);//性能问题：0.2s-2.0s，加载文件时出现的问题，应该是首次的加载缓存时
        //        reader = new StreamMediaFoundationReader(stream);
        //        sampleChannel = new SampleChannel(reader, false);
        //    }
        //    catch
        //    {
        //        //throw new NotSupportedException();
        //        //MessageBox.Show("不支持此格式");

        //    }
        //    //   reader.Volume = Volume;
        //    try
        //    {
        //        wavePlayer.Init(reader);
        //    }
        //    catch (Exception ex)
        //    {
        //        //MessageBox.Show(ex.Message);
        //    }
        //}

        public void Open(string pFilePath)
        {
            CleanupPlayback();
            _filePath = pFilePath;
            InitializePlayback();
        }

        public void Pause()
        {
            if (wavePlayer != null)
            {
                wavePlayer.Pause();
            }
        }

        public void Play()
        {
            if (wavePlayer != null)
            {

                wavePlayer.Play();

            }
            else
            {
                InitializePlayback();
                wavePlayer.Play();
            }
            //if (timer == null)
            //{
            //    timer = new System.Threading.Timer(delegate
            //    {
            //        if(wavePlayer.PlaybackState==PlaybackState.Playing)
            //            PlaybackContiune?.Invoke(Position);
            //    }, Position, 100, 20);
            //}
            //else timer.Change(100, 20);
        }

        public void Stop()
        {
            if (wavePlayer != null)
            {
                wavePlayer.Stop();
            }
        }

        public void GetDev() {
            for (int n = -1; n < WaveOut.DeviceCount; n++)
            {
                var caps = WaveOut.GetCapabilities(n);
                Console.WriteLine($"{n}: {caps.ProductName}");
            }
        }

        public List<string> GetWaveOutDeviceName()
        {
            List<WaveOutCapabilities> list = new List<WaveOutCapabilities>();
            for (int n = -1; n < WaveOut.DeviceCount; n++)
            {
                list.Add(WaveOut.GetCapabilities(n));
            }
            return list.Select(x => "WaveOut - " + x.ProductName).ToList();
        }

        public List<string> GetDirectSoundOutDeviceName()
        {
            List<DirectSoundDeviceInfo> list = new List<DirectSoundDeviceInfo>();
            foreach (var dev in DirectSoundOut.Devices)
            {
                list.Add(dev);
            }
            return list.Select(x => "DirectSoundOut - " + x.Description).ToList();
        }

        public List<string> GetWasApiDeviceName()
        {
            var enumerator = new MMDeviceEnumerator();

            var list = new List<MMDevice>();
            foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.All))
            {
                list.Add(wasapi);
            }
            return list.Where(x => x.State == DeviceState.Active).Select(x => "WasApiOut - " + x.FriendlyName).ToList();
        }

        public List<string> GetAsioOutDeviceName()
        {
            return AsioOut.GetDriverNames().Select(x => "AsioOut - " + x).ToList();
        }


        public string CurrentPlayerName => "naudio";

        //public PlaybackStoppedDele PlaybackStopped { get; set; }
        //public PlaybackContiuneDele PlaybackContiune { get; set; }


        //public PlaybackStopped playbackStopped => throw new NotImplementedException();

        //public PlaybackContiune PlaybackContiune => throw new NotImplementedException();


        #region 接口：IDisposable

        private bool m_disposed;

        private void CleanupPlayback()
        {
            if (wavePlayer != null)
                lock (wavePlayer)
                {
                    try
                    {
                        if (wavePlayer != null)
                        {
                            wavePlayer.Dispose();
                            wavePlayer = null;
                        }
                        if (reader != null)
                        {
                            reader.Dispose();
                            reader = null;
                        }
                    }
                    catch {
                        if(Thread.CurrentThread.ThreadState== ThreadState.Running)
                            Thread.CurrentThread.Abort();
                    }
                    }
        }

        public void Dispose()
        {

            try
            {
                Dispose(true);

            }
            catch
            {
            }
            finally {
                audioPlayer = null;
                reader = null;
            }

        }

        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (disposing)
                {
                    // Release managed resources
                }

                // Release unmanaged resources
                //timer.Dispose();
                //timer = null;
                CleanupPlayback();


                m_disposed = true;
            }
        }

        public void Open(string pFilePath, string openMethods = "waveout", string device = "扬声器")
        {
            throw new NotImplementedException();
        }

        public void Open(string pFilePath, int Volume = 50, string openMethods = "waveout", string device = "扬声器")
        {
            throw new NotImplementedException();
        }

        #endregion 接口：IDisposable

        ///https://github.com/naudio/NAudio/blob/master/Docs/OutputDeviceTypes.md
        /// 
        ///
    }
}