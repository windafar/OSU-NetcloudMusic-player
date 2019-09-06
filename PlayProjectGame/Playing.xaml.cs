using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AddIn.Lrc;
using AddIn.LrcSercher;
using System.IO;
using System.Threading;
using System.Windows.Threading;
using PlayProjectGame.LrcSercher;
using PlayProjectGame.PlayList;
using PlayProjectGame.Helper;
using PlayProjectGame.Data;

namespace PlayProjectGame
{
    public delegate void LrcUIUpdateDele();
    public delegate void PlayingPageBackgroundUpdate();
    /// <summary>
    /// xaml 的交互逻辑
    /// </summary>
    public partial class Playing : Page
    {
        /// <summary>
        /// Playing实例，初始化时被赋值，用于该类上下文与静态对象交互
        /// </summary>
        public static Playing CurPlayingInstance { get; private set; }
        private static ILyricsSercher lrcFileSercher { set; get; }

        void OnPlayingChanged(SongInfoExpend songInfoExpend) {
                LoadLoaclLrc(null, Dispatcher, LoadLrcSuccessToUI, LoadLrcFailToUI);
                InitLrcShow(Dispatcher);//性能问题：0.3-0.5s
                PlayingPageBackgroundUpdateUIDeleLink?.Invoke();
            notesViewData = new Note.NotesViewData(CurrentSongInfo.SongInfo);
            FirstNode = notesViewData.Notes.First;
                Dispatcher.Invoke(()=>PlayingGrid.Children.Clear());
                

        }

        #region LRC的所有非UI全局属性，字段，函数
        //LRC的全局使用：
        //首先LoadLoaclLrc,
        //然后添加UI处理逻辑到UpdateLrcUIDeleLink委托
        //最后，如果Lrc的定时器没有初始化，初始化LrcShow..
        //如果更换了歌曲，则将lrcshow和CurLrcItem赋NULL
        //如果后退了，手动改变IsMovePre属性
        public static LrcInfo lrcshow { get; set; }
        public static SongInfoExpend CurrentSongInfo
        {
            get
            {
                if (PlayListBase.PlayListIndex == 0) return null;
                return PlayListBase.PlayListSongs[PlayListBase.PlayListIndex - 1];
            }
        }
        public static LinkedListNode<LrcItem> CurLrcItem { get; set; }
        public static bool IsMovePre { get; set; }
        public static Timer LrcTimer;
        public static LrcUIUpdateDele UpdateLrcUIDeleLink;//用于所有rc实时滚动更新
        public static PlayingPageBackgroundUpdate PlayingPageBackgroundUpdateUIDeleLink;//用于播放页面的背景更新
        public static bool IsShowOlddRowLrc { get => GlobalConfigClass._Config.LrcMode; } //指定显示奇数行

