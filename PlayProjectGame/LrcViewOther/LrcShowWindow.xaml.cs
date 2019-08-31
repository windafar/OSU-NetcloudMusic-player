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
using System.Windows.Shapes;
using PlayProjectGame;
using System.Threading;
using AddIn.Lrc;
using PlayProjectGame.PlayList;
using PlayProjectGame.Data;

namespace PlayProjectGame.LrcViewOther
{
    /// <summary>
    /// LrcShowWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LrcShowWindow : Window
    {
        /// <summary>
        /// 该类的当前实例
        /// </summary>
        static public LrcShowWindow CurrentInstence { get; private set; }

        void OnPlayingChanged(SongInfoExpend songInfoExpend )
        {
                Playing.LoadLoaclLrc(null, LrcViewOther.LrcShowWindow.CurrentInstence.Dispatcher, LrcViewOther.LrcShowWindow.CurrentInstence.LoadLrcSuccessToUI, LrcViewOther.LrcShowWindow.CurrentInstence.LoadLrcFailToUI);
                Playing.InitLrcShow(LrcViewOther.LrcShowWindow.CurrentInstence.Dispatcher);

            }

        public LrcShowWindow()
        {
            InitializeComponent();
            this.DataContext = ConfigPage.GlobalConfig;
            CurrentInstence = this;
            PlayListBase.PlayingChanged += OnPlayingChanged;
            //LRC滚动
            UpdateDesktopLrcScrollerTimer = new System.Windows.Threading.DispatcherTimer();
            UpdateDesktopLrcScrollerTimer.Interval = TimeSpan.FromSeconds(0.1);
            UpdateDesktopLrcScrollerTimer.Tick += UpdateDesktopLrcScrollerTimer_Tick;
        }

        private void UpdateDesktopLrcScrollerTimer_Tick(object sender, EventArgs e)
        {//更新LRC滚动
            if (_curLrc == null || CurLrcTextBox == null|| _curLrc.Next == null) return;
            var __curItem = _curLrc;
            if (Playing.IsShowOlddRowLrc)
            {
                if (_curLrc.Next != null)
                    __curItem = _curLrc.Next;
                if (__curItem.Next == null) return;
            }
            //UpdateDesktopLrcScroller将运行在新的定时器里面，所以注意CurLrcItem是不是下一项，预计是下一项，最糟糕的是第二次是下一项
            //更改此计时器间隔测试线程切换的可能
            double ItemLrcMillsecTotal = __curItem.Next.Value.Time.TotalMilliseconds - __curItem.Value.Time.TotalMilliseconds;//当前项LRC的存在时间
            double ItemLrcMillsecCur = (PlayListBase.player.Position - __curItem.Value.Time).TotalMilliseconds;//一项LRC当前已经走过的时间
           // double ItemLrcMillsecCur = TimeSpan.FromTicks(PlayListBase.CurrentPlayTime - __curItem.Value.Time.Ticks).TotalMilliseconds;//一项LRC当前已经走过的时间
            double ItemLrcLenIndex = (ItemLrcMillsecCur / ItemLrcMillsecTotal) * (CurLrcTextBox.ActualWidth - DesktopLrcWindow.ActualWidth+30);//这30是其他控件的呈现高度，最后多出来的应该为30/4-1
            //ItemLrcMillsecCur应是逐渐变大
            LrcScroller.ScrollToHorizontalOffset(ItemLrcLenIndex);
        }

        ScrollViewer LrcScroller;

        private void LrcShowWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LrcScroller = Helper.UIhelper.FindChild<ScrollViewer>((DependencyObject)sender, null);

            //if(Playing.lrcshow==null)
            Playing.LoadLoaclLrc(null, Dispatcher,
                () =>
                {
                    LoadLrcSuccessToUI();
                },
                () =>
                {
                    LoadLrcFailToUI();
                });
            Playing.UpdateLrcUIDeleLink += UpdateDesktopLrc;
                Playing.InitLrcShow(Dispatcher);
            LrcShowWindowListBox.DataContext = GlobalConfigClass._Config;
        }

        public void LoadLrcFailToUI()
        {
            LrcShowWindowListBox.ItemsSource = null;
            LrcShowWindowListBox.Items.Clear();
            LrcShowWindowListBox.Items.Add(new ListBoxItem() { Content = Playing.CurrentSongInfo.SongInfo.SongName + " - " + Playing.CurrentSongInfo.SongInfo.SongArtist });
        }

        public void LoadLrcSuccessToUI()
        {
            LrcShowWindowListBox.ItemsSource = null;
            LrcShowWindowListBox.Items.Clear();
            LrcShowWindowListBox.ItemsSource = Playing.lrcshow.Items;
        }
        TextBlock CurLrcTextBox;
        
        private System.Windows.Threading.DispatcherTimer UpdateDesktopLrcScrollerTimer;
        /// <summary>
        /// 因Playing的CurLrcItem属性会变化，所以使用该字段记录当前视图Lrc项
        /// </summary>
        private LinkedListNode<LrcItem> _curLrc;

        private void UpdateDesktopLrc()
        {//每句LRC的更新
            if (Playing.CurLrcItem != null)
            {
                UpdateDesktopLrcScrollerTimer.Stop();

                if (Playing.CurLrcItem == null) return;
                else _curLrc = Playing.CurLrcItem;

                if (Playing.IsShowOlddRowLrc)
                {
                    if(Playing.CurLrcItem.Previous == null) return;
                    else _curLrc = Playing.CurLrcItem.Previous;
                }

                LrcShowWindowListBox.ScrollIntoView(_curLrc.Value);
                ListBoxItem lstitem = (ListBoxItem)LrcShowWindowListBox.ItemContainerGenerator.ContainerFromItem(_curLrc.Value);
                CurLrcTextBox = Helper.UIhelper.FindChild<TextBlock>(lstitem, null);
                LrcScroller.ScrollToHorizontalOffset(0);
                UpdateDesktopLrcScrollerTimer.Start();
            }
        }

        private void DesktopLrcWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            CurrentInstence = null;
            Playing.UpdateLrcUIDeleLink -= UpdateDesktopLrc;
            PlayListBase.PlayingChanged -= OnPlayingChanged;

        }

        private void LrcShowWindowListBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
            LrcShowWindowRect.Visibility = Visibility.Visible;
        }

        private void LrcShowWindowListBox_MouseLeave(object sender, MouseEventArgs e)
        {
            LrcShowWindowListBox.SelectedIndex = -1;
          //  LrcShowWindowRect.Visibility = Visibility.Hidden;
        }

        Point point;
        private void Rectangle_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                point = e.GetPosition((UIElement)sender);
                // if (point.X < 0 || point.Y < 0) return;
                DesktopLrcWindow.Width = DesktopLrcWindow.Width + point.X < 24 ? DesktopLrcWindow.Width : DesktopLrcWindow.Width + point.X;
                DesktopLrcWindow.Height = DesktopLrcWindow.Height + point.Y <12 ? DesktopLrcWindow.Height : DesktopLrcWindow.Height + point.Y;
                ((UIElement)sender).CaptureMouse();
            }
            else
                if(((UIElement)sender).IsMouseCaptured)
            ((UIElement)sender).ReleaseMouseCapture();

        }

        private void Rectangle_MouseLeave(object sender, MouseEventArgs e)
        {
            ((UIElement)sender).ReleaseMouseCapture();
        }
    }
}
