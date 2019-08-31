///弃掉bass实现

//using AddIn.Audio;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Un4seen.Bass;
//using Un4seen.Bass.AddOn.Flac;

//namespace AddIn.Audio
//{
//    public class BassPlayer : IAudioPlayer
//    {
//        public static IAudioPlayer GetBassPlayer()
//        {
//            BassPlayer bassplayer = new BassPlayer();
//            bassplayer.audioPlayer = bassplayer;
//            return bassplayer;
//        }
//        /// <summary>
//        /// 当前流
//        /// </summary>
//        private int stream;

//        BassPlayer()
//        {
//            Un4seen.Bass.BassNet.Registration("470046850@qq.com", "2X33736160022");
//            if (!Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, System.IntPtr.Zero))
//                throw new Exception("Failed to initialize: " + Bass.BASS_ErrorGetCode().ToString());

//        }

//        private IAudioPlayer audioPlayer;
//        public IAudioPlayer AudioPlayer
//        {
//            get
//            {
//                return audioPlayer;
//            }
//        }

//        string currentPlayFileName;

//        public string CurrentPlayFileName
//        {
//            get
//            {
//                return currentPlayFileName;
//            }
//        }

//        public TimeSpan Position
//        {
//            get
//            {
//                long pos = Bass.BASS_ChannelGetPosition(stream);
//                double etiem = Bass.BASS_ChannelBytes2Seconds(stream, pos);
                

//                return TimeSpan.FromSeconds(etiem);
//            }

//            set
//            {
//                long pos = Bass.BASS_ChannelSeconds2Bytes(stream,value.TotalSeconds);
//                Bass.BASS_ChannelSetPosition(stream, pos);
//            }
//        }

//        public int Volume
//        {
//            get
//            {
//                throw new NotImplementedException();
//            }

//            set
//            {
//                throw new NotImplementedException();
//            }
//        }

//        public TimeSpan TotalTime
//        {
//            get
//            {
//               long Maxpos = Bass.BASS_ChannelGetLength(stream);
//               double totaltime = Bass.BASS_ChannelBytes2Seconds(stream, Maxpos);
//                TimeSpan t = TimeSpan.FromSeconds(totaltime);
//                return t;
//            }
//        }

//        bool IAudioPlayer.IsPause
//        {
//            get
//            {
//                return IsPause;
//            }
//        }

//        public void Dispose()
//        {
//            Bass.BASS_Free();
//        }

//        public void Open(string pFilePath)
//        {
//            if (pFilePath == null)
//                throw new NullReferenceException();
//            this.currentPlayFileName = pFilePath;
//            if (stream != 0)
//            {
//            //    IsPause = false;
//                Bass.BASS_StreamFree(stream);
//            }
//            //flac音乐解码
//            stream = BassFlac.BASS_FLAC_StreamCreateFile(pFilePath, 0, 0, BASSFlag.BASS_SPEAKER_FRONT);
//            if (stream != 0)
//            {
//                Bass.BASS_Start();
//                return;
//            }
//            //MP3解码
//            stream = Bass.BASS_StreamCreateFile(pFilePath, 0, 0, BASSFlag.BASS_SAMPLE_FLOAT);
//            if (stream != 0)
//            {
//                Bass.BASS_Start();
//                return;
//            }
//        }

//        public bool IsPause;
//        TimeSpan NotePauseTime;
//        public void Pause()
//        {
//            NotePauseTime = Position;
//            Bass.BASS_ChannelPause(stream);
//            IsPause = true;
//        }

//        public void Play()
//        {
//            Bass.BASS_ChannelPlay(stream, true);
//            if (IsPause)
//                Position = NotePauseTime;
//            IsPause = false;
//        }
//        public void Stop()
//        {
//            Bass.BASS_ChannelStop(stream);
//        }
//    }
//}
