using AddIn.Audio;
using Communication;
using PlayProjectGame.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;

namespace PlayProjectGame.PlayList
{
    delegate void PlayingChanged(SongInfoExpend newValue);
    delegate void PlaybackContiune(TimeSpan position);
    class PlayListBase
    {
        //static public PlayListBase CurInstance;
        //static public IAudioPlayer player = BassPlayer.GetBassPlayer();
        //static public IAudioPlayer naudioplayer = NAudioPlayer.GetNAudioPlayer();
        //static public IAudioPlayer cscplayer = CSCorePlayer.GetCSCorePlayer();
        //static public IAudioPlayer player = naudioplayer;
        public static IAudioPlayer player = (IAudioPlayer)Communication.Communication.IPCCommunication.InitClient<CSCorePlayer>().getAudioObj();

        //public static IAudioPlayer player = CSCorePlayer.GetCSCorePlayer();
        public static PlayingChanged PlayingChanged;
        public static PlaybackContiune PlaybackContiune;
        ///以下4个量加个当前歌曲名称可以满足大部分需求，基本上不需要使用ipc来修改
        ///保持简单通信以后可能会搬掉进程的方式
        //static public long CurrentPlayTime;
        //static public long CurPlayTotalTime;
        //static public int CurrentVolum;
        //static public PlayState CurPlayState;
        static public SongInfoExpend CurSongInfoEx;
        //static public IPCCommunicationDataFromt CommunicationData = new IPCCommunicationDataFromt();

        static private double secx;//比例
        static public Timer timer = new Timer((TimerCallback)delegate
        {//这一块之后用消息改掉
            TimeSpan position;
            try
            {
                position = player.Position;
                secx = (position.TotalSeconds / player.TotalTime.TotalSeconds);
            }
            catch {
                PlayListIndex++;
                MainWindow.CurMainWindowInstence.Dispatcher.BeginInvoke((ThreadStart)delegate ()
                {
                    PlayListBase.PlayAndExceptionPass();
                });

                return;
            }
            MainWindow.CurMainWindowInstence.Dispatcher.BeginInvoke((ThreadStart)delegate ()
            {
                //这可能有问题，
                if (!double.IsNaN(secx))
                    MainWindow.CurMainWindowInstence.PalySlider.Value = secx * 10;//MainWindow.CurMainWindowInstence.PalySlider.Maximum;
            });
            if ((player.TotalTime - position).TotalSeconds < 1 && player.TotalTime.TotalSeconds > 0)
            {
                timer.Change(Timeout.Infinite, 50);
                PlayListIndex++;
                MainWindow.CurMainWindowInstence.Dispatcher.BeginInvoke((ThreadStart)delegate ()
                {
                    PlayListBase.PlayAndExceptionPass();
                });
            }
            PlaybackContiune?.Invoke(position);
        }, null, Timeout.Infinite, 100);
        static public Thread PlayThread;
        static public ObservableCollection<SongInfoExpend> PlayListSongs = new ObservableCollection<SongInfoExpend>();
        static private int playListTotal = 0;
        /// <summary>
        /// 播放列表项总数，该属性会被PlayListIndex属性更新
        /// </summary>
        static public int PlayListTotal
        {
            get
            {
                playListTotal = PlayListBase.PlayListSongs.Count;
                return playListTotal;
            }
            set
            {
                playListTotal = value;
            }
        }
        static private int playListIndex = 1;
        /// <summary>
        /// 从1开始计数的播放列表位置
        /// </summary>
        static public int PlayListIndex
        {
            get
            {
                //注意PlayListTotal == 0主要为了更新PlayListTotal属性的数目
                if (PlayListTotal == 0) return 0;
                if (playListIndex == 0) playListIndex = 1;
                return playListIndex;
            }
            set
            {
                if (value > PlayListTotal) playListIndex = 1;
                else if (value < 1) playListIndex = 1;
                else playListIndex = value;
            }
        }
        /// <summary>
        /// 播放异常计数
        /// </summary>
        static public int ExceptionCount = 0;
        //private CollectionView myCollectionView;