        /// <summary>
        /// 初始化LRC显示,包含初始化定时器,
        /// </summary>
        /// <param name="UIElemDispatcher">要显示LRC的的元素的Dispatcher</param>
        public static void InitLrcShow(Dispatcher UIElemDispatcher)
        {
            if (LrcTimer == null)
                LrcTimer = new Timer(delegate
                {
                    if (lrcshow != null && CurLrcItem != null)
                    {
                        //if (CurLrcItem.Value.Time <= PlayListBase.player.Position)
                        var CurPlayPosition = PlayListBase.player.Position;
                        if (CurLrcItem!=null&&CurLrcItem.Value.Time <= CurPlayPosition)
                        {
                            if (UpdateLrcUIDeleLink != null)
                                UIElemDispatcher.Invoke(UpdateLrcUIDeleLink);
                            if (CurLrcItem != null)
                                CurLrcItem = CurLrcItem.Next;
                        }
                        else if (IsMovePre == true && CurLrcItem != null&& CurLrcItem.Value.Time > CurPlayPosition)
                        {
                            if (UpdateLrcUIDeleLink != null)
                                UIElemDispatcher.Invoke(UpdateLrcUIDeleLink);
                            if (CurLrcItem != null)
                                CurLrcItem = CurLrcItem.Previous;
                            if (CurLrcItem != null && CurLrcItem.Value.Time <= PlayListBase.player.Position)
                                IsMovePre = false;
                        }
                    }
                }, null, 200, 20);//这里100->0,可能会有多线程锁问题
            else
                LrcTimer.Change(200, 20);

            //return LrcTimer;
        }
        /// <summary>
        /// 关联LRC到当前歌曲同时加载到控件。约定：若非使用新的LRC，LRCFilePath都为空。
        /// LRCFilePath为空时的加载顺序：1，当前的lrcshow静态对象，2当前歌曲关联的LRC，3搜索本地LRC文件；
        /// LRCFilePath有效时，1，关联，2，加载；
        /// 如果UIElemDispatcher参数为空将不更新UI
        /// </summary>
        /// <param name="LRCFilePath">LRC文件本地路径（可以为空以获取关联LRC）</param>
        /// <param name="UIElemDispatcher">控件拥有的Dispatcher</param>
        /// <param name="f_LoadSuccess">加载成功后的UI逻辑(lrcshow已经加载，快把lrcshow.Items加进去吧~)</param>
        /// <param name="f_LoadFail">加载失败后的UI逻辑（此次加载失败了，只好把集合控件源赋NULL了( ▼-▼ )）</param>
        public static void LoadLoaclLrc(string LRCFilePath, Dispatcher UIElemDispatcher, ThreadStart f_LoadSuccess, ThreadStart f_LoadFail)
        {
            try
            {
                var curSonrInfo = CurrentSongInfo;//为了准确的关联到歌曲
                if (LRCFilePath != null)
                {
                    if (LrcTimer != null) LrcTimer.Change(Timeout.Infinite, 50);
                }
                if (File.Exists(LRCFilePath))//如果带有有效参数
                    curSonrInfo.SongInfo.LrcRefPath = LRCFilePath;//关联

                if (curSonrInfo != null && File.Exists(curSonrInfo.SongInfo.LrcRefPath))
                {//如果含有有效关联，加载
                    if (curSonrInfo != CurrentSongInfo) return;//歌曲改变返回

                    FileInfo fileInfo = new FileInfo(curSonrInfo.SongInfo.LrcRefPath);
                    try
                    {
                        if (LRCFilePath != null || lrcshow == null)
                            lrcshow = new LrcInfo(fileInfo.FullName);
                    }
                    catch (FileLoadException)
                    {//加载LRC文件错误时
                        if (UIElemDispatcher != null)
                            UIElemDispatcher.Invoke(f_LoadFail);
                        CurLrcItem = null;
                        return;
                    }
                    if (CurLrcItem == null)//处理同步
                        CurLrcItem = lrcshow.Items.First;
                    if (UIElemDispatcher != null)
                        UIElemDispatcher.Invoke(f_LoadSuccess);
                }
                else//没有指定LRC文件，且加载关联LRC也失败，此处搜索
                {
                    if (CurrentSongInfo == null) return;
                    var curSongInfo = CurrentSongInfo; string path;
                    if (lrcFileSercher == null) lrcFileSercher = new LocalLyricsList(ConfigPage.GlobalConfig.LrcDir);
                    lrcFileSercher.GetLrcInfoSercherList(curSongInfo.SongInfo.SongArtist, curSongInfo.SongInfo.SongName, ConfigPage.GlobalConfig.LrcDir);
                    if (lrcFileSercher.SercherResultList.Count > 0)
                    {
                        path = lrcFileSercher.SercherResultList[0].FilePath;
                        if (File.Exists(path))
                        {
                            LoadLoaclLrc(path, UIElemDispatcher, f_LoadSuccess, f_LoadFail);
                            return;
                        }
                    }
                    //无法挽救了，给NULL吧
                    if (UIElemDispatcher != null)
                        UIElemDispatcher.Invoke(f_LoadFail);
                    CurLrcItem = null;
                }
            }
            catch {
                if (UIElemDispatcher != null)
                    UIElemDispatcher.Invoke(f_LoadFail);
                CurLrcItem = null;

            }
        }
        /// <summary>
        /// 该函数用于改变了当前LRC后。更新lrcshow以及PlayingPage的LRCListBox(如果PlayingPage初始化了)
        /// </summary>
        /// <param name="LrcPath"></param>
        public static void ChangeLrcAndPlayingPage(string LrcPath)
        {
            if (LrcTimer==null)
                throw new InvalidOperationException("操作无效，Lrc没有初始化");
            lrcshow = new LrcInfo(LrcPath);
            CurLrcItem = lrcshow.Items.First;
            CurrentSongInfo.SongInfo.LrcRefPath = LrcPath;

            if (CurPlayingInstance != null)
            {
                CurPlayingInstance.LrcList.ItemsSource = lrcshow.Items;
                CurPlayingInstance.ErrorInfoTextBox.Visibility = Visibility.Hidden;
            }
            if (LrcViewOther.LrcShowWindow.CurrentInstence != null)
            {
                LrcViewOther.LrcShowWindow.CurrentInstence.LrcShowWindowListBox.ItemsSource = null;
                LrcViewOther.LrcShowWindow.CurrentInstence.LrcShowWindowListBox.Items.Clear();
                LrcViewOther.LrcShowWindow.CurrentInstence.LrcShowWindowListBox.ItemsSource = lrcshow.Items;
            }

        }
        #endregion
        public Brush CurrentLrcBrush {
            get => (Brush)(GetValue(CurrentLrcBrushProperty));
            set => SetValue(CurrentLrcBrushProperty, value);
        }
        public static readonly DependencyProperty CurrentLrcBrushProperty = DependencyProperty.Register("currentLrcBrush", typeof(Brush), typeof(Playing), new PropertyMetadata(Brushes.Black));
        public Note.NotesViewData notesViewData;

