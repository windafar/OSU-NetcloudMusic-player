using AddIn.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using PlayProjectGame.PlayList;
using Communication;
using PlayProjectGame.Data;

namespace PlayProjectGame
{
    /// <summary>
    /// PlayList.xaml 的交互逻辑
    /// </summary>
    public partial class SimplePlayList : Page
    {
        static public SimplePlayList CurInstance;
        public ListBoxItem BeforPlayItem;
        Timer t1;

        void OnPlayingChanged(SongInfoExpend CurSongInfoEx) {
            Dispatcher.BeginInvoke((ThreadStart)delegate
            {
                UserSongList.SelectedItem = CurSongInfoEx;//选择为当前播放项
                ListBoxItem item = (ListBoxItem)UserSongList.ItemContainerGenerator.ContainerFromItem(CurSongInfoEx);
                if (item != null)//使用虚拟化的锅，毕竟只是区区文字，还是不用虚拟化了吧
                {
                    item.ContentTemplate = Resources["PlayListItemSelectedDataTemplate"] as DataTemplate;
                    if (BeforPlayItem != null)
                        BeforPlayItem.ContentTemplate = Resources["PlayListItemDataTemplate"] as DataTemplate;
                    BeforPlayItem = item;
                }
            });

        }

        public SimplePlayList()
        {
            InitializeComponent();
            CurInstance = this;
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            PlayListBase.PlayingChanged += OnPlayingChanged;
            UserSongList.ItemsSource = PlayListBase.PlayListSongs;
            PlayListAlbumImageShowListBox.ItemsSource = PlayListBase.PlayListSongs;
            if (PlayListBase.PlayListTotal <= 0) return;
            UserSongList.SelectedItem = PlayListBase.PlayListSongs[PlayListBase.PlayListIndex - 1];
            UserSongList.ScrollIntoView(UserSongList.SelectedItem);
            if (t1 == null)
                t1 = new Timer(delegate
                 {//用于等待项的生成
                  Dispatcher.Invoke((ThreadStart)delegate
                     {
                         SongInfoExpend sinfoEx = PlayListBase.PlayListSongs[PlayListBase.PlayListIndex - 1];
                         ListBoxItem item = (ListBoxItem)UserSongList.ItemContainerGenerator.ContainerFromItem(sinfoEx);
                         if (item == null) return;
                         item.ContentTemplate = CurInstance.Resources["PlayListItemSelectedDataTemplate"] as DataTemplate;
                         if (CurInstance.BeforPlayItem != null)
                             CurInstance.BeforPlayItem.ContentTemplate = CurInstance.Resources["PlayListItemDataTemplate"] as DataTemplate;
                         CurInstance.BeforPlayItem = item;
                     });

                 }, null, 500, Timeout.Infinite);
            else t1.Change(500, Timeout.Infinite);

            PlayListAlbumImageShowListBox.SelectedItem = PlayListBase.PlayListSongs[PlayListBase.PlayListIndex - 1];
            PlayListAlbumImageShowListBox.ScrollViewToCur(PlayListAlbumImageShowListBox.SelectedItem);
        //    myCollectionView = (CollectionView)CollectionViewSource.GetDefaultView(PlayListAlbumImageShowListBox.ItemsSource);
        //    myCollectionView.MoveCurrentToFirst();

        }
        private void UserSongList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
           ListBoxItem item= Helper.UIhelper.FindPrent<ListBoxItem>((DependencyObject)e.OriginalSource);
            if (item == null) return;
            PlayListAlbumImageShowListBox.ImageListBox.ScrollIntoView(item.DataContext);
            PlayListAlbumImageShowListBox.ImageListBox.SelectedItem = item.DataContext;
            PlayListBase.PlayListIndex = UserSongList.SelectedIndex+1;
            PlayListBase.PlayAndExceptionPass();

        }

        private void MenuItem_UserSongList_Click(object sender, RoutedEventArgs e)
        {
            IList LIST = UserSongList.SelectedItems;
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
                PlayListBase.SendMsg(MyMsgType.audio_Stop, "");
            }
            else if ((j = PlayListBase.PlayListSongs.IndexOf(curinfo)) >= 0)
            {
                PlayListBase.PlayListIndex = j + 1;
            }
            else { PlayListBase.SendMsg(MyMsgType.audio_Stop, ""); }

        }

        private void PlayListAlbumImageShowListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem item = Helper.UIhelper.FindPrent<ListBoxItem>((DependencyObject)e.OriginalSource);
            //myCollectionView.MoveCurrentTo(item);
            if (item == null) return;
            UserSongList.ScrollIntoView(item.DataContext);
            UserSongList.SelectedItem = item.DataContext;
            PlayListBase.PlayListIndex = UserSongList.SelectedIndex + 1;
            PlayListBase.PlayAndExceptionPass();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            CurInstance = null;
            PlayListBase.PlayingChanged -= OnPlayingChanged;

        }
    }
}
