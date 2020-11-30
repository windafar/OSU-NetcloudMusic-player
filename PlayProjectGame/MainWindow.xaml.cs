using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AddIn.Audio;
using PlayProjectGame.PlayList;
using PlayProjectGame.Core;
using PlayProjectGame.Helper;
using System.Windows.Media.Animation;
using WindowDemo;
using PlayProjectGame.Data;

namespace PlayProjectGame
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        public static readonly DependencyProperty CurrentThemeBrushProperty = DependencyProperty.Register("CurrentThemeBrush", typeof(Brush), typeof(MainWindow), new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255,230,230,230))));
        public static readonly DependencyProperty BottomControlVisibilityProperty = DependencyProperty.Register("BottomControlVisibility", typeof(Visibility), typeof(MainWindow), new PropertyMetadata(Visibility.Collapsed));

        public Brush CurrentThemeBrush
        {
            get => (Brush)(GetValue(CurrentThemeBrushProperty));
            set
            {
                SetValue(CurrentThemeBrushProperty, value);

            }
        }
        public Visibility BottomControlVisibility
        {
            get => (Visibility)(GetValue(BottomControlVisibilityProperty));
            set
            {
                SetValue(BottomControlVisibilityProperty, value);

            }
        }

        Slider VolumeSlider;
        static public MainWindow CurMainWindowInstence;
        public List<PlayListData> NetCloudData = new List<PlayListData>();
        public List<PlayListData> OsuData = new List<PlayListData>();

        void OnPlayingChanged(SongInfoExpend songInfoExpend) {
            var sinfo = songInfoExpend.SongInfo;
            //播放相关_UI-playbutton
            MainWindow.CurMainWindowInstence.Dispatcher.BeginInvoke((ThreadStart)delegate
            {
                BottomControlVisibility = Visibility.Visible;
                MainWindow.CurMainWindowInstence.PauseButtonCanvas.Visibility = Visibility.Visible;
                MainWindow.CurMainWindowInstence.PlayButtonCanvas.Visibility = Visibility.Collapsed;
                MainWindow.CurMainWindowInstence.Title = PlayListBase.CurSongInfoEx.SongInfo.SongName;
            });

            new MusicTag().GetCurrentMusicFileJPGBufferAsync(sinfo, true)
               .ContinueWith(r1 => {
                   if (r1.Result == null)
                   {
                       Debug.Print("返回流为空");
                       return;
                   }
                   var r2 = new System.IO.MemoryStream(r1.Result);
                   MainWindow.CurMainWindowInstence.Dispatcher.BeginInvoke((ThreadStart)delegate
                   {
                       MainWindow.CurMainWindowInstence.PlayAblum.Source = UIhelper.ConveBitmapImage(r2);
                       //r2.Dispose();
                       MainWindow.CurMainWindowInstence.PlaySongName.Text = sinfo.SongName;
                       MainWindow.CurMainWindowInstence.PlaySongArt.Text = sinfo.SongArtist.Trim(',');
                       MainWindow.CurMainWindowInstence.Icon = UIhelper.ConveBitmapImage(r2);

                   });


               });
            //播放相关_UI-color
            new MusicTag().GetCurrentColorAsync(sinfo, true)
                          .ContinueWith((r1) =>
                          {
                              var color = r1.Result;
                              MainWindow.CurMainWindowInstence.Dispatcher.BeginInvoke((ThreadStart)delegate
                              {
                                  Color c1 = new Color() { A = 125, B = color.B, G = color.G, R = color.R };
                                  Color c2 = new Color() { A = 230, B = color.B, G = color.G, R = color.R };
                                  if (Playing.CurPlayingInstance != null)
                                  {

                                      //ColorAnimation colorAnimation = new ColorAnimation();
                                      //colorAnimation.To = c1;
                                      //colorAnimation.Duration = new Duration(TimeSpan.FromSeconds(1));
                                      //storyboard.Children.Add(colorAnimation);

                                      //storyboard.Begin();

                                      MainWindow.CurMainWindowInstence.CurrentThemeBrush = new SolidColorBrush(c1);
                                  }
                                  else if (MainWindow.CurMainWindowInstence != null)
                                  {
                                      //Storyboard.SetTarget(storyboard, (CurMainWindowInstence));
                                      //Storyboard.SetTargetProperty(storyboard, new PropertyPath("CurrentThemeBrush.Color"));

                                      //ColorAnimation colorAnimation = new ColorAnimation();
                                      //colorAnimation.To = c2;
                                      //colorAnimation.Duration = new Duration(TimeSpan.FromSeconds(1));
                                      //storyboard.Children.Add(colorAnimation);

                                      //storyboard.Begin();

                                      MainWindow.CurMainWindowInstence.CurrentThemeBrush = new SolidColorBrush(c2);
                                  }
                  
                              });
                          });
        }


        private Thread ClundMusicTreeNewThread;
        public List<string> filesyscath = new List<string>();//用于匹配歌曲的全局缓存

        public void LoadNetCloudMusic()
        {
            ClundMusicTreeNewThread = new Thread(delegate ()
            {
                var list = CouldMusicLocalDataGeter.GetSongList();
                List<PlayListData> List = list.Last().Pids;
                NetCloudData = List;

                Dispatcher.Invoke((ThreadStart)delegate ()
                {
                    NetClouldMusicaListBox.ItemsSource = List;
                });
            });
            ClundMusicTreeNewThread.Start();
            //Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
        }

        public void LoadOSU(bool IsUpdate)
        {
            OSUListBox.ItemsSource = null;
            OSUListBox.Items.Clear();
            OSUListBox.Items.Add("正在加载中");
            List<PlayListData> list = new List<PlayListData>();
            var t = new OsuLocalDataGeter(ConfigPage.GlobalConfig.OsuDir).GetOsuSongList(IsUpdate)
                .ContinueWith((x) =>
                 {
                     Dispatcher.Invoke((ThreadStart)delegate ()
                     {
                         OSUListBox.Items.Clear();
                         if (x.Result.Count>0&&x.Result[0].Pids.Count > 0)
                         {
                             OsuData = x.Result[0].Pids;
                             OSUListBox.ItemsSource = x.Result[0].Pids;
                         }
                         else { OSUListBox.Visibility = Visibility.Collapsed; };
                     });
                 });



        }

        public MainWindow()
        {
            ConfigPage.GlobalConfig = GlobalConfigClass.LoadConfig();

            InitializeComponent();
            PauseButtonCanvas.Visibility = Visibility.Collapsed;
            LoadNetCloudMusic();

            LoadOSU(false);

        }

        #region 导航事件
        /// <summary>
        /// 用于恢复SongListPage的状态（直接的回退那种）
        /// </summary>
        /// <param name="state">历史状态</param>
        public void ReplySongListPage(PlayListJournalEntry state)
        {
            if (frame.Content as SongList != null)
            {
                ((SongList)frame.Content).PLD = state.PLD;
                ((SongList)frame.Content).SongListListView.ItemsSource = state.PLD.Songs;
                ((SongList)frame.Content).SongListName.Text = state.PLD.PlatListName;
                ((SongList)frame.Content).SongListInfo.Text = state.PLD.PlayListInfo;
                ((SongList)frame.Content).SongListCreator.Text = state.PLD.UersName;
                ((SongList)frame.Content).GetImage(state.PLD.PlayListId);
                if (state.curSelected != null)
                {
                    ((SongList)frame.Content).SongListListView.SelectedItem = state.curSelected;
                    ((SongList)frame.Content).SongListListView.ScrollIntoView(state.curSelected);
                }
            }
            else
            {
                frame.Content = null;
            }
        }

        private void Frame_LoadCompleted(object sender, NavigationEventArgs e)
        {


        }

        private void frame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (frame.CanGoBack)
            {
                frame.GoBack();
            }
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (frame.CanGoForward)
            {
                frame.GoForward();
            }
        }

        private void frame_NavigationStopped(object sender, NavigationEventArgs e)
        {

        }

        private void frame_Navigated(object sender, NavigationEventArgs e)
        {

        }

        #endregion

        #region 导航源事件
        private void NetClouldMusicaListBox_SelectItemChanged(object sender, SelectionChangedEventArgs e)
        {
            PlayListData pld = NetClouldMusicaListBox.SelectedItem as PlayListData;
            if (pld != null)
            {
                if (frame.Content != null)
                {
                    SongList Pr = frame.Content as SongList;
                    if (Pr != null)
                    {
                        frame.AddBackEntry(new PlayListJournalEntry(Pr.PLD, ReplySongListPage));
                    }
                }
                SongList.SetPlayListData(pld);
                Uri NextUri = new Uri("SongList/SongList.xaml", UriKind.Relative);
                if (frame.Source==null||frame.Source.OriginalString != "SongList/SongList.xaml")
                    frame.Navigate(NextUri);
                else frame.Refresh();
                //frame.Navigate(new SongList(pld));
            }

        }

        private void OSUListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlayListData pld = OSUListBox.SelectedItem as PlayListData;
            if (pld != null)
            {
                if (frame.Content != null)
                {
                    SongList Pr = frame.Content as SongList;
                    if (Pr != null)
                    {
                        frame.AddBackEntry(new PlayListJournalEntry(Pr.PLD, ReplySongListPage));
                    }
                }
                SongList.SetPlayListData(pld);
                Uri NextUri = new Uri("SongList/SongList.xaml", UriKind.Relative);
                if (frame.Source == null || frame.Source.OriginalString != "SongList/SongList.xaml")
                    frame.Navigate(NextUri);
                else frame.Refresh();
                //frame.Navigate(new SongList(pld));

            }
        }
        private void PlayListTextBox_MouseDown_1(object sender, MouseButtonEventArgs e)
        {//待完成
            //可以使用与SongList相同的日志，但必须另外重写恢复函数
            frame.Navigate(new Uri("/PlayList/WrapPlayList.xaml", UriKind.Relative));

        }

        private void PlayingTextBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //可以使用与SongList相同的日志，但必须另外重写恢复函数

            frame.Navigate(new Uri("/Playing.xaml", UriKind.Relative));
        }

        private void ConfigTextBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            frame.Navigate(new Uri("/ConfigPage.xaml", UriKind.Relative));

        }
        #endregion

        #region 窗口相关事件
            #region 子进程事件
            public HwndSource hwndSource;
            public Process AudioProcess;
            private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // Handle messages...
            //if (msg == 0x004A)
            //{
            //    COPYDATASTRUCT cds = new COPYDATASTRUCT();
            //    var obj = Marshal.PtrToStructure<COPYDATASTRUCT>(lParam);
            //    switch ((int)wParam)
            //    {
            //        case (int)MyMsgType.Playing:
            //            //PlayListBase.CurrentPlayTime = long.Parse(obj.lpData);
            //            //updatePlayUI(TimeSpan.FromTicks(PlayListBase.CurrentPlayTime),
            //            //    TimeSpan.FromTicks(PlayListBase.CurPlayTotalTime));
            //            updatePlayUI(PlayListBase.player.Position,
            //                        PlayListBase.player.TotalTime);

            //            break;
            //        //case (int)MyMsgType.Startplay:
            //        //    PlayListBase.CurPlayTotalTime = long.Parse(obj.lpData);
            //         //   break;
            //    }
            //}
            return IntPtr.Zero;
        }

            private void updatePlayUI(TimeSpan Position, TimeSpan TotalTime)
            {
                double secx = (Position.TotalSeconds / TotalTime.TotalSeconds);
                MainWindow.CurMainWindowInstence.Dispatcher.BeginInvoke((ThreadStart)delegate ()
                {
                    //这可能有问题，
                    if (!double.IsNaN(secx)&& !double.IsInfinity(secx))
                        MainWindow.CurMainWindowInstence.PalySlider.Value = secx * 10;//MainWindow.CurMainWindowInstence.PalySlider.Maximum;
                });
                if ((TotalTime - Position).TotalSeconds < 1 && TotalTime.TotalSeconds > 0)
                {
                    //timer.Change(Timeout.Infinite, 50);
                    PlayListBase.PlayListIndex++;
                    CurMainWindowInstence.Dispatcher.BeginInvoke((ThreadStart)delegate ()
                    {
                        PlayListBase.PlayAndExceptionPass();
                    });

                }
            }
        #endregion

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            CurMainWindowInstence = this;
            hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            //hwndSource.AddHook(WndProc);
            AudioProcess= Helper.OtherHelper.StartAudioProcesses();
            AudioProcess.PriorityClass = ProcessPriorityClass.High ;
            
            PlayListBase.PlayingChanged += OnPlayingChanged;
            //Storyboard.SetTarget(storyboard, MainWindow.CurMainWindowInstence);
            //Storyboard.SetTargetProperty(storyboard, new PropertyPath("CurrentThemeBrush.Color"));
            //storyboard.Completed += ThemeBrushStoryboard_Completed;
            //storyboard.FillBehavior = FillBehavior.HoldEnd;
        }
        //private void ThemeBrushStoryboard_Completed(object sender, EventArgs e)
        //{
        //    ((ClockGroup)sender).Controller.Remove();
        //    storyboard.Children.Clear();
            
        //}

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            PlayListBase.PlayingChanged -= OnPlayingChanged;
            WrapPlayList.pageClosed = true;
            QuickPlayListControl.pageClosed = true;
            if (LrcDeskTopWindow != null)
                LrcDeskTopWindow.Close();
            if (PlayListBase.timer != null) PlayListBase.timer.Dispose();
            if (Playing.LrcTimer != null)Playing.LrcTimer.Dispose();
            if (PlayListBase.player != null)PlayListBase.player.Dispose();
            if(PlayListBase.PlayThread!=null&& PlayListBase.PlayThread.ThreadState== System.Threading.ThreadState.Running)PlayListBase.PlayThread.Abort();
            byte[] buff = Helper.OtherHelper.SerializeObject(LRCRefClass.LrcRefDic);
            if(buff!=null) System.IO.File.WriteAllBytes(GlobalConfigClass.OBJ_LRCDIC_REFPATH, buff);
            OtherHelper.WriteXMLSerializer(ConfigPage.GlobalConfig, typeof(GlobalConfigClass), "Config.xml");
            //DataGeter.WriteSerializer(DataGeter.NetClouldMusicData, typeof(List<UserData>), GlobalConfigClass.XML_CLOUDMUSIC_SAVEPATH);
            //DataGeter.WriteSerializer(OsoDataGeter.OsuListData, typeof(List<UserData>), GlobalConfigClass.XML_OSU_SAVEPATH);
            //GC.Collect();
            if (AudioProcess != null && !AudioProcess.HasExited)
                AudioProcess.Kill();//之前的任务取消异常没处理，现在都不知道哪里抛的异常导致的不执行

        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                acc();//执行最小化后台功能

            }
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            window.Close();
        }

        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!Helper.UIhelper.IsMaxWindow)
                Helper.UIhelper.WindowMaxEx(this,grid.Margin);
            else
                Helper.UIhelper.WindowSizeRestore(this);
        }

        private void Line_MouseDown(object sender, MouseButtonEventArgs e)
        {
            window.WindowState = WindowState.Minimized;
        }

        private void TitleRect_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (this.WindowState == WindowState.Maximized || Helper.UIhelper.IsMaxWindow)
                {
                    double y = e.GetPosition(window).Y - 20;
                    double x1 = e.GetPosition(window).X;
                    double x2 = window.ActualWidth - x1;
                    if (this.WindowState == WindowState.Maximized)
                        window.WindowState = WindowState.Normal;
                    else
                        Helper.UIhelper.WindowSizeRestore(this);
                    window.Top = y;
                    window.Left = x1 - window.ActualWidth * (x1 / (x1 + x2));
                }
                window.DragMove();
            }
        }
        int ClickTime = 0;
        Timer DoubleClickTime;
        private void TitleRect_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DoubleClickTime == null)
            {
                DoubleClickTime = new Timer(new TimerCallback((obj) =>
                {
                    ClickTime = 0;

                    DoubleClickTime.Dispose();
                    DoubleClickTime = null;
                }));
            }
            //单击的第一下将单击数目+1，等待200ms开始计时器，计时器将单击计数清零，并注销自身
            DoubleClickTime.Change(200, 200);
            ClickTime++;
            if (ClickTime >= 2)
            {
                Helper.UIhelper.WindowMaxEx(this,grid.Margin);
                //imagInTab2.Width = SystemParameters.PrimaryScreenWidth;
                //imagInTab2.Height = SystemParameters.PrimaryScreenHeight;

            }
        }

        Window LrcDeskTopWindow;
        private void DesktopLrctextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (LrcViewOther.LrcShowWindow.CurrentInstence == null)
            {
                LrcDeskTopWindow = new LrcViewOther.LrcShowWindow();
                LrcDeskTopWindow.Show();
            }
            else
                LrcViewOther.LrcShowWindow.CurrentInstence.Close();
        }

        #endregion

        #region 播放相关控件事件
        private void PalySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed && ((Slider)sender).IsMouseOver)
            {
                    var Position = PlayListBase.player.TotalTime.TotalSeconds * (PalySlider.Value / (PalySlider.Maximum - PalySlider.Minimum));
                PlayListBase.player.Position = TimeSpan.FromSeconds(Position);
                //var Position = new TimeSpan(PlayListBase.CurPlayTotalTime).TotalSeconds * (PalySlider.Value / (PalySlider.Maximum - PalySlider.Minimum));
                //PlayListBase.SendMsg(MyMsgType.Drop, Position.ToString(), AudioProgressCommunicationDataType.DoubleSec);
                if (e.NewValue < e.OldValue)
                        Playing.IsMovePre = true;
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (PauseButtonCanvas.Visibility == Visibility.Visible)
            {
                PauseButtonCanvas.Visibility = Visibility.Collapsed;
                PlayButtonCanvas.Visibility = Visibility.Visible;
                //PlayListBase.CurPlayState = PlayState.suspend;
                //PlayListBase.SendMsg(MyMsgType.Suspend , PlayListBase.CurSongInfoEx.SongInfo.SongPath);
                if (!PlayListBase.player.IsPause)
                    PlayListBase.player.Pause();
            }
            else if (PlayButtonCanvas.Visibility == Visibility.Visible)
            {
                if (PlayListBase.PlayListIndex == 0) return;
                PauseButtonCanvas.Visibility = Visibility.Visible;
                PlayButtonCanvas.Visibility = Visibility.Collapsed;
                //if (PlayListBase.CurPlayState == PlayState.suspend)
                //{
                //   PlayListBase.SendMsg(MyMsgType.Replay,PlayListBase.CurSongInfoEx.SongInfo.SongPath);
                //}
                if (PlayListBase.player.IsPause)
                    PlayListBase.player.Play();
                else
                    PlayListBase.PlayAndExceptionPass();
            }
        }

        private void previousButton_Click(object sender, RoutedEventArgs e)
        {
            PlayListBase.PlayListIndex--;
            PlayListBase.PlayAndExceptionPass("prev");

        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            PlayListBase.PlayListIndex++;
            PlayListBase.PlayAndExceptionPass();
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed && ((Slider)sender).IsMouseOver)
            {
                int Volume = (int)(100 * (VolumeSlider.Value / (VolumeSlider.Maximum - VolumeSlider.Minimum)));
                 PlayListBase.player.Volume = (int)Volume;
                 ConfigPage.GlobalConfig.CurrentVolume = Volume;
                //PlayListBase.SendMsg(MyMsgType.Volume, Volume.ToString(), AudioProgressCommunicationDataType.IntPercent);
            }
        }

        private void CurPlayStackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            frame.Navigate(new Uri("/Playing.xaml", UriKind.Relative));
        }

        #endregion

        #region 左侧控件
        private void NetClouldMusicaListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scroller = Helper.UIhelper.FindPrent<ScrollViewer>((DependencyObject)sender);
            scroller.ScrollToVerticalOffset(scroller.ContentVerticalOffset - e.Delta * 0.5);
        }
        private void OSUListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scroller = Helper.UIhelper.FindPrent<ScrollViewer>((DependencyObject)sender);
            scroller.ScrollToVerticalOffset(scroller.ContentVerticalOffset - e.Delta * 0.5);
        }
        private void scrollViewer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ScrollViewer scroller = ((ScrollViewer)sender);
            if (e.RightButton == MouseButtonState.Pressed)
            {
                scroller.ScrollToTop();
                //e.Handled = true;
            }
            scroll_Dist = e.GetPosition(scroller).Y;
        }

        double scroll_Dist = 0;//记录滚动开始时的位置
        private void scrollViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                ScrollViewer scroller = ((ScrollViewer)sender);
                //根据偏移量调节滚动速度，0.02是偏移和速度的比率，scroller.ScrollableHeight / scroller.ViewportHeight是为了保证所有大小窗口的滚动时间一致
                scroller.ScrollToVerticalOffset(scroller.VerticalOffset +
                    (e.GetPosition(scroller).Y - scroll_Dist) * 0.001 * scroller.ScrollableHeight / scroller.ViewportHeight);
                //e.Handled = true;
            }

        }

        private void scrollViewer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            scroll_Dist = 0;
        }

        #endregion

        //顶部搜索
        private void MainSearcher_TextChanged(object sender, TextChangedEventArgs e)
        {//如果后续会变得复杂，就改成导航服务+视图来做吧
            TextBox textbox = e.OriginalSource as TextBox;
            if (textbox.Name != "MainSearcher") return;
            var text = textbox.Text;
            if (Keyboard.IsKeyDown(Key.Back)&& string.IsNullOrWhiteSpace(text)&& frame.CanGoBack) {
                frame.GoBack();
                return;
            }
            else if (string.IsNullOrWhiteSpace(text))
                return;
            MainSearchResult mainSearchResult = new MainSearchResult();
            mainSearchResult.key = text;
            foreach(var x in NetCloudData)
            {
                //if (x.PlatListName.IndexOf(text) > -1) mainSearchResult.pld.Add(x);
                if (x.Songs.Count > 0)
                    mainSearchResult.songs.AddRange(
                        x.Songs.Where(s =>
                            s.SongInfo.SongName.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) > -1
                            || s.SongInfo.SongArtist.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) > -1
                            || s.SongInfo.SongAlbum.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) > -1
                            ));
            };
            foreach (var x in OsuData)
            {
                //if (x.PlatListName.IndexOf(text) > -1) mainSearchResult.pld.Add(x);
                if (x.Songs.Count > 0)
                    mainSearchResult.songs.AddRange(
                        x.Songs.Where(s =>
                            s.SongInfo.SongName.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) > -1
                            || s.SongInfo.SongArtist.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) > -1
                            || s.SongInfo.SongAlbum.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) > -1
                            ));
            };
            if (frame.Source==null||frame.Source.OriginalString != "SearchPage.xaml")
            {
                SearchPage.SetMSRBeforStart = mainSearchResult;
                frame.Navigate(new Uri("/SearchPage.xaml", UriKind.Relative));
            }
            else {
                SearchPage.SetMSRBeforStart = mainSearchResult;
                frame.Refresh();
            }
        }

        private void NavButtonClick(object sender, RoutedEventArgs e)
        {
            if (e.Source.GetType() != typeof(MainWindow)) return;
            string name = ((System.Windows.Controls.Primitives.ButtonBase)e.OriginalSource).Name;
            if (name == "BackButton")
                BackButton_Click(e.OriginalSource, e);
            else if (name == "ForwardButton")
                ForwardButton_Click(e.OriginalSource, e);
            else if (name == "Sound")
            {
                DependencyObject p = Helper.UIhelper.FindPrent<StackPanel>((DependencyObject)sender);
                if (p == null) p = (DependencyObject)sender;
                    VolumeSlider = UIhelper.FindChild<Slider>(p, "VolumeSlider");
                if (VolumeSlider.Visibility == Visibility.Visible)
                {
                    VolumeSlider.Visibility = Visibility.Collapsed;
                    VolumeSlider.ValueChanged -= VolumeSlider_ValueChanged;
                }
                else
                {
                    VolumeSlider.Visibility = Visibility.Visible;
                    VolumeSlider.Value = ConfigPage.GlobalConfig.CurrentVolume/100f * VolumeSlider.Maximum;
                    VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;
                }
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source.GetType() != typeof(MainWindow)) return;
            var Image = e.OriginalSource as Image;
            if (Image != null)
            {
                if(SongCoverPrvImage.Visibility== Visibility.Collapsed)
                    SongCoverPrvImage.Visibility = Visibility.Visible;
                else
                    SongCoverPrvImage.Visibility = Visibility.Collapsed;

            }
        }

        private void SongCoverPrvImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SongCoverPrvImage.Visibility = Visibility.Collapsed;
        }

    }
}