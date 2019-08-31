using AddIn.LrcSercher;
using AddIn.Audio;
using System;
using System.Collections.Generic;
using System.IO;
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
using PlayProjectGame.Data;

namespace PlayProjectGame
{
    /// <summary>
    /// ConfigPage.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigPage : Page
    {
       public static GlobalConfigClass GlobalConfig;
        public ConfigPage()
        {
            InitializeComponent();

        }

        private void ConfigStackPanel_Loaded(object sender, RoutedEventArgs e)
        {
            ConfigStackPanel.DataContext = GlobalConfig;
            if (GlobalConfig.AddIndex == 0)
                addToLastRadioButton.IsChecked = true;
            else
                addToNextRadioButton.IsChecked = true;
        }

        private void addToLastRadioButton_Click(object sender, RoutedEventArgs e)
        {
            if (addToLastRadioButton.IsChecked==true) GlobalConfig.AddIndex = 0;

        }

        private void addToNextRadioButton_Click(object sender, RoutedEventArgs e)
        {
            if (addToNextRadioButton.IsChecked == true) GlobalConfig.AddIndex = 1;

        }

        private void DownAllLrcButton_Click(object sender, RoutedEventArgs e)
        {
            NetCloudLyricsList nclrc = new NetCloudLyricsList(2.0);
            foreach (var user in CouldMusicLocalDataGeter.GetSongList()) {
                foreach (var playlist in user.Pids) {
                    foreach (var song in playlist.Songs){
                        if (song.SongInfo.SongId != null){
                           var list= nclrc.GetLrcInfoSercherList(null, null, null, song.SongInfo.SongId);
                            if (list.Count > 0) {
                                //组合将要保存到LRC目录的路径
                                string fileName = song.SongInfo.SongArtist + " - " + song.SongInfo.SongName + ".lrc";
                                fileName = Helper.OtherHelper.ReplaceValidFileName(fileName, ' ');
                                string path = GlobalConfigClass._Config.LrcDir + "//" + fileName;
                                //处理同名
                                if (File.Exists(path))
                                    continue;
                                list[0].DownloadLrc(path);                 
                            }
                        }
                    }
                }
            }
        }

        private void ReloadCloudMusicDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(GlobalConfigClass.XML_CLOUDMUSIC_SAVEPATH))
                File.Delete(GlobalConfigClass.XML_CLOUDMUSIC_SAVEPATH);
            if (MainWindow.CurMainWindowInstence != null) {
                MainWindow.CurMainWindowInstence.LoadNetCloudMusic();
            }
        }

        private void WaveOut_Click(object sender, RoutedEventArgs e)
        {
            GlobalConfig.OutMode1 = OutDevice.SelectedItem.ToString();
        }

        private void WasapiOut_Click(object sender, RoutedEventArgs e)
        {

        }

        private void OutDevice_Loaded(object sender, RoutedEventArgs e)
        {
            List<string> list = new List<string>();
            list.AddRange(PlayList.PlayListBase.player.GetDirectSoundOutDeviceName());
            list.AddRange(PlayList.PlayListBase.player.GetWaveOutDeviceName());
            list.AddRange(PlayList.PlayListBase.player.GetWasApiDeviceName());
            list.AddRange(PlayList.PlayListBase.player.GetAsioOutDeviceName());

            OutDevice.ItemsSource = list;
            var select = list.FirstOrDefault(x => x.IndexOf(GlobalConfig.DeviceName) != -1);
            if (string.IsNullOrWhiteSpace(select))
                select = list.FirstOrDefault();
            OutDevice.SelectedValue = select;
        }

        private void ReloadOsuDataButton_Click(object sender, RoutedEventArgs e)
        {
            //if (File.Exists(GlobalConfigClass.XML_OSU_SAVEPATH))
            //    File.Delete(GlobalConfigClass.XML_OSU_SAVEPATH);
            if (MainWindow.CurMainWindowInstence != null)
            {
                MainWindow.CurMainWindowInstence.LoadOSU(true);
            }
        }
    }
}
