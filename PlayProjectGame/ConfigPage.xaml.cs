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
                                list[0].DownloadLrc(path);//覆盖或者创建                 
                            }
                        }
                    }
                }
            }
        }

        private void ReloadCloudMusicDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.CurMainWindowInstence != null) {
                var list = CouldMusicLocalDataGeter.GetSongList(true);
                var list1=CouldMusicLocalDataGeter.AppendNewSongListToXML(list).Last();
                MainWindow.CurMainWindowInstence.NetClouldMusicaListBox.ItemsSource = list1.Pids;
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

        private void MatchSongListByFileNameClick(object sender, RoutedEventArgs e)
        {
            var filesys = MainWindow.CurMainWindowInstence.filesyscath;
            int count = 0;
            if (filesys.Count == 0)//程序运行阶段只能初始化一次
            {
                ((Button)sender).IsEnabled = false;
                MessageBox.Show("开始初始化文件系统");
                //Task.Run(() =>
                //{
                var DriveNames = ConfigPage.GlobalConfig.MatchSongFromDivce.Split(new char[1] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var driveInfo in DriveInfo.GetDrives())
                {
                    if (driveInfo.IsReady)
                        filesys.AddRange(FileSerch.FileSercher.EnumerateFiles(driveInfo));
                }
                MainWindow.CurMainWindowInstence.filesyscath = filesys.Where(x =>
                {
                    int index = x.LastIndexOf(".");
                    string Extension = x.Substring(index + 1, x.Length - index - 1);
                    return Extension == "mp3" ||
                    Extension == "flac" ||
                    //  Extension == "wav" ||
                    Extension == "ncm";
                }).OrderBy(x => new FileInfo(x).Name).ToList();
                Dispatcher.Invoke(() => {
                    MessageBox.Show("文件系统初始化完成,请点击开始匹配");
                    ((Button)sender).IsEnabled = true;
                });
                //});
            }
            else if (((Button)sender).IsEnabled)
            {
                Parallel.ForEach(MainWindow.CurMainWindowInstence.NetCloudData, p =>
                {
                    foreach(var s in p.Songs)
                        if (!string.IsNullOrEmpty(s.SongInfo.SongPath) && !File.Exists(s.SongInfo.SongPath))
                        {
                            int index = MainWindow.CurMainWindowInstence.filesyscath.BinarySearch(s.SongInfo.SongPath, Helper.Comparer.SampleFileNameComparer); ;
                            if (index > -1)
                            {
                                s.SongInfo.SongPath = MainWindow.CurMainWindowInstence.filesyscath[index];
                                count++;
                            }
                        }
                });
                MessageBox.Show("匹配完成,共计" + count);
                CouldMusicLocalDataGeter.backup();
            }
        }
    }
}
