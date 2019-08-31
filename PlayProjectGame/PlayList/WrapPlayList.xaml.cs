using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PlayProjectGame.Helper;
using PlayProjectGame.PlayList;
using System.IO;
using Communication;
using PlayProjectGame.Data;

namespace PlayProjectGame.PlayList
{
    /// <summary>
    /// WrapPlayList.xaml 的交互逻辑
    /// </summary>
    public partial class WrapPlayList : Page,IDisposable
    {
        public ListBoxItem BeforPlayItem;
        Queue<SongInfoExpend> threadQueue = new Queue<SongInfoExpend>();
       public Thread[] tArr = new Thread[ConfigPage.GlobalConfig.PlaylistThreadNum];
        object thelock = new object();
        static public bool pageClosed = false;

        private MainWindow mainWindow = MainWindow.CurMainWindowInstence;
        public MainWindow MainWindow { get => mainWindow; set => mainWindow = value; }
        void OnPlayChanged(SongInfoExpend CurSongInfoEx) {
            Dispatcher.BeginInvoke((ThreadStart)delegate
            {
                WrapPlayListBox.ScrollIntoView(CurSongInfoEx);
                WrapPlayListBox.SelectedItem = CurSongInfoEx;//选择为当前播放项
            });

        }

        public WrapPlayList()
        {
            InitializeComponent();
            
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            pageClosed = false;
            PlayListBase.PlayingChanged += OnPlayChanged;
            WrapPlayListBox.ItemsSource = PlayListBase.PlayListSongs;
            if (PlayListBase.PlayListTotal <= 0) return;
            WrapPlayListBox.SelectedItem = PlayListBase.PlayListSongs[PlayListBase.PlayListIndex - 1];
            WrapPlayListBox.ScrollIntoView(WrapPlayListBox.SelectedItem);
            //if (timer1 == null)
            //    timer1 = new Timer(delegate
            //    {//用于等待项的生成
            //        Dispatcher.Invoke((ThreadStart)delegate
            //        {
            //            SongInfoExpend sinfoEx = PlayListBase.PlayListSongs[PlayListBase.PlayListIndex - 1];
            //            ListBoxItem item = (ListBoxItem)WrapPlayListBox.ItemContainerGenerator.ContainerFromItem(sinfoEx);
            //            if (item == null) return;
            //            //item.ContentTemplate = CurInstance.Resources["PlayListItemSelectedDataTemplate"] as DataTemplate;
            //            //if (CurInstance.BeforPlayItem != null)
            //            //    CurInstance.BeforPlayItem.ContentTemplate = CurInstance.Resources["PlayListItemDataTemplate"] as DataTemplate;
            //            //CurInstance.BeforPlayItem = item;

            //        });

            //    }, null, 500, Timeout.Infinite);
            //else timer1.Change(500, Timeout.Infinite);
        }
        private void WrapSongList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem item = Helper.UIhelper.FindPrent<ListBoxItem>((DependencyObject)e.OriginalSource);
            if (item == null) return;
            PlayListBase.PlayListIndex = WrapPlayListBox.SelectedIndex + 1;
            PlayListBase.PlayAndExceptionPass();

        }

        private void MenuItem_UserSongList_Click(object sender, RoutedEventArgs e)
        {
            var LIST = WrapPlayListBox.SelectedItems;
            int total = LIST.Count;
            SongInfoExpend curinfo = PlayListBase.PlayListSongs[PlayListBase.PlayListIndex - 1];
            for (int i = 0; i < total; i++)
            {
                PlayListBase.PlayListSongs.Remove((SongInfoExpend)LIST[0]);
            }
            int j;
            if (PlayListBase.PlayListSongs.Count == 0)
            {
                PlayListBase.PlayListIndex = 0;
                PlayListBase.SendMsg(MyMsgType.Stop, "");
            }
            else if ((j = PlayListBase.PlayListSongs.IndexOf(curinfo)) >= 0)
            {
                PlayListBase.PlayListIndex = j + 1;
            }
            else
            {
                PlayListBase.SendMsg(MyMsgType.Stop, "");
            }

        }

        private void PlayListAlbumImageShowListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem item = Helper.UIhelper.FindPrent<ListBoxItem>((DependencyObject)e.OriginalSource);
            //myCollectionView.MoveCurrentTo(item);
            if (item == null) return;
            //WrapPlayListBox.ScrollIntoView(item.DataContext);
            //WrapPlayListBox.SelectedItem = item.DataContext;
            PlayListBase.PlayListIndex = WrapPlayListBox.SelectedIndex + 1;
            PlayListBase.PlayAndExceptionPass();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            PlayListBase.PlayingChanged -= OnPlayChanged;
            pageClosed = true;

        }

        private void Image_Loaded(object sender, RoutedEventArgs e)
        {
            threadQueue.Enqueue(((Image)sender).DataContext as SongInfoExpend);
            if (threadQueue.Count > ConfigPage.GlobalConfig.PlaylistItemNum) threadQueue.Dequeue();
            for(int i=0;i<tArr.Count();i++)
            if (tArr[i] == null||!tArr[i].IsAlive) {
                    tArr[i] = new Thread(() => LoadImage());
                    tArr[i].Start();//开了更多线程后会产生io问题，音乐会卡
            }
        }
        private void LoadImage() {
            SongInfoExpend data;
            lock (thelock)
            {
                while (threadQueue.Count == 0)
                {
                    if (pageClosed) return;
                    Thread.Sleep(200);
                }
                if (threadQueue.Count > 0) data = threadQueue.Dequeue();
                else {
                    return;
                };
            }
            Image IMAGE = null;
            ListBoxItem listBoxItem = null;
            double virtualPanelHeight=0, virtualPanelWidth=0;
            Dispatcher.Invoke(() =>
            {//测试元素是否在视口上，这种方式可能会有性能问题
                listBoxItem = ((ListBoxItem)WrapPlayListBox.ItemContainerGenerator.ContainerFromItem(data));
                IMAGE = Helper.UIhelper.FindChild<Image>(listBoxItem, null);
            });
            if (IMAGE != null)
            {
                var srd = new SalientRegionDetection();
                System.IO.Stream outImageStream = null;
                byte[] imagebuff=null;
                System.Drawing.Bitmap GenerImage=null;

                //var ss = DateTime.Now;
                var path= Core.Cach.CachPool.GetPath(data.SongInfo);
                var spath = Core.Cach.CachPool.GetSDRImagePath(path);
                var apath = Core.Cach.CachPool.GetAbPath(path);
                if (GlobalConfigClass._Config.UseSRDImageCach&&File.Exists(spath))
                {
                    try
                    {
                        GenerImage = new System.Drawing.Bitmap(spath);
                    }
                    catch(ArgumentException) { Debug.Print("参数无效异常"); }
                }
                if (GenerImage == null)
                {
                    virtualPanelHeight = listBoxItem.ActualHeight;//正常的话继承至父级VirtualizingWrapPanel自定义属性ChildHeight
                    virtualPanelWidth = listBoxItem.ActualWidth;

                    var rect =UIhelper.GetSystemDpi();
                    var childrenItemHeight =(int)Math.Round(rect.Height * virtualPanelHeight*0.01);
                    var childrenItemWidth = (int)Math.Round(rect.Width * virtualPanelWidth * 0.01);


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

                if (outImageStream == null)
                {
                    GenerImage.Dispose();
                    return;
                }
                outImageStream.Seek(0, SeekOrigin.Begin);
                Dispatcher.BeginInvoke((ParameterizedThreadStart)delegate(object outImageStreamx)
                {
                    BitmapImage bi = new BitmapImage();
                    bi.BeginInit();
                    bi.StreamSource = (System.IO.Stream)outImageStreamx;
                    bi.EndInit();
                    IMAGE.Source = bi;
                }, outImageStream);

                if (GlobalConfigClass._Config.UseAlbumImageCach&& imagebuff!=null&& !File.Exists(apath))
                    Core.Cach.CachPool.SetAb(data.SongInfo, imagebuff);
                if(GlobalConfigClass._Config.UseSRDImageCach && !File.Exists(spath))
                    Core.Cach.CachPool.SetSDRImageSource(data.SongInfo, GenerImage);
                GenerImage.Dispose();
            }
            LoadImage();//依赖于代码的优化，如果调试的时候溢出可以考虑循环
        }

        private void Image_Initialized(object sender, EventArgs e)
        {

        }

        private void PlayListGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
          var ev=  e.OriginalSource as ScrollViewer;
            if (ev != null)
            {
                MainWindow.CurMainWindowInstence.frame.Navigate(new Uri("/Playing.xaml", UriKind.Relative));
            }
        }

        private void Image_Unloaded(object sender, RoutedEventArgs e)
        {
        }

        private void WrapPlayListBox_Loaded(object sender, RoutedEventArgs e)
        {
            var scrollbar = UIhelper.FindChild<ScrollBar>(WrapPlayListBox, "");
            var scroller = UIhelper.FindChild<ScrollViewer>(WrapPlayListBox, "");

            Binding MaximumScrollBinding = new Binding() { Source = scrollbar, Path = new PropertyPath("Maximum"), Mode = BindingMode.TwoWay, Converter = new PublicConver.RatioConver(), ConverterParameter = SongListPageScrollBar.ActualHeight / scrollbar.ActualHeight };
            Binding ValueScrollBinding = new Binding() { Source = scrollbar, Path = new PropertyPath("Value"), Mode = BindingMode.OneWay,Converter=new PublicConver.RatioConver(),ConverterParameter= SongListPageScrollBar.ActualHeight/scrollbar.ActualHeight };
            Binding MinimumScrollBinding = new Binding() { Source = scrollbar, Path = new PropertyPath("Minimum"), Mode = BindingMode.TwoWay };
            Binding ViewPortScrollBinding = new Binding() { Source = SongListPageScrollBar, Path = new PropertyPath("ActualHeight"), Mode = BindingMode.OneWay };

            SongListPageScrollBar.SetBinding(ScrollBar.MaximumProperty, MaximumScrollBinding);
            SongListPageScrollBar.SetBinding(ScrollBar.ValueProperty, ValueScrollBinding);
            SongListPageScrollBar.SetBinding(ScrollBar.MinimumProperty, MinimumScrollBinding);
            SongListPageScrollBar.SetBinding(ScrollBar.ViewportSizeProperty, ViewPortScrollBinding);
            //scroller.ScrollChanged += (sender1,e1)=> { SongListPageScrollBar.Value = ((ScrollViewer)sender1).VerticalOffset; };
        }


        private void WrapPlayListBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            //e.Handled = true;
            //var value = -e.Delta / 12;
            //var scrollbar = UIhelper.FindChild<ScrollBar>(WrapPlayListBox, "");
            //var scroller = UIhelper.FindChild<ScrollViewer>(WrapPlayListBox, null);
            //scroller.ScrollToVerticalOffset(value + scroller.VerticalOffset);
            //if(scrollbar.Maximum>0)
            //    SongListPageScrollBar.Value = (scroller.VerticalOffset + 240 * (1 - scroller.VerticalOffset / scrollbar.Maximum)) / scrollbar.Maximum * SongListPageScrollBar.Maximum;

        }
        private void SongListPageScrollBar_LostMouseCapture(object sender, MouseEventArgs e)
        {
            var scrollbar = UIhelper.FindChild<ScrollBar>(WrapPlayListBox, "");
            Binding ValueScrollBinding = new Binding() { Source = scrollbar, Path = new PropertyPath("Value"), Mode = BindingMode.OneWay, Converter = new PublicConver.RatioConver(), ConverterParameter = SongListPageScrollBar.ActualHeight / scrollbar.ActualHeight };
            SongListPageScrollBar.SetBinding(ScrollBar.ValueProperty, ValueScrollBinding);

        }
        private void WrapPlayListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {

        }

        private void SongListPageScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            var scrollbar = UIhelper.FindChild<ScrollBar>(WrapPlayListBox, "");
            var scroller = UIhelper.FindChild<ScrollViewer>(WrapPlayListBox, null);
            scroller.ScrollToVerticalOffset(e.NewValue*  scrollbar.ActualHeight/ SongListPageScrollBar.ActualHeight);
        }

        private void WrapPlayListBox_Drop(object sender, DragEventArgs e)
        {
            SongInfoExpend deste = ((FrameworkElement)e.OriginalSource).DataContext as SongInfoExpend;
            SongInfoExpend source = (SongInfoExpend)e.Data.GetData(typeof(SongInfoExpend));
            if (deste == null) return;

            if (deste != source)
            {
                if (PlayListBase.PlayListSongs.Remove(source))
                {
                    int index = PlayListBase.PlayListSongs.IndexOf(deste) + 1;
                    PlayListBase.AddToPlayList(source, index);
                    if (source.SongInfo == PlayListBase.CurSongInfoEx.SongInfo)
                    {//如果拖动的是当前播放歌曲，则移动正在播放位置
                        PlayListBase.PlayListIndex = index+1;
                    }

                }
            }
            FrameworkElement elemt = ((FrameworkElement)e.OriginalSource);
            elemt = Helper.UIhelper.FindPrent<Grid>(elemt);
            Helper.UIhelper.FindChild<Line>(elemt, "").Visibility = Visibility.Collapsed;
            elemt.Margin = new Thickness(0, 0, 0, 0);
        }

        private void WrapPlayListBox_DragEnter(object sender, DragEventArgs e)
        {
            FrameworkElement elemt = ((FrameworkElement)e.OriginalSource);
            elemt = Helper.UIhelper.FindPrent<Grid>(elemt,2);
            if (elemt == null) return;
            elemt.Margin = new Thickness(-8, 0, 0, 0);
            Helper.UIhelper.FindChild<Line>(elemt, "").Visibility = Visibility.Visible;

        }

        private void WrapPlayListBox_DragLeave(object sender, DragEventArgs e)
        {
            FrameworkElement elemt = ((FrameworkElement)e.OriginalSource);
            elemt = Helper.UIhelper.FindPrent<Grid>(elemt);
            Helper.UIhelper.FindChild<Line>(elemt, "").Visibility = Visibility.Collapsed;
            elemt.Margin = new Thickness(0, 0, 0, 0);

        }

        public void Dispose()
        {
            pageClosed = true;
        }

        private void WrapPlayListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete) {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) {
                    PlayListBase.PlayListSongs.Clear();
                }
                //foreach (var item in WrapPlayListBox.SelectedItems) {
               else if (WrapPlayListBox.SelectedIndex!=-1)
                    PlayListBase.PlayListSongs.RemoveAt(WrapPlayListBox.SelectedIndex);
                //}
            }
        }


    }
}
