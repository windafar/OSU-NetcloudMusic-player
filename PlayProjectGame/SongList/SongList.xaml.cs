using PlayProjectGame.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using PlayProjectGame.PlayList;
using System.Windows.Controls.Primitives;
using ImageFilter;
using PlayProjectGame.Data;
using System.Diagnostics;

namespace PlayProjectGame
{
    /// <summary>
    /// SongList.xaml 的交互逻辑
    /// </summary>
    public partial class SongList : Page, IProvideCustomContentState
    {
        //拓展的ListView属性放到自定义控件xaml有时候报名称已被使用的异常，轮流替换命名空间编译又没问题了，感觉像是编译器bug，先扔这儿吧

        public static readonly DependencyProperty HearderBackgroudProperty = DependencyProperty.Register("HearderBackgroud", typeof(Brush), typeof(SongList), new PropertyMetadata(Brushes.Transparent));
        public static readonly DependencyProperty ListViewBackgroudProperty = DependencyProperty.Register("ListViewBackgroud", typeof(Brush), typeof(SongList), new PropertyMetadata(Brushes.Transparent));

        private MainWindow mainWindow = MainWindow.CurMainWindowInstence;
        public MainWindow MainWindow { get => mainWindow; set => mainWindow = value; }
        public Brush HearderBackgroud
        {
            get => (Brush)(GetValue(HearderBackgroudProperty));
            set
            {
                SetValue(HearderBackgroudProperty, value);

            }
        }
        public Brush ListViewBackgroud
        {
            get => (Brush)(GetValue(ListViewBackgroudProperty));
            set
            {
                SetValue(ListViewBackgroudProperty, value);

            }
        }
        Grid _Grid;
        public PlayListData PLD;
        static public PlayListData PLDStart;
        //放在最耗时的钩子上用来执行加载完成时的命令
        static public Queue<string> StartCmd = new Queue<string>();

        internal static void SetPlayListData(PlayListData pld)
        {
            PLDStart = pld;
        }
        public SongList()
        {
            InitializeComponent();
            this.PLD = PLDStart;

        }

        //Timer tt;

