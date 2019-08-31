using PlayProjectGame.Data;
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

namespace PlayProjectGame
{
    /// <summary>
    /// SearchPage.xaml 的交互逻辑
    /// </summary>
    public partial class SearchPage : Page
    {
        public static SearchPage CurPageInstance { get; private set; }
        public MainSearchResult MSR;
        public static MainSearchResult SetMSRBeforStart;
        public SearchPage()
        {
            InitializeComponent();
            if (SetMSRBeforStart != null)
                this.MSR = SetMSRBeforStart;
            else
                throw new Exception();
        }

        private void SearchList_Loaded(object sender, RoutedEventArgs e)
        {
            SearchList.ItemsSource = MSR.songs;
        }

        private void SearchPageSongListLinkClick(object sender, RoutedEventArgs e)
        {
            SongInfoExpend cursong = ((FrameworkElement)(sender)).DataContext as SongInfoExpend;

            PlayListData PlayListData = null;
            if (cursong.Source.IndexOf("osuname_") == 0)
            {
                var f = cursong.Source.Remove(0, 8);
                PlayListData = MainWindow.CurMainWindowInstence.OsuData.FirstOrDefault(x => x.PlatListName == f);
            }
            if (cursong.Source.IndexOf("netid_") == 0)
            {
                var f = cursong.Source.Remove(0, 6);
                PlayListData = MainWindow.CurMainWindowInstence.NetCloudData.FirstOrDefault(x => {
                    if (x.PlayListId == f)
                    {
                        return true;
                    }
                    else return false;
                });
            }
            if (PlayListData == null) return;
            SongList.SetPlayListData(PlayListData);
            SongList.StartCmd.Enqueue("select_"+ cursong.SongInfo.SongName);
            if (MainWindow.CurMainWindowInstence.frame.Source==null||MainWindow.CurMainWindowInstence.frame.Source.OriginalString != "SongList.xml")
                MainWindow.CurMainWindowInstence.frame.Navigate(new Uri("SongList.xaml", UriKind.Relative));
            else MainWindow.CurMainWindowInstence.frame.Refresh();
        }

        private void SearchList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SongInfoExpend se = ((FrameworkElement)(e.OriginalSource)).DataContext as SongInfoExpend;
            PlayList.PlayListBase.AddToPlayList(se, true);
            PlayList.PlayListBase.PlayAndExceptionPass();
        }
    }
}