        /// <summary>
        /// 从播放列表中播放音乐
        /// </summary>
        /// <param name="NextPlay">选择当出现意外播放时的行为</param>
        static public void PlayAndExceptionPass(string NextPlay = "next")
        {
            if (PlayListIndex == 0) { return; }
            CurSongInfoEx = PlayListSongs[PlayListIndex - 1];
            SongInfo sinfo = CurSongInfoEx.SongInfo;
            //播放相关
            if (sinfo.SongPath == null || !File.Exists(sinfo.SongPath))
            {
                ExceptionCount++;
                if (ExceptionCount > PlayListTotal)
                {
                    MessageBox.Show("全曲目播放失败，暂停循环");
                    return;
                }
                if (NextPlay == "next" && ++PlayListIndex <= PlayListTotal)
                    PlayAndExceptionPass("next");
                else if (NextPlay == "prev" && --PlayListIndex >= 1)
                    PlayAndExceptionPass("prev");
                return;
            }
            //player.Open(sinfo.SongPath);
            //player.Play();
            //try
            //{
            //    if (player.CurrentPlayerName == "naudio")
            //        player.Open(sinfo.SongPath);
            //    else
            //    {
            //        player.Stop();
            //        player = naudioplayer;
            //        player.Open(sinfo.SongPath);
            //    }
            //}
            //catch (NotSupportedException)
            //{
            //    player = cscplayer;
            //    try
            //    {
            //        cscplayer.Open(sinfo.SongPath);
            //    }
            //    catch (Exception)
            //    {
            //        ++PlayListIndex;
            //        PlayAndExceptionPass();
            //    }
            //}
            //player.CurPlayState = PlayState.playing;
            //SendMsg(MyMsgType.Startplay, sinfo.SongPath);
            ExceptionCount = 0;
            //LRC相关
            Playing.lrcshow = null;
            Playing.CurLrcItem = null;
            //Playing.LoadLoaclLrc(null, null, null, null);
            PlayThread = new Thread(() =>
            {
                try
                {
                    player.Open(sinfo.SongPath,ConfigPage.GlobalConfig.CurrentVolume,ConfigPage.GlobalConfig.OpenMethodsStr, ConfigPage.GlobalConfig.DeviceStr);
                    player.Play();
                }
                catch {
                    return;
                }
                PlayingChanged?.Invoke(CurSongInfoEx);

                timer.Change(1000, 1000);//这个延时大概是为了处理加载的结束；待改

            });
            PlayThread.Start();


           // Thread.Sleep(10000);
        }



        /// <summary>
        /// 加入单曲到播放列表
        /// </summary>
        /// <param name="sinfo">要播放的曲目</param>
        /// <param name="IsPlayImmediately">是否要下一个播放</param>
        internal static void AddToPlayList(SongInfoExpend sinfo, bool IsPlayImmediately)
        {
            if (sinfo == null) return;
            if (ConfigPage.GlobalConfig.AddIndex == 1)
            {
                int index = PlayListIndex;
                PlayListSongs.Insert(index,new SongInfoExpend(sinfo));
                if (index == 0) PlayListIndex = 1;//PlayListIndex的逻辑造成当PlayListIndex=0时，PlayListIndex++=2
                else
                    if (IsPlayImmediately == true)
                    PlayListIndex++;
            }
            else if (ConfigPage.GlobalConfig.AddIndex == 0)
            {
                PlayListSongs.Add(new SongInfoExpend(sinfo));
                if (IsPlayImmediately == true)
                    PlayListIndex = PlayListTotal;
            }
        }
        internal static void AddToPlayList(SongInfoExpend sinfo, int index)
        {
            if (index > PlayListBase.PlayListSongs.Count - 1)
                PlayListBase.PlayListSongs.Add(new SongInfoExpend(sinfo));
            else
                PlayListBase.PlayListSongs.Insert(index, new SongInfoExpend(sinfo));
        }

        /// <summary>
        /// 加入歌单到播放列表
        /// </summary>
        /// <param name="sinfos">要播放的曲目</param>
        /// <param name="IsPlayImmediately">是否要下一个播放</param>
        internal static void AddToPlayList(IList<SongInfoExpend> sinfos, bool IsPlayImmediately)
        {
            int conut = sinfos.Count();
            int index = PlayListBase.PlayListIndex;
            if (ConfigPage.GlobalConfig.AddIndex == 1)
            {
                for (int i = 0; i < conut; i++)
                {
                    PlayListSongs.Insert(index + i, new SongInfoExpend(sinfos[i]));
                }
                if (IsPlayImmediately == true)
                    if (index == 0) PlayListIndex = 1;//PlayListIndex的逻辑造成当PlayListIndex=0时，PlayListIndex++=2
                    else PlayListIndex++;
            }
            else if (ConfigPage.GlobalConfig.AddIndex == 0)
            {
                for (int i = 0; i < sinfos.Count(); i++)
                {
                    PlayListSongs.Insert(index + i, new SongInfoExpend(sinfos[i]));
                }
                if (IsPlayImmediately == true)
                    PlayListIndex = PlayListTotal - conut;
            }
        }



        static public void SendMsg(MyMsgType myMsgType, string msgValue,AudioProgressCommunicationDataType datatype= AudioProgressCommunicationDataType.UNKNOWN)
        {
            string sendString = msgValue;
            //byte[] sarr = System.Text.Encoding.Default.GetBytes(sendString);
            //int len = sarr.Length;
            //COPYDATASTRUCT cds;
            //cds.dwData = (IntPtr)datatype;
            //cds.cbData = len + 1;
            //cds.lpData = sendString;
            //if ((int)MainWindow.CurMainWindowInstence.AudioProcess.MainWindowHandle == 0) MessageBox.Show("没找到音频进程");
            //SendMessage(MainWindow.CurMainWindowInstence.AudioProcess.MainWindowHandle, WM_COPYDATA, (int)myMsgType, ref cds);
            //if (MainWindow.CurMainWindowInstence.AudioProcess.HasExited) MessageBox.Show("没找到音频进程");
            //PostThreadMessage(MainWindow.CurMainWindowInstence.AudioProcess.Threads[0].Id, WM_COPYDATA, IntPtr.Zero, IntPtr.Zero);

        }

    }
}
