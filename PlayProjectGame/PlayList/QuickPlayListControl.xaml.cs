using PlayProjectGame.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;

namespace PlayProjectGame.PlayList
{
    /// <summary>
    /// QuickPlayListControl.xaml 的交互逻辑
    /// </summary>
    public partial class QuickPlayListControl : UserControl,IDisposable
    {
        static public QuickPlayListControl CurInstance;
        public ListBoxItem BeforPlayItem;
        Queue<SongInfoExpend> threadQueue = new Queue<SongInfoExpend>();
        public Thread[] tArr = new Thread[GlobalConfigClass._Config.PlaylistThreadNum];
        object thelock = new object();
        object cachlock = new object();

        static public bool pageClosed = false;

        private MainWindow mainWindow = MainWindow.CurMainWindowInstence;
        public MainWindow MainWindow { get => mainWindow; set => mainWindow = value; }

        public QuickPlayListControl()
        {
            InitializeComponent();
            CurInstance = this;

        }
        private void quickPlayListControl_Loaded(object sender, RoutedEventArgs e)
        {
            QuickPlayListBox.ItemsSource = PlayListBase.PlayListSongs;
            if (PlayListBase.PlayListTotal <= 0) return;
            QuickPlayListBox.SelectedItem = PlayListBase.PlayListSongs[PlayListBase.PlayListIndex - 1];
            QuickPlayListBox.ScrollIntoView(QuickPlayListBox.SelectedItem);
            //if (timer1 == null)
            //    timer1 = new Timer(delegate
            //    {//用于等待项的生成
            //        Dispatcher.Invoke((ThreadStart)delegate
            //        {
            //            SongInfoExpend sinfoEx = PlayListBase.PlayListSongs[PlayListBase.PlayListIndex - 1];
            //            ListBoxItem item = (ListBoxItem)QuickPlayListBox.ItemContainerGenerator.ContainerFromItem(sinfoEx);
            //            if (item == null) return;
            //            //item.ContentTemplate = CurInstance.Resources["PlayListItemSelectedDataTemplate"] as DataTemplate;
            //            //if (CurInstance.BeforPlayItem != null)
            //            //    CurInstance.BeforPlayItem.ContentTemplate = CurInstance.Resources["PlayListItemDataTemplate"] as DataTemplate;
            //            //CurInstance.BeforPlayItem = item;

            //        });

            //    }, null, 500, Timeout.Infinite);
            //else timer1.Change(500, Timeout.Infinite);
        }
        private void QuickPlayListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem item = Helper.UIhelper.FindPrent<ListBoxItem>((DependencyObject)e.OriginalSource);
            if (item == null) return;
            PlayListBase.PlayListIndex = QuickPlayListBox.SelectedIndex + 1;
            PlayListBase.PlayAndExceptionPass();

        }

        private void PlayListAlbumImageShowListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem item = Helper.UIhelper.FindPrent<ListBoxItem>((DependencyObject)e.OriginalSource);
            //myCollectionView.MoveCurrentTo(item);
            if (item == null) return;
            //QuickPlayListBox.ScrollIntoView(item.DataContext);
            //QuickPlayListBox.SelectedItem = item.DataContext;
            PlayListBase.PlayListIndex = QuickPlayListBox.SelectedIndex + 1;
            PlayListBase.PlayAndExceptionPass();
        }

        private void quickPlayListControl_Unloaded(object sender, RoutedEventArgs e)
        {
            CurInstance = null;
            pageClosed = true;

            foreach (var t in tArr)
            {
                if (t != null && t.IsAlive)
                    t.Abort();
            }

        }

        private void Image_Loaded(object sender, RoutedEventArgs e)
        {
            threadQueue.Enqueue(((Image)sender).DataContext as SongInfoExpend);
            if (threadQueue.Count > ConfigPage.GlobalConfig.PlaylistItemNum) threadQueue.Dequeue();
            for (int i = 0; i < tArr.Count(); i++)
                if (tArr[i] == null || !tArr[i].IsAlive)
                {
                    tArr[i] = new Thread(() => LoadImage());
                    tArr[i].Start();//开了更多线程后会产生io问题，音乐会卡
                }
        }
        private void LoadImage()
        {

            SongInfoExpend data;
            lock (thelock)
            {
                while (threadQueue.Count == 0)
                {
                    if (pageClosed) return;

                    Thread.Sleep(200);
                }
                if (threadQueue.Count > 0) data = threadQueue.Dequeue();
                else
                {
                    return;
                };
            }
            Image IMAGE = null;
            Dispatcher.Invoke(() =>
            {//测试元素是否在视口上，这种方式可能会有性能问题
                var listBoxItem = ((ListBoxItem)QuickPlayListBox.ItemContainerGenerator.ContainerFromItem(data));
                IMAGE = Helper.UIhelper.FindChild<Image>(listBoxItem, null);
            });
            if (IMAGE != null)
            {
                var srd = new SalientRegionDetection();
                System.IO.Stream outImageStream = null;
                byte[] imagebuff = null;
                System.Drawing.Bitmap GenerImage = null;

                //var ss = DateTime.Now;
                var path = Core.Cach.CachPool.GetPath(data.SongInfo);
                var spath = Core.Cach.CachPool.GetSDRImagePath(path);
                var apath = Core.Cach.CachPool.GetAbPath(path);
                if (GlobalConfigClass._Config.UseSRDImageCach && File.Exists(spath))
                {
                    lock(spath)
                        GenerImage = new System.Drawing.Bitmap(spath);
                }
                else
                {
                    if (data.SongInfo.SongType == 3)
                    {
                        spath = OsuLocalDataGeter.LoadOsuFileNameAs(1, data.SongInfo.OsuPath);
                        if (!string.IsNullOrWhiteSpace(spath))
                            GenerImage = srd.GetSRDFromPath(spath, 600, 200, "W", "jpg", new System.Drawing.Rectangle(0, 0, 475, 120), 215);
                        //System.Diagnostics.Debug.Print("文件：" + (ss - DateTime.Now).TotalSeconds.ToString());

                    }
                    else if (data.SongInfo.SongType == 2)
                    {
                        apath = Core.Cach.CachPool.GetAbPath(data.SongInfo);
                        if (GlobalConfigClass._Config.UseAlbumImageCach && File.Exists(apath))
                        {
                            GenerImage = srd.GetSRDFromPath(apath, 600, 200, "W", "jpg", new System.Drawing.Rectangle(0, 0, 475, 120), 215);
                        }
                        else
                        {
                            imagebuff = new Helper.MusicTag().GetJPGBuffer(data.SongInfo);
                            if (imagebuff == null) return;
                            var ms2 = new System.IO.MemoryStream(imagebuff);
                            GenerImage = srd.GetSRDFromStream(ms2, 600, 200, "W", "jpg", new System.Drawing.Rectangle(0, 0, 475, 120), 215);
                            ms2.Dispose();
                        }
                    }
                }
                if (GenerImage == null) return;
                outImageStream = new MemoryStream();
                GenerImage.Save(outImageStream, System.Drawing.Imaging.ImageFormat.Jpeg);

                if (outImageStream == null) return;
                outImageStream.Seek(0, SeekOrigin.Begin);
                Dispatcher.BeginInvoke((ParameterizedThreadStart)delegate (object outImageStreamx)
                {
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.StreamSource = (System.IO.Stream)outImageStreamx;
                    bi.EndInit();
                    IMAGE.Source = bi;
                }, outImageStream);

                if (GlobalConfigClass._Config.UseAlbumImageCach && imagebuff != null && !File.Exists(apath))
                    Core.Cach.CachPool.SetAb(data.SongInfo, imagebuff);
                if (GlobalConfigClass._Config.UseSRDImageCach && !File.Exists(spath))
                    Core.Cach.CachPool.SetSDRImageSource(data.SongInfo, GenerImage);
                GenerImage.Dispose();
            }
            LoadImage();
        }

        private void Image_Initialized(object sender, EventArgs e)
        {

        }

        private void PlayListGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var ev = e.OriginalSource as ScrollViewer;
            if (ev != null)
            {
                MainWindow.CurMainWindowInstence.frame.Navigate(new Uri("/Playing.xaml", UriKind.Relative));
            }
        }

        private void Image_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private void QuickPlayListBox_Drop(object sender, DragEventArgs e)
        {
            SongInfoExpend deste = ((FrameworkElement)e.OriginalSource).DataContext as SongInfoExpend;
            SongInfoExpend source = (SongInfoExpend)e.Data.GetData(typeof(SongInfoExpend));
            //if (deste == null) return;


            if (source.DataContainerName == "SongList")
            {
                int index = PlayListBase.PlayListSongs.IndexOf(deste) + 1;
                PlayListBase.AddToPlayList(source, index);
            }
            else
            {//当成本页面处理
                if (deste != null
                    && deste != source)
                    {
                    if (PlayListBase.PlayListSongs.Remove(source))
                    {
                        int index = PlayListBase.PlayListSongs.IndexOf(deste) + 1;
                        PlayListBase.AddToPlayList(source, index);
                        if (source.SongInfo == PlayListBase.CurSongInfoEx.SongInfo)
                        {//如果拖动的是当前播放歌曲，则移动正在播放位置
                            PlayListBase.PlayListIndex = index + 1;
                        }
                    }
                }
            }
            source.DataContainerName = "";
            if(deste!=null)
            deste.DataContainerName = "";
            FrameworkElement elemt = ((FrameworkElement)e.OriginalSource);
            elemt = Helper.UIhelper.FindPrent<Grid>(elemt);
            Helper.UIhelper.FindChild<Line>(elemt, "").Visibility = Visibility.Collapsed;
            elemt.Margin = new Thickness(0, 0, 0, 0);
        }

        private void QuickPlayListBox_DragEnter(object sender, DragEventArgs e)
        {
            FrameworkElement elemt = ((FrameworkElement)e.OriginalSource);
            elemt = Helper.UIhelper.FindPrent<Grid>(elemt, 2);
            if (elemt == null) return;
            elemt.Margin = new Thickness(-8, 0, 0, 0);
            Helper.UIhelper.FindChild<Line>(elemt, "").Visibility = Visibility.Visible;
        }

        private void QuickPlayListBox_DragLeave(object sender, DragEventArgs e)
        {
            FrameworkElement elemt = ((FrameworkElement)e.OriginalSource);
            elemt = Helper.UIhelper.FindPrent<Grid>(elemt);
            Helper.UIhelper.FindChild<Line>(elemt, "").Visibility = Visibility.Collapsed;
            elemt.Margin = new Thickness(0, 0, 0, 0);
            Debug.Print(e.OriginalSource.GetType().Name);
            //SongInfoExpend source = (SongInfoExpend)e.Data.GetData(typeof(SongInfoExpend));
            //source.DataContainerName = "";
        }

        private void quickPlayListControl_MouseLeave(object sender, MouseEventArgs e)
        {
            if(e.LeftButton== MouseButtonState.Released&&e.RightButton== MouseButtonState.Released)
                this.Visibility = Visibility.Collapsed;

        }

        private void QuickPlayListBox_Loaded(object sender, RoutedEventArgs e)
        {
            QuickPlayListBox.ScrollIntoView(PlayListBase.CurSongInfoEx);

        }

        public void Dispose()
        {
            pageClosed = true;
        }
    }
}
