using CSCore;
using CSCore.Codecs;
using CSCore.CoreAudioAPI;
using CSCore.DirectSound;
using CSCore.SoundOut;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace AddIn.Audio
{
    /// <summary>
    /// 音频播放器的实现，采用CSCore内核
    /// </summary>
    public class CSCorePlayer : MarshalByRefObject, IAudioPlayer
    {
        static public IAudioPlayer GetCSCorePlayer(string openMethods = null)
        {

            CSCorePlayer cscp = new CSCorePlayer();
            cscp.audioPlayer = cscp;
            return cscp;
        }

        /// <summary>
        /// 使用默认方式初始化<see cref="CSCorePlayer"/>实例
        /// </summary>
        private CSCorePlayer()
        {
        }

        ~CSCorePlayer()
        {
            Dispose(false);
        }

        private string _filePath;
        private ISoundOut _soundOut;
        private IWaveSource _waveSource;

        public event EventHandler<PlaybackStoppedEventArgs> PlaybackStopped
        {
            add
            {
                if (_soundOut != null)
                    _soundOut.Stopped += value;
            }
            remove
            {
                if (_soundOut != null)
                    _soundOut.Stopped -= value;
            }
        }


        #region Properties

        public PlaybackState PlaybackState
        {
            get
            {
                if (_soundOut != null)
                    return _soundOut.PlaybackState;
                return PlaybackState.Stopped;
            }
        }

        public TimeSpan Position
        {
            get
            {
                if (_waveSource != null)
                    return _waveSource.GetPosition();
                return TimeSpan.Zero;
            }
            set
            {
                if (_waveSource != null)
                {
                    _waveSource.SetPosition(value);
                }
            }
        }

        public TimeSpan Length
        {
            get
            {
                if (_waveSource != null)
                    return _waveSource.GetLength();
                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// 音量大小百分比
        /// </summary>
        public int Volume
        {
            get
            {
                if (_soundOut != null)
                    return Math.Min(100, Math.Max((int)(_soundOut.Volume * 100), 0));
                return 100;
            }
            set
            {
                if (_soundOut != null)
                {
                    _soundOut.Volume = Math.Min(1.0f, Math.Max(value / 100f, 0f));
                }
            }
        }

        CSCorePlayer audioPlayer;
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
                return _filePath;
            }
        }

        public TimeSpan TotalTime
        {
            get
            {
                return _total;
            }
        }

        public bool IsPause
        {
            get
            {
                return _soundOut.PlaybackState == PlaybackState.Paused;
            }
        }

        public string CurrentPlayerName =>"cscore";

        //public PlaybackStoppedDele PlaybackStopped { get; set; }

        //public PlaybackContiuneDele PlaybackContiune { set; get; }

        #endregion Properties

        #region Methods

        private void InitializePlayback(int Volume=50, string openMethods= "waveout", string device= "扬声器")
        {
            MMDevice mMDevice;
            device=device.Trim();
            openMethods = openMethods.Trim();
            if (openMethods.IndexOf("WaveOut") != -1)
            {
                IEnumerable<WaveOutDevice> dives = WaveOutDevice.EnumerateDevices();
                IEnumerable<WaveOutDevice> divselect = dives.Where(x => x.Name.IndexOf(device) != -1);
                WaveOutDevice div = null;
                if (divselect.Count() == 0)
                    div = dives.FirstOrDefault();
                else if (divselect.Count() == 1)
                    div = divselect.FirstOrDefault();
                else
                {
                    Debug.Print("*****输入异常");
                    div = divselect.FirstOrDefault();
                }
                    if (div == null) throw new NotSupportedException("not exist directsound device");
                _soundOut = new WaveOut() { Device= div,Latency=100 };//300延时有个运算溢出，怀疑是其他异常造成的

            }
            else if (openMethods.IndexOf("WasApiOut") != -1)
            {
                var enumerator = new MMDeviceEnumerator();
                IEnumerable<MMDevice> mMDevices = MMDeviceEnumerator.EnumerateDevices(DataFlow.Render).Where(x=>x.DeviceState== DeviceState.Active);
                IEnumerable<MMDevice> dives = enumerator.EnumAudioEndpoints(DataFlow.All, DeviceState.All).Where(x=>x.DeviceState== DeviceState.Active);
                mMDevices = mMDevices.Join(dives, x => x.FriendlyName, x => x.FriendlyName, (x, y) => x).ToArray();
                mMDevice = mMDevices.Where(x => x.FriendlyName.IndexOf(device) != -1).FirstOrDefault(x=>x.DeviceState== DeviceState.Active);
                _soundOut = new WasapiOut() { Device = mMDevice, Latency = 200 };

            }
            else
            {
                IEnumerable<DirectSoundDevice> dives = DirectSoundDeviceEnumerator.EnumerateDevices();
                var divselect = dives.Where(x => x.Description.IndexOf(device) != -1);
                DirectSoundDevice div = null;
                if (divselect.Count() == 0)
                    div = dives.FirstOrDefault();
                else if (divselect.Count() == 1)
                    div = divselect.FirstOrDefault();
                else
                {
                    //Debug.Print("*****输入异常*****");
                    div = divselect.FirstOrDefault();
                }
                if (div == null) throw new NotSupportedException("not exist directsound device");
                _soundOut = new DirectSoundOut() { Device = div.Guid, Latency = 100 };

            }


            if (_filePath.LastIndexOf(".mp3") != -1)//流异步读取,此api异步读取flac流在频繁pos时有死锁bug
            {
                Stream fs = File.OpenRead(_filePath);
                _waveSource = new CSCore.Codecs.MP3.Mp3MediafoundationDecoder(fs);
            }
            else if (_filePath.LastIndexOf(".flac") != -1)
            {
                Stream fs = File.OpenRead(_filePath);
                _waveSource = new CSCore.Codecs.FLAC.FlacFile(fs, CSCore.Codecs.FLAC.FlacPreScanMode.Default);
               // _waveSource = new CSCore.Codecs.FLAC.FlacFile(_filePath);
            }
            else
                _waveSource = CodecFactory.Instance.GetCodec(_filePath);
            lock (obj_soundOut)
            {
                if (_soundOut == null) return;//奇怪，为啥锁了还能是空的，暂时不管了
                _soundOut.Initialize(_waveSource);
                _soundOut.Volume = Volume / 100f;
            }
            //_soundOut.Stopped += _soundOut_Stopped;
            _total = _waveSource.GetLength();
        }

        //private void _soundOut_Stopped(object sender, PlaybackStoppedEventArgs e)
        //{
        //    //PlaybackStopped?.Invoke(audioPlayer);
        //}

        public void Open(string pFilePath, int Volume = 50, string openMethods = "waveout", string device = "扬声器")
        {
            CleanupPlayback();
            _filePath = pFilePath;
            try
            {
                    InitializePlayback(Volume, openMethods, device);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

            }
            }

        public IEnumerable<KeyValuePair<string, string>> GetCodecInfo(string songpath) 
        {
            var i = CodecFactory.Instance.GetCodec(songpath);
            var w = i.WaveFormat;
            i.Dispose();
           return w.GetType().GetProperties().Select(x => new KeyValuePair<string, string>(x.Name, x.GetValue(w).ToString())).ToList();
        }
        public IEnumerable<KeyValuePair<string, string>> GetCodecInfo(string songpath, string tag)
        {
            var i = CodecFactory.Instance.GetCodec(songpath);
            var w = i.WaveFormat;
            i.Dispose();
            return w.GetType().GetProperties().Where(x=>x.Name.ToLower()== tag|| string.IsNullOrWhiteSpace(tag)).Select(x => new KeyValuePair<string, string>(x.Name, x.GetValue(w).ToString())).ToList();
        }
        public void Play()
        {
            
            if (_soundOut != null)
            {
                _soundOut.Play();

            }
            else
            {
                throw new Exception("dot init");
            }
        }

        public void Pause()
        {
            if (_soundOut != null)
                _soundOut.Pause();
        }

        public void Stop()
        {
            if (_soundOut != null)
                _soundOut.Stop();
        }


        public List<string> GetWaveOutDeviceName()
        {
            List<CSCore.SoundOut.WaveOutDevice> list = new List<WaveOutDevice>();
            foreach (var dev in WaveOutDevice.EnumerateDevices())
            {
                list.Add(dev);
            }
            return list.Select(x => "WaveOut - " + x.Name).ToList();
        }

        public List<string> GetDirectSoundOutDeviceName()
        {
            List<DirectSoundDevice> list = new List<DirectSoundDevice>();
            foreach (var dev in DirectSoundDeviceEnumerator.EnumerateDevices())
            {
                list.Add(dev);
            }
            return list.Select(x => "DirectSoundOut - " + x.Description).ToList();
        }

        public List<string> GetWasApiDeviceName()
        {
            var enumerator = new MMDeviceEnumerator();

            var list = new List<MMDevice>();
            foreach (var wasapi in enumerator.EnumAudioEndpoints(DataFlow.All, DeviceState.All))
            {
                list.Add(wasapi);
            }
            return list.Where(x => x.DeviceState == DeviceState.Active).Select(x => "WasApiOut - " + x.FriendlyName).ToList();
        }

        public List<string> GetAsioOutDeviceName()
        {
            return new List<string>() { "None" };

        }


        #endregion Methods

        #region 接口：IDisposable

        private bool m_disposed;
        private TimeSpan _total;
        private readonly object objdispose=new object();
        private readonly object obj_soundOut = new object();

        private void CleanupPlayback()
        {
            lock (objdispose) {
                if (_soundOut != null)
                {

                    _soundOut.Dispose();
                    _soundOut = null;
                }
           
            if (_waveSource != null)
            {
                _waveSource.Dispose();
                _waveSource = null;
            } }
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
                CleanupPlayback();
                m_disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion 接口：IDisposable
    }
}