        public Playing()
        {
            InitializeComponent();
            lrcFileSercher = new LocalLyricsList(ConfigPage.GlobalConfig.LrcDir);
            CurPlayingInstance = this;

        }

        private void LrcList_Loaded(object sender, RoutedEventArgs e)
        {
            var curSongInfo = CurrentSongInfo;
            PlayListBase.PlayingChanged += OnPlayingChanged;

            //if (lrcshow == null)
            //加载
            LoadLoaclLrc(null, Dispatcher,
                ()=> { LoadLrcSuccessToUI(); }, 
                ()=>{LoadLrcFailToUI();
                });
            UpdateLrcUIDeleLink += UpdatePlayingPageLrc;//附加播放页ui实时滚动处理
                //显示
                InitLrcShow(Dispatcher);

            UpdatePlayingPageBackground();
            PlayingPageBackgroundUpdateUIDeleLink += UpdatePlayingPageBackground;
        }

        public void LoadLrcFailToUI()
        {
            LrcList.ItemsSource = null;
            if (CurrentSongInfo != null)
                ErrorInfoTextBox.Visibility = Visibility.Visible;
        }

        public void LoadLrcSuccessToUI()
        {
            ErrorInfoTextBox.Visibility = Visibility.Hidden;
            LrcList.ItemsSource = lrcshow.Items;
            ScrollLrc.ScrollToHorizontalOffset(ScrollLrc.ViewportWidth / 2);

        }

        private void SercherLrcMenuItem_Click(object sender, RoutedEventArgs e)
        {
            LrcSeacherWindow lrcwindow = new LrcSeacherWindow(CurrentSongInfo.SongInfo);
            lrcwindow.Show();
        }

        private void ReSercharHyperlink_Click(object sender, RoutedEventArgs e)
        {
            LrcSeacherWindow lrcwindow = new LrcSeacherWindow(CurrentSongInfo.SongInfo);
            lrcwindow.Show();
        }