        Thread ImageThread;
        /// <summary>
        /// 该方法用于从歌曲内部获取歌单的图片，未完成
        /// </summary>
        public void GetImage(string Pid)
        {
            ImageThread = new Thread((ThreadStart)delegate
            {
                byte[] AbImageBuff = null;
                long result;
                if (GlobalConfigClass._Config.UseCouldMusicSongListCover && long.TryParse(Pid, out result))
                {
                    AbImageBuff = Data.NetCloudMusicSource.GetCloudMusicSource().GetSongListImage(PLD.PlayListNetImageUri);
                    //之后处理
                }
                if (AbImageBuff == null)
                {
                    for (int i = 0; i < PLD.Songs.Count; i++)
                    {
                        if (PLD.Songs[i].SongInfo.SongType == 1 || PLD.Songs[i].SongInfo.SongType == 2)
                        {
                            if (PLD.Songs[i].SongInfo.SongPath != null)
                            {
                                MusicTag MT = new MusicTag();
                                AbImageBuff = MT.GetJPGBuffer(PLD.Songs[i].SongInfo, 10240);
                                if (AbImageBuff != null)
                                {
                                    var ms = new MemoryStream(AbImageBuff);
                                    BitmapImage img;
                                    try
                                    {
                                        img = Helper.UIhelper.ConverTotBitmapImage(new System.Drawing.Bitmap(ms));
                                        img.Freeze();
                                        Dispatcher.BeginInvoke((ThreadStart)(() => { SongListImage.Source = img; }));
                                    }
                                    catch(Exception)
                                    {
                                        img = null;
                                      
                                    }
                                    finally
                                    {
                                        ms.Dispose();
                                    }
                                    break;
                                }
                            }
                        }
                        else if (PLD.Songs[i].SongInfo.SongType == 3)
                        {
                            if (PLD.Songs[i].SongInfo.OsuPath != null)
                            {
                                var osubgPath = OsuLocalDataGeter.LoadOsuFileNameAs(1, PLD.Songs[i].SongInfo.OsuPath);
                                if (osubgPath == "") continue;
                                if (File.Exists(osubgPath))
                                {
                                    AbImageBuff = File.ReadAllBytes(osubgPath);

                                    var GenerImage = new SalientRegionDetection().GetSRDFromPath(osubgPath, 256, 256, "H", "jpg", new System.Drawing.Rectangle(0, 0, 256, 256), 210);
                                    var img = Helper.UIhelper.ConverTotBitmapImage(GenerImage);
                                    img.Freeze();

                                    Dispatcher.BeginInvoke((ThreadStart)(() => { SongListImage.Source = img; }));

                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    var ms = new MemoryStream(AbImageBuff);
                    var img = Helper.UIhelper.ConverTotBitmapImage(new System.Drawing.Bitmap(ms));
                    img.Freeze();
                    ms.Dispose();
                    Dispatcher.BeginInvoke((ThreadStart)(() => { SongListImage.Source = img; }));
                }
                if (AbImageBuff == null)
                {
                    Dispatcher.BeginInvoke((ThreadStart)delegate
                    {
                        SongListImage.Source = null;
                    });
                    return;
                }

                Stream stream = new MemoryStream(AbImageBuff);
                ///heard背景
                double headerHeight = 36;// scrollViewer==null?36:scrollViewer.ActualHeight;
                headerHeight = headerHeight * UIhelper.GetSystemDpi().Width * 0.01;
                var SRDBrushImage = GetSRDFromStream(stream, new System.Drawing.Rectangle(0, 0, 300, (int)Math.Round(headerHeight)));
                SRDBrushImage.Freeze();
                //list背景
                System.Drawing.Bitmap srcBitmap = null;
                if (GlobalConfigClass._Config.UseSongListPageBackground)
                {
                    try
                    {
                        srcBitmap = new System.Drawing.Bitmap(stream);
                    }
                    catch (ArgumentException)
                    {
                        srcBitmap = new System.Drawing.Bitmap(128, 128);
                    }
                    if (srcBitmap.Width > 900)
                    {
                        MemoryStream memoryStream = new MemoryStream();
                        ImageBasic.BasicMethodClass.MakeThumbnail(srcBitmap, memoryStream, 900, 900, "W", "jpg");
                        srcBitmap.Dispose();
                        srcBitmap = new System.Drawing.Bitmap(memoryStream);
                        memoryStream.Dispose();
                    }
                    srcBitmap = SketchFilter.Sketch(srcBitmap, 0.13, 0, 230, false);
                    //ImageBasic.BasicMethodClass.WriteEdgeGradual(srcBitmap);//目的是融合图片，有问题，考虑数学上的办法（拉普拉斯和泊松）

                    srcBitmap = ImageBasic.BasicMethodClass.AddSpace(srcBitmap, 0, 36, 0, 24);
                }
                stream.Dispose();
                Dispatcher.BeginInvoke((ThreadStart)delegate
                {
                    HearderBackgroud = new ImageBrush(SRDBrushImage) { Stretch = Stretch.None, AlignmentX = AlignmentX.Right, AlignmentY = AlignmentY.Center };
                    if (GlobalConfigClass._Config.UseSongListPageBackground)
                        ListViewBackgroud = new ImageBrush(UIhelper.ConverTotBitmapImage(srcBitmap)) { Stretch = Stretch.None, AlignmentY = AlignmentY.Center, AlignmentX = AlignmentX.Right };
                    else ListViewBackgroud = Brushes.Transparent;
                });
            });
            if (ImageThread.ThreadState == System.Threading.ThreadState.Unstarted || ImageThread.ThreadState == System.Threading.ThreadState.Stopped)
                ImageThread.Start();
            else
                ImageThread.Abort();
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke((ThreadStart)delegate
            {
                SongListListView.ItemsSource = PLD.Songs;
                SongListName.Text = PLD.PlatListName;
                SongListCreator.Text = PLD.UersName;
                SongListInfo.Text = PLD.PlayListInfo;
                RemoveLoalNotExistSQButton.Visibility = PLD.PlayListType == 2 ? Visibility.Visible : Visibility.Collapsed;
                //SongListHistory.Visibility = PLD.PlayListType == 2 ? Visibility.Visible : Visibility.Collapsed; ;
                ImportSong.Visibility = PLD.PlayListType == 2 ? Visibility.Visible : Visibility.Collapsed;
                GetImage(PLD.PlayListId);
                view = (ListCollectionView)CollectionViewSource.GetDefaultView(SongListListView.ItemsSource);
                Point point = SongListInfo.TranslatePoint(new Point(0, 0), SongListListView);
                SongListInfoSrcoller.Height = Math.Abs(point.Y);
                _Grid = UIhelper.FindPrent<Grid>(SongListListView);

            });
        }

        ListCollectionView view;//此视图用于过滤

        private void SearcherBox_TextChanged(object sender, TextChangedEventArgs e) => view.Filter = new Predicate<object>(FilterAll);
        public bool FilterAll(object item)
        {
            var it = ((SongInfoExpend)item).SongInfo;
            var result = false;
            if (!result)
            {
                if (it.SongName == null) return false;
                result = it.SongName.IndexOf(SearcherBox.Text, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            if (!result)
            {
                if (it.SongAlbum == null) return false;

                result = it.SongAlbum.IndexOf(SearcherBox.Text, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            if (!result)
            { if (it.SongArtist == null) return false;
                result = it.SongArtist.IndexOf(SearcherBox.Text, StringComparison.OrdinalIgnoreCase) >= 0;
            }
                return result;
        }

        private void SongListListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //如果是双击的Item
            SongInfoExpend sinfo;
            try
            {
                sinfo = ((FrameworkElement)e.OriginalSource).DataContext as SongInfoExpend;
            }
            catch { return; }
            if (sinfo != null && sinfo.SongInfo.SongPath != null)
            {
                PlayListBase.AddToPlayList(sinfo, true);
                PlayListBase.PlayAndExceptionPass();
            }
        }


        int CH_GridViewColumn_Click = 0;
        private void SongListListView_Click(object sender, RoutedEventArgs e)
        {

            if (e.OriginalSource.GetType() != typeof(GridViewColumnHeader)) return;
            GridViewColumnHeader s = (GridViewColumnHeader)(e.OriginalSource);
            if (view == null) return;

            TextBlock o = UIhelper.FindChild<TextBlock>(s, null);
            if (o == null) return;
            if (view.SortDescriptions.Count != 0)
                view.SortDescriptions.Clear();
            if (o.Name == "Index")
            {
                view.SortDescriptions.Add(new SortDescription("SongInfoIndex", CH_GridViewColumn_Click % 2 == 0 ? ListSortDirection.Ascending : ListSortDirection.Descending));
            }
            else if (o.Name == "SongNameGridLable")
                view.SortDescriptions.Add(new SortDescription("SongInfo.SongName", CH_GridViewColumn_Click % 2 == 0 ? ListSortDirection.Ascending : ListSortDirection.Descending));
            else if (o.Name == "ArtistGridLable")
                view.SortDescriptions.Add(new SortDescription("SongInfo.SongArtist", CH_GridViewColumn_Click % 2 == 0 ? ListSortDirection.Ascending : ListSortDirection.Descending));
            else if (o.Name == "SongAlbumGridHead")
                view.SortDescriptions.Add(new SortDescription("SongInfo.SongAlbum", CH_GridViewColumn_Click % 2 == 0 ? ListSortDirection.Ascending : ListSortDirection.Descending));

            if (CH_GridViewColumn_Click > 1000) CH_GridViewColumn_Click = 0;
            else CH_GridViewColumn_Click++;

        }
        /// <summary>
        /// 用于恢复历史状态，在历史中导航时才会被触发
        /// </summary>
        /// <param name="state"></param>
        private void ReplySongListPage_Itself(PlayListJournalEntry state)
        {
            PLD = state.PLD;
            SongListListView.ItemsSource = state.PLD.Songs;
            SongListName.Text = state.PLD.PlatListName;
            SongListInfo.Text = state.PLD.PlayListInfo;
            SongListCreator.Text = state.PLD.UersName;
            GetImage(state.PLD.PlayListId);
            if (state.curSelected != null)
            {
                SongListListView.SelectedItem = state.curSelected;
                SongListListView.ScrollIntoView(state.curSelected);

            }
        }
        /// <summary>
        /// 用于保存历史状态
        /// </summary>
        /// <returns></returns>
        public CustomContentState GetContentState() => new PlayListJournalEntry(PLD, ReplySongListPage_Itself, SongListListView.SelectedItem);

        private void PlaySongListButton_Click(object sender, RoutedEventArgs e)
        {
            PlayListBase.PlayListSongs.Clear();
            PlayListBase.PlayListIndex = 1;
            PlayListBase.AddToPlayList(PLD.Songs, true);

            //tt = new Timer(delegate
            //{
            PlayListBase.PlayAndExceptionPass();
            //    PlayListBase.PlayListIndex++;
            //}, null, 2000, 2000);


        }

        private void AddSongListAddButton_Click(object sender, RoutedEventArgs e) => PlayListBase.AddToPlayList(PLD.Songs, true);

        private void SongListAddMenu_Click(object sender, RoutedEventArgs e)
        {
            if (SongListListView.SelectedItem is SongInfoExpend songEx)
            {
                for (int i = SongListListView.SelectedItems.Count - 1; i >= 0; i--)
                    PlayListBase.AddToPlayList((SongInfoExpend)SongListListView.SelectedItems[i], false);
            }
        }

        private void RemoveLoalNotExistSQButton_Click(object sender, RoutedEventArgs e)
        {
            var result = PLD.Songs.Where(x => (!x.SongInfo.LocalFlac && x.SongInfo.RemoteFlac));
            var Num = 0;
            if (MessageBox.Show("将要移除" + result.Count() + "项，请至少在程序所在磁盘上留足"+ result.Count()*20+"Mkb的空间", "确认", MessageBoxButton.YesNo)
                == MessageBoxResult.Yes)
            {
                PLD.Songs.Where(x => (!x.SongInfo.LocalFlac && x.SongInfo.RemoteFlac)).ToList()
                    .ForEach(x =>
                    {
                        if (File.Exists(x.SongInfo.SongPath))
                        {
                            var t = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            var fi = new FileInfo(x.SongInfo.SongPath);
                            var dir = System.Environment.CurrentDirectory + "\\back\\" + OtherHelper.ReplaceValidFileName(t) + "\\";
                            if (!Directory.Exists(dir))
                            {
                                Directory.CreateDirectory(dir);
                            }
                            try
                            {
                                fi.MoveTo(dir + fi.Name);
                            }
                            catch (Exception ex) 
                            {

                                MessageBox.Show("移除"+fi.FullName+"时发生错误，文件未移动："+ex.Message);
                                Num--;
                                
                            }
                            Num++;
                        }
                        x.SongInfo.SongPath = null;
                    });
                MessageBox.Show("完成，已删除" + Num + "项,可刷新页面查看");
            };
        }

        private void SongListListView_Loaded(object sender, RoutedEventArgs e)
        {
            var scrollbar = UIhelper.FindChild<ScrollBar>(SongListListView, "PART_VerticalScrollBar");
            Binding MaximumScrollBinding = new Binding() { NotifyOnTargetUpdated = true, Source = scrollbar, Path = new PropertyPath("Maximum"), Mode = BindingMode.OneWay, Converter = new MaximumScrollConver(), ConverterParameter = _Grid };
            Binding ValueScrollBinding = new Binding() { Source = scrollbar, Path = new PropertyPath("Value"), Mode = BindingMode.OneWay };
            Binding MinimumScrollBinding = new Binding() { Source = scrollbar, Path = new PropertyPath("Minimum"), Mode = BindingMode.OneWay };
            Binding ViewPortScrollBinding = new Binding() { Source = SongListPageScrollBar, Path = new PropertyPath("ActualHeight"), Mode = BindingMode.OneWay };

            SongListPageScrollBar.SetBinding(ScrollBar.MaximumProperty, MaximumScrollBinding);
            SongListPageScrollBar.SetBinding(ScrollBar.ValueProperty, ValueScrollBinding);
            SongListPageScrollBar.SetBinding(ScrollBar.MinimumProperty, MinimumScrollBinding);
            SongListPageScrollBar.SetBinding(ScrollBar.ViewportSizeProperty, ViewPortScrollBinding);


            while (StartCmd.Count > 0)
            {
                var cmd = StartCmd.Dequeue().Split('_');
                if (cmd.Length < 2) return;
                switch (cmd[0])
                {
                    case "select":
                        if (cmd[1] == "$currentplay")
                        {
                            SongListListView.ScrollIntoView(PlayListBase.CurSongInfoEx);
                            SongListListView.SelectedItem = PlayListBase.CurSongInfoEx;
                        }
                        else
                        {
                            var item = this.PLD.Songs.FirstOrDefault(x => x.SongInfo.SongName == cmd[1]);
                            SongListListView.ScrollIntoView(item);
                            SongListListView.SelectedItem = item;

                        }
                        break;

                }
            }
        }

        private void SongListListView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            e.Handled = true;
        }

        private void SongListListView_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Source.GetType() == typeof(TextBlock))
            {
                return;
            }
            e.Handled = true;
            var value = -e.Delta / 12;
            var scrollbar = UIhelper.FindChild<ScrollBar>(SongListListView, "PART_VerticalScrollBar");
            var scroller = UIhelper.FindChild<ScrollViewer>(SongListListView, null);
            //scroller.ScrollToVerticalOffset(scrollbar.Value);
            //SongListPageScrollBar.Value = scrollbar.Value;
            if (value > 0 && scroller.ExtentHeight <= scroller.ViewportHeight)
            {//滚动视图大小小于视口大小，隐藏滚动条

            }
            else
            {//移动top
                if (value > 0)//向下滚动
                    if (Math.Abs(_Grid.Margin.Top) < 220)
                    {
                        var newCurMargin = new Thickness(0, _Grid.Margin.Top - value, 0, 0);
                        if (-newCurMargin.Top > SongListPageScrollBar.Maximum)
                            return;//抹除误差，这个误差应该是由于最大滚动数的改变造成，
                                   //直接使用绑定好的滚动条数据即可
                        _Grid.Margin = newCurMargin;
                        SongListPageScrollBar.Value = -newCurMargin.Top;
                    }
                    else
                    {
                        _Grid.Margin = new Thickness(0, -220, 0, 0);
                        //scroller.ScrollToVerticalOffset((SongListPageScrollBar.Value - 240 * (1 - SongListPageScrollBar.Value / SongListPageScrollBar.Maximum)) / SongListPageScrollBar.Maximum * scrollbar.Maximum);
                        scroller.ScrollToVerticalOffset(value + scroller.VerticalOffset);
                        SongListPageScrollBar.Value = (scroller.VerticalOffset + 240 * (1 - scroller.VerticalOffset / scrollbar.Maximum)) / scrollbar.Maximum * SongListPageScrollBar.Maximum;
                    }
                if (value < 0)//向上滚动
                    if (_Grid.Margin.Top <= 0 && scrollbar.Value == 0)
                    {
                        _Grid.Margin = new Thickness(0, _Grid.Margin.Top - value, 0, 0);
                        SongListPageScrollBar.Value = -_Grid.Margin.Top;
                        if (SongListPageScrollBar.Value == 0) _Grid.Margin = new Thickness(0, 0, 0, 0);
                    }
                    else
                    {
                        scroller.ScrollToVerticalOffset(value + scroller.VerticalOffset);
                        if (scrollbar.Maximum == 0) return;
                        SongListPageScrollBar.Value = (scroller.VerticalOffset + 240 * (1 - scroller.VerticalOffset / scrollbar.Maximum)) / scrollbar.Maximum * SongListPageScrollBar.Maximum;

                    }
            }
        }
        double oldValue = 0;


        private void SongListPageScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            var value = e.NewValue - oldValue;
            oldValue = e.NewValue;
            var scrollbar = UIhelper.FindChild<ScrollBar>(SongListListView, "PART_VerticalScrollBar");
            var scroller = UIhelper.FindChild<ScrollViewer>(SongListListView, null);
            if (value > 0 && scroller.ExtentHeight <= scroller.ViewportHeight)
            {//滚动视图大小小于视口大小，隐藏滚动条

            }
            else
            {//移动top
                var _Grid = UIhelper.FindPrent<Grid>(SongListListView);
                if (value > 0)//向下滚动
                    if (Math.Abs(_Grid.Margin.Top) < 220)
                        _Grid.Margin = new Thickness(0, _Grid.Margin.Top - value, 0, 0);
                    else
                    {
                        _Grid.Margin = new Thickness(0, -220, 0, 0);
                        scroller.ScrollToVerticalOffset((SongListPageScrollBar.Value - 240 * (1 - SongListPageScrollBar.Value / SongListPageScrollBar.Maximum)) / SongListPageScrollBar.Maximum * scrollbar.Maximum);
                    }
                if (value < 0)//向上滚动
                    if (_Grid.Margin.Top <= 0 && scrollbar.Value == 0)
                    {
                        _Grid.Margin = new Thickness(0, _Grid.Margin.Top - value, 0, 0);
                        if (SongListPageScrollBar.Value == 0) _Grid.Margin = new Thickness(0, 0, 0, 0);
                    }
                    else
                    {
                        scroller.ScrollToVerticalOffset((SongListPageScrollBar.Value - 240 * (1 - SongListPageScrollBar.Value / SongListPageScrollBar.Maximum)) / SongListPageScrollBar.Maximum * scrollbar.Maximum);
                    }
            }
        }

        public BitmapImage GetSRDFromStream(Stream filestream, System.Drawing.Rectangle R)
        {
            var GenerImage = new SalientRegionDetection().GetSRDFromStream(filestream, 600, 200, "W", "jpg", R, 210);
            if (GenerImage == null) return new BitmapImage();

            var stffimage = new System.Drawing.Bitmap("AppResource\\overlap1.png");
            ImageBasic.BasicMethodClass.Overlap(GenerImage, stffimage);
            GenerImage = ImageBasic.BasicMethodClass.AddSpace(GenerImage, 1256, 0, 0, 0);

            stffimage.Dispose();

            return UIhelper.ConverTotBitmapImage(GenerImage);
        }

        private void SongListListView_DragEnter(object sender, DragEventArgs e)
        {
            if (QuickPlayListControl.CurInstance != null)
            {
                QuickPlayListControl.CurInstance.quickPlayListControl.Visibility = Visibility.Visible;
                QuickPlayListControl.CurInstance.QuickPlayListBox.ScrollIntoView(PlayListBase.CurSongInfoEx);

            }
        }

        private void SongListListView_DragLeave(object sender, DragEventArgs e)
        {
            SongInfoExpend source = (SongInfoExpend)e.Data.GetData(typeof(SongInfoExpend));
            source.dataContainer = SongInfoExpend.DataContainerType.SongList;
        }

        private void SongListGoDir_Click(object sender, RoutedEventArgs e)
        {
            SongInfoExpend songEx = SongListListView.SelectedItem as SongInfoExpend;
            if (songEx != null)
            {
                System.Diagnostics.Process.Start("Explorer.exe", "/select," + songEx.SongInfo.SongPath);// Note: only 2 para !!!
            }
        }

        private void ExportSongList_Click(object sender, RoutedEventArgs e)
        {
            File.WriteAllLines(OtherHelper.ReplaceValidFileName(PLD.PlatListName) + ".m3u8", PLD.Songs.Select(x => x.SongInfo.SongPath).ToArray(), UTF8Encoding.UTF8);
            File.WriteAllLines(OtherHelper.ReplaceValidFileName(PLD.PlatListName) + ".m3u8", PLD.Songs.Select(x => x.SongNameAndArtist).ToArray(), UTF8Encoding.UTF8);

        }

        private void SongListPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.VisualScrollableAreaClip = new Rect(0, 0, this.ActualWidth, ActualHeight);

        }
        private void ShowSearcherBox(object sender, RoutedEventArgs e)
        {
            if (SearcherBox.Visibility == Visibility.Visible)
                SearcherBox.Visibility = Visibility.Collapsed;
            else
                SearcherBox.Visibility = Visibility.Visible;
        }

        private void History_Click(object sender, RoutedEventArgs e)
        {
            var dir = new DirectoryInfo(GlobalConfigClass.DIR_CLOUDMUSIC_SONGLISTHISTORY);
            if (!dir.Exists) dir.Create();
            if (dir.GetDirectories().Count() == 0) return;
            string path = dir.GetDirectories().Last().FullName + "\\" + OtherHelper.ReplaceValidFileName(PLD.PlatListName) + PLD.PlayListId;
            var sub = new DirectoryInfo(path);
            if (sub.Exists)
            {
                TimeLineListBox.ItemsSource = sub.GetFiles().OrderBy(x => x.CreationTime)
                    .Select(x => new { Time = x.Name, Path = x.FullName })
                    ;
            }
            if (TimeLineListBox.Visibility == Visibility.Visible)
                TimeLineListBox.Visibility = Visibility.Collapsed;
            else
                TimeLineListBox.Visibility = Visibility.Visible;
        }

        private void HistoryTime_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var dataconext = ((TextBlock)sender).DataContext as dynamic;
            string filepath = dataconext.Path;
            PlayListData pld = null;
            try
            {
                pld = OtherHelper.ReadXMLObj<PlayListData>(File.ReadAllText(filepath));
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "读取历史失败"); }
            if (pld != null)
            {
                if (NavigationService.Content != null)
                {
                    SongList Pr = NavigationService.Content as SongList;
                    if (Pr != null)
                    {
                        NavigationService.AddBackEntry(new PlayListJournalEntry(Pr.PLD, MainWindow.ReplySongListPage, SongListListView.SelectedItem));
                    }
                }
                pld.PlatListName += "_" + dataconext.Time;
                SongList.SetPlayListData(pld);
                Uri NextUri = new Uri("SongList/SongList.xaml", UriKind.Relative);
                if (NavigationService.Source == null || NavigationService.Source.OriginalString != "SongList/SongList.xaml")
                    NavigationService.Navigate(NextUri);
                else NavigationService.Refresh();
            }
        }

