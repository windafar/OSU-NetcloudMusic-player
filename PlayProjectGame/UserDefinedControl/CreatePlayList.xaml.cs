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
using System.Windows.Shapes;

namespace PlayProjectGame.UserDefinedControl
{
    /// <summary>
    /// CreatePlayList.xaml 的交互逻辑
    /// </summary>
    public partial class CreatePlayList : Window
    {
        public IEnumerable<SongInfoExpend> songInfoExpends;
        public CreatePlayList()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            PlayListData pld = ((FrameworkElement)sender).DataContext as PlayListData;
            LocalMusicGeter localMusicGeter = new LocalMusicGeter(songInfoExpends, pld.UersName,pld.PlayListInfo,pld.PlatListName);
         //   MainWindow.CurMainWindowInstence.LocalListBox.ItemsSource= localMusicGeter.GetLocalUserData().Pids;
        }

        private void SongListFrom_Loaded(object sender, RoutedEventArgs e)
        {
            PlayListData playListData = new PlayListData() { UserId = Guid.NewGuid().ToString(), PlayListType = 1 };
            SongListFrom.DataContext = playListData;


        }
    }
}
