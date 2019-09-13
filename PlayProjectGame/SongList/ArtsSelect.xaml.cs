using PlayProjectGame.Data;
using PlayProjectGame.Helper;
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

namespace PlayProjectGame
{
    /// <summary>
    /// ArtsSelect.xaml 的交互逻辑
    /// </summary>
    public partial class ArtsSelect : Window
    {
        public ArtsSelect(string[] arts)
        {
            InitializeComponent();
            Arts = arts;
        }

        public string[] Arts { get; }

        private void ArtsSelectListBox_Loaded(object sender, RoutedEventArgs e)
        {
            ArtsSelectListBox.ItemsSource = Arts.Select(x => new { ArtName = x }); ;
        }

        private void ArtTextBlockMouseDown(object sender, MouseButtonEventArgs e)
        {
            string art = ((dynamic)(((TextBlock)sender).DataContext)).ArtName;
            var NavigationService = MainWindow.CurMainWindowInstence.frame;
            var netcloud = MainWindow.CurMainWindowInstence.NetCloudData.AsParallel().SelectMany(x => x.Songs).Where(x=>art.IndexExistOfAny(x.SongInfo.GetSongArtists)).ToList();
            var osu = MainWindow.CurMainWindowInstence.OsuData.AsParallel().SelectMany(x => x.Songs).Where(x => art.IndexExistOfAny(x.SongInfo.GetSongArtists)).ToList();
            netcloud.AddRange(osu);
            netcloud = netcloud.GroupBy(x => x.SongInfo.SongName).Select(x => x.First()).ToList();
            PlayListData pld = null;
            if (NavigationService.Content != null)
            {
                SongList Pr = NavigationService.Content as SongList;
                if (Pr != null)
                {
                    NavigationService.AddBackEntry(new PlayListJournalEntry(Pr.PLD, MainWindow.CurMainWindowInstence.ReplySongListPage,Pr.SongListListView.SelectedItem));
                }
            }
            pld = new PlayListData() { PlatListName = "艺术家：" + art, PlayListType = 5, Songs = netcloud };
            SongList.SetPlayListData(pld);
            Uri NextUri = new Uri("SongList/SongList.xaml", UriKind.Relative);
            if (NavigationService.Source == null || NavigationService.Source.OriginalString != "SongList/SongList.xaml")
                NavigationService.Navigate(NextUri);
            else NavigationService.Refresh();
            this.Close();

        }
    }
}