        private void SongAlbumLinkClick(object sender, RoutedEventArgs e)
        {
            if (NavigationService == null) return;
            var data = ((TextBlock)sender).DataContext as SongInfoExpend;
            string albm = data.SongInfo.SongAlbum;
            string[] arts = data.SongInfo.GetSongArtists;
            var netcloud = mainWindow.NetCloudData.AsParallel().SelectMany(x => x.Songs).Where(x => x.SongInfo.SongAlbum == albm).ToList();
            var osu = mainWindow.OsuData.AsParallel().SelectMany(x => x.Songs).Where(x => x.SongInfo.SongAlbum == albm).ToList();
            netcloud.AddRange(osu);
            netcloud = netcloud.GroupBy(x => x.SongInfo.SongName).Select(x => x.First()).ToList();
            PlayListData pld = null;
            pld = new PlayListData() { PlatListName = "专辑：" + albm, PlayListType = 4, Songs = netcloud };
            if (NavigationService.Content != null)
            {
                SongList Pr = NavigationService.Content as SongList;
                if (Pr != null)
                {
                    NavigationService.AddBackEntry(new PlayListJournalEntry(Pr.PLD, MainWindow.ReplySongListPage, SongListListView.SelectedItem));
                }
            }
            SongList.SetPlayListData(pld);
            Uri NextUri = new Uri("SongList/SongList.xaml", UriKind.Relative);
            if (NavigationService.Source == null || NavigationService.Source.OriginalString != "SongList/SongList.xaml")
                NavigationService.Navigate(NextUri);
            else NavigationService.Refresh();
        }

