using System;
using System.Collections.Generic;

namespace AddIn.Audio
{
    public delegate void PlaybackStoppedDele(IAudioPlayer audioPlayer);
    public delegate void PlaybackContiuneDele(TimeSpan playPosition);

    public interface IAudioPlayer : IDisposable
    {
        /// <summary>
        /// 音量大小百分比
        /// </summary>
        int Volume { get; set; }

        /// <summary>
        /// 当前播放位置
        /// </summary>
        TimeSpan Position { get; set; }

        TimeSpan TotalTime { get; }

        /// <summary>
        /// 播放当前活动音频
        /// </summary>
        void Play();

        /// <summary>
        /// 暂停播放
        /// </summary>
        void Pause();

        /// <summary>
        /// 停止播放
        /// </summary>
        void Stop();

        /// <summary>
        /// 加载音频，使其成为当前活动音频
        /// </summary>
        /// <param name="pFilePath"></param>
        void Open(string pFilePath, int Volume = 50, string openMethods = "waveout", string device = "扬声器");

        /// <summary>
        /// 获取当前实例
        /// </summary>
        IAudioPlayer AudioPlayer
        {
            get;
        }

        /// <summary>
        /// 获取当前播放文件名
        /// </summary>
        string CurrentPlayFileName { get; }

        bool IsPause { get; }

        string CurrentPlayerName { get; }

        //PlaybackStoppedDele PlaybackStopped { get; set; }

        //PlaybackContiuneDele PlaybackContiune { get; set; }

        List<string> GetAsioOutDeviceName();
        List<string> GetDirectSoundOutDeviceName();
        List<string> GetWasApiDeviceName();
        List<string> GetWaveOutDeviceName();
        IEnumerable<KeyValuePair<string, string>> GetCodecInfo(string songpath,string tag);
    }
}