        private void UpdatePlayingPageLrc()
        {
            try
            {
                LinkedListNode<LrcItem> _curLrc;
                if (CurLrcItem == null) return;
                else _curLrc = Playing.CurLrcItem;

                if (IsShowOlddRowLrc)
                {
                    if (CurLrcItem.Previous == null) return;
                    else _curLrc =CurLrcItem.Previous;
                }
                LrcList.SelectedItem = _curLrc.Value;
                DependencyObject lstitem = LrcList.ItemContainerGenerator.ContainerFromItem(LrcList.SelectedItem);
                int y = (int)((ListBoxItem)lstitem).TranslatePoint(new System.Windows.Point(0, 0), LrcList).Y;
                y = y - (int)ScrollLrc.ActualHeight / 2;
                ScrollLrc.ScrollToVerticalOffset(y);
            }
            catch (Exception e2)
            {
                MessageBox.Show("lrcListboxScroll_Next:" + e2.Message);
            }

        }
        
        private void UpdatePlayingPageBackground()
        {
            var curSongInfo = CurrentSongInfo;
            if (curSongInfo != null)
            {
                if (curSongInfo.SongInfo.SongType == 3) {
                    var bitmap = new System.Drawing.Bitmap(OsuLocalDataGeter.LoadOsuFileNameAs(1, curSongInfo.SongInfo.OsuPath));
                    UIhelper.SetBlurBitmapImage(BackgroundImage2, BackgroundImage1, bitmap);
                    //bitmap.Dispose();
                }
                else
                    new MusicTag().GetCurrentMusicFileJPGBufferAsync(curSongInfo.SongInfo).ContinueWith((r1) =>
                    {
                        if (r1.Result == null) return;
                        var bitmap = new MusicTag().GetBitmapFromBuffer(r1.Result);
                        UIhelper.SetBlurBitmapImage(BackgroundImage2, BackgroundImage1, bitmap);
                        //bitmap.Dispose();
                    });

                new MusicTag().GetCurrentColorAsync(curSongInfo.SongInfo).ContinueWith((r1) =>
                {
                    var cc = r1.Result;
                    Dispatcher.Invoke(() =>
                    {
                        CurrentLrcBrush = new SolidColorBrush(new Color() { A = cc.A, R = cc.R, G = cc.G, B = cc.B });
                        var br = MainWindow.CurMainWindowInstence.CurrentThemeBrush;
                        //MainWindow.CurMainWindowInstence.CurrentThemeBrush = new SolidColorBrush(
                        //    new Color() { A = 125, R = cc.R, G = cc.G, B = cc.B });
                    });
                });
            }

        }

        private void UnpdatePlayingNote(TimeSpan postion)
        {

            Random random = new Random(DateTime.Now.Millisecond);
            lock (notesViewData)
            {
                if (FirstNode != null && postion.Ticks > FirstNode.Value.StartTime)
                {
                    if (!string.IsNullOrWhiteSpace(FirstNode.Value.Content))
                    {
                        //如果保证了添加时的顺序则永远不需要二次进行排序
                        Dispatcher.Invoke(() =>
                    {
                        Note.SingleNote singleNoteWidget = new Note.SingleNote(FirstNode.Value);
                        singleNoteWidget.Margin = new Thickness(random.Next((int)(this.ActualWidth - singleNoteWidget.ActualWidth)), random.Next((int)(this.ActualHeight - singleNoteWidget.ActualHeight-32)), 0, 0);
                        //输出节点内容；
                        PlayingGrid.Children.Add(singleNoteWidget);
                    });
                    }
                    FirstNode = FirstNode.Next;
                }
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (CurrentSongInfo == null) return;
            new MusicTag().GetCurrentColorAsync(CurrentSongInfo.SongInfo).ContinueWith((r1) =>
            {
                var cc = r1.Result;
                Dispatcher.Invoke(() =>
                {
                    CurrentLrcBrush = new SolidColorBrush(new Color() { A = cc.A, R = cc.R, G = cc.G, B = cc.B });
                    var br = MainWindow.CurMainWindowInstence.CurrentThemeBrush;
                    MainWindow.CurMainWindowInstence.CurrentThemeBrush = new SolidColorBrush(
                        new Color() { A = 245, R = cc.R, G = cc.G, B = cc.B });
                });
            });

            CurPlayingInstance = null;
            UpdateLrcUIDeleLink -= UpdatePlayingPageLrc;
            PlayingPageBackgroundUpdateUIDeleLink -= UpdatePlayingPageBackground;
            PlayListBase.PlayingChanged -= OnPlayingChanged;
            PlayListBase.PlaybackContiune -= UnpdatePlayingNote;

        }

        private void ReturnMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.CurMainWindowInstence.frame.GoBack();
        }