        private void SongArtistLinkCilcked(object sender, MouseButtonEventArgs e)
        {
            if (NavigationService == null) return;
            var data = ((TextBlock)sender).DataContext as SongInfoExpend;
            string albm = data.SongInfo.SongAlbum;
            string[] arts = data.SongInfo.GetSongArtists;
            arts = arts.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            if (arts.Length == 1)
            {
                var netcloud = MainWindow.CurMainWindowInstence.NetCloudData.AsParallel().SelectMany(x => x.Songs).Where(x => arts[0].IndexExistOfAny(x.SongInfo.GetSongArtists)).ToList();
                var osu = MainWindow.CurMainWindowInstence.OsuData.AsParallel().SelectMany(x => x.Songs).Where(x => arts[0].IndexExistOfAny(x.SongInfo.GetSongArtists)).ToList();
                netcloud.AddRange(osu);
                netcloud = netcloud.GroupBy(x => x.SongInfo.SongName).Select(x => x.First()).ToList();
                if (NavigationService.Content != null)
                {
                    SongList Pr = NavigationService.Content as SongList;
                    if (Pr != null)
                    {
                        NavigationService.AddBackEntry(new PlayListJournalEntry(Pr.PLD, MainWindow.ReplySongListPage, SongListListView.SelectedItem));
                    }
                }

                PlayListData pld = null;
                pld = new PlayListData() { PlatListName = "艺术家：" + arts[0], PlayListType = 5, Songs = netcloud };
                SongList.SetPlayListData(pld);
                Uri NextUri = new Uri("SongList/SongList.xaml", UriKind.Relative);
                if (NavigationService.Source == null || NavigationService.Source.OriginalString != "SongList/SongList.xaml")
                    NavigationService.Navigate(NextUri);
                else NavigationService.Refresh();

            }
            else
            {
                ArtsSelect artsSelect = new ArtsSelect(arts);
                artsSelect.Show();
            }
        }

        private void DetailedMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ((MenuItem)sender).DataContext = SongListListView.SelectedItem;


        }

        private void ImportSong_Click(object sender, RoutedEventArgs e)
        {
            var selecteddata = UIhelper.SelectFile("audio");
            if (selecteddata != null)
                PLD.Songs.InsertRange(SongListListView.SelectedIndex == -1 ? 0 : SongListListView.SelectedIndex, selecteddata.Select(x =>
                     {
                         var track = new MusicTag().GetAudioTrack(x);
                        //var codec = PlayListBase.player.GetCodecInfo(x, "");
                        return new SongInfoExpend(new SongInfoExpend
                         {
                             SongInfo = new SongInfo
                             {
                                 SongType = 1,
                                 SongPath = x,
                                 SongAlbum = track == null ? ""  : track.Album,
                                 SongArtist = track == null ? "" : track.JoinedArtists,
                                 SongId = x,
                                 SongName = track == null ? new FileInfo(x).Name : track.Title,
                             },
                             dataContainer = SongInfoExpend.DataContainerType.SongList,
                             Source = "localfile_" + PLD.PlayListId,
                             SourceName = PLD.PlatListName,
                             FileTime = new FileInfo(x).CreationTime.Ticks,
                             SongInfoIndex = PLD.Songs.Count
                        });
                     }));
            CouldMusicLocalDataGeter.backup();
            NavigationService.Refresh();
        }

        private void SongToSongList_Click(object sender, RoutedEventArgs e)
        {
            var wind = new UserDefinedControl.CreatePlayList();
            wind.Owner = this.mainWindow;
            wind.songInfoExpends = SongListListView.SelectedItems.Cast<SongInfoExpend>();
            wind.Show();
        }

        private void PlayListDelMenu_Click(object sender, RoutedEventArgs e)
        {
            if (SongListListView.SelectedItem != null)
            {
                PLD.Songs.Remove(SongListListView.SelectedItem as SongInfoExpend);
                NavigationService.Refresh();
            }
        }
    }
}