        private void GoSource_Click(object sender, RoutedEventArgs e)
        {
            var cursong = PlayListBase.PlayListSongs[PlayListBase.PlayListIndex - 1];
            PlayListData PlayListData =null;
            if (cursong.Source.IndexOf("osuname_") ==0) {
                var f= cursong.Source.Remove(0, 8);
                PlayListData = MainWindow.CurMainWindowInstence.OsuData.FirstOrDefault(x => x.PlatListName == f);
            }
            if (cursong.Source.IndexOf("netid_") ==0)
            {
                var f = cursong.Source.Remove(0, 6);
                PlayListData = MainWindow.CurMainWindowInstence.NetCloudData.FirstOrDefault(x => x.PlayListId == f);
            }
            if (PlayListData == null) return;
            SongList.SetPlayListData(PlayListData);
            SongList.StartCmd.Enqueue("select_$currentplay");
            if (MainWindow.CurMainWindowInstence.frame.Source==null|| MainWindow.CurMainWindowInstence.frame.Source.OriginalString != "SongList.xml")
                MainWindow.CurMainWindowInstence.frame.Navigate(new Uri("SongList.xaml", UriKind.Relative));
            else MainWindow.CurMainWindowInstence.frame.Refresh();
        }

        private void PlayingGoDir_Click(object sender, RoutedEventArgs e)
        {
            SongInfoExpend songEx = PlayListBase.CurSongInfoEx;
            if (songEx != null)
            {
                System.Diagnostics.Process.Start("Explorer.exe", "/select," + songEx.SongInfo.SongPath);// Note: only 2 para !!!
            }
        }

        LinkedListNode<Note.NoteContentViewData> FirstNode;
        private void PlayingGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (CurrentSongInfo != null)
                notesViewData = new Note.NotesViewData(CurrentSongInfo.SongInfo);
            else notesViewData = null;
            if (notesViewData != null)
            {
                PlayListBase.PlaybackContiune += UnpdatePlayingNote;
                FirstNode = notesViewData.Notes.First;

            }
        }
        SongInfo editNoteItem;
        private void MusicNoteButton_Click(object sender, RoutedEventArgs e)
        {
            editNoteItem = CurrentSongInfo.SongInfo;
            Note.NoteContentViewData noteContentViewData
                = new Note.NoteContentViewData(PlayListBase.player.Position, "input");
            if(FirstNode!=null)//firstnode永远是大于播放时间的，所以新节点应该在其前面
                notesViewData.Notes.AddBefore(FirstNode, new LinkedListNode<Note.NoteContentViewData>(noteContentViewData));
            else notesViewData.Notes.AddLast(new LinkedListNode<Note.NoteContentViewData>(noteContentViewData));

            PlayingGrid.Children.Add(new Note.SingleNote(noteContentViewData) { Margin=new Thickness(12,24,0,0)});

        }

        private void PlayingGrid_LostFocus(object sender, RoutedEventArgs e)
        {
            if (e.Source.GetType() == typeof(Note.SingleNote))
            {   
                notesViewData.Save(editNoteItem ?? CurrentSongInfo.SongInfo);
            }
        }

        private void PlayingGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void PlayingGrid_MouseEnter(object sender, MouseEventArgs e)
        {
                editNoteItem = CurrentSongInfo.SongInfo;
        }
    }
}
