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
using AddIn.LrcSercher;
using System.IO;
using AddIn.Lrc;
using System.Threading;
using PlayProjectGame.Data;

namespace PlayProjectGame.LrcSercher
{
    /// <summary>
    /// LrcSeacherPage.xaml 的交互逻辑
    /// </summary>
    public partial class LrcSeacherWindow :Window
    {
        internal static string LrcTempEditorText
        {
            set;
            get;
        }
        static public Thread downThread;
        SongInfo Cursinfo;
        public LrcSeacherWindow(SongInfo sinfo)
        {
            InitializeComponent();
            Cursinfo = sinfo;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SongInfo sInfo = new SongInfo()
            {
                LrcRefPath = Cursinfo.LrcRefPath,
                SongId = Cursinfo.SongId,
                SongArtist = Cursinfo.SongArtist,
                SongAlbum = Cursinfo.SongAlbum,
                OsuPath = Cursinfo.OsuPath,
                SongLengh = Cursinfo.SongLengh,
                SongName = Cursinfo.SongName,
                SongPath = Cursinfo.SongPath,
                SongType = Cursinfo.SongType,
            };
            grid.DataContext = sInfo;
        }

        private void SercherLocalLrcButton_Click(object sender, RoutedEventArgs e)
        {
            ILyricsSercher sercher = new LocalLyricsList(GlobalConfigClass._Config.LrcDir);
            sercher.GetLrcInfoSercherList(ArtistTextBox.Text, MusicNameTextBox.Text, GlobalConfigClass._Config.LrcDir);
            ListLrcSelect.ItemsSource = sercher.SercherResultList;
            LrcTempEditorText = null;
        }

        private void SercherNetcoLrcButton_Click(object sender, RoutedEventArgs e)
        {
            ILyricsSercher sercher = new NetCloudLyricsList(GlobalConfigClass._Config.DelayTimeWebLoad);
            if(Cursinfo.SongType==2)
             sercher.GetLrcInfoSercherList(ArtistTextBox.Text, MusicNameTextBox.Text, GlobalConfigClass._Config.LrcDir,Cursinfo.SongId);
            //test Sercher Lrc No Id In CoudMusic :// sercher.GetLrcInfoSercherList(ArtistTextBox.Text, MusicNameTextBox.Text, GlobalConfigClass._Config.LrcDir);
           else
                sercher.GetLrcInfoSercherList(ArtistTextBox.Text, MusicNameTextBox.Text, GlobalConfigClass._Config.LrcDir);

            ListLrcSelect.ItemsSource = sercher.SercherResultList;
            LrcTempEditorText = null;
        }

        private void SercherQqjtLrcButton_Click(object sender, RoutedEventArgs e)
        {
            ILyricsSercher sercher = new QianQianLyricsList();
            sercher.GetLrcInfoSercherList(ArtistTextBox.Text, MusicNameTextBox.Text, GlobalConfigClass._Config.LrcDir);
            ListLrcSelect.ItemsSource = sercher.SercherResultList;
            LrcTempEditorText = null;

        }

        private void ListLrcSelect_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //获取数据项
            ILrcSercherInfoItem item = ListLrcSelect.SelectedItem as ILrcSercherInfoItem;
            try
            {
                if (item == null) item = Helper.UIhelper.FindPrent<ListViewItem>((DependencyObject)e.OriginalSource).DataContext as ILrcSercherInfoItem;
            }
            catch (Exception)
            {
                return;
            }
            if (item == null) return;
            //组合将要保存到LRC目录的路径
            string fileName = ArtistTextBox.Text + " - " + MusicNameTextBox.Text + ".lrc";
            fileName = Helper.OtherHelper.ReplaceValidFileName(fileName,' ');
            string path = GlobalConfigClass._Config.LrcDir + "//" + fileName;
            //处理同名
            if (File.Exists(path) && MessageBoxResult.No == MessageBox.Show("文件已经存在，是否覆盖", "确认覆盖LRC", MessageBoxButton.YesNo))
                return;
            //处理未下载(未编辑)
            if (LrcTempEditorText == null|| LrcTempEditorText.Count() < 7) return;
            //文件写入
            File.WriteAllText(path, LrcTempEditorText);
            item.FilePath = path;
            Playing.ChangeLrcAndPlayingPage(path);
        }

        private void ListLrcSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Expander_MouseWheel(object sender, MouseWheelEventArgs e)
        {

        }

        private void ex_Expanded(object sender, RoutedEventArgs e)
        {
            Expander expend = (Expander)sender;
            if (expend.Content != null) return;
            ILrcSercherInfoItem lrcSercherItem = expend.DataContext as ILrcSercherInfoItem;
            byte[] buff;
            string LrcTempDownPath="lrc_temp";
            if (File.Exists(lrcSercherItem.FilePath))
            {//在本地存在
                using (FileStream FS = File.OpenRead(lrcSercherItem.FilePath))
                {
                    buff = new byte[(int)FS.Length];
                    FS.Read(buff, 0, (int)FS.Length);
                    LrcTempEditorText = BeforeEncodingClass.GetText(buff);
                    LrcTempEditorText = LrcTempEditorText.Replace("\\n", "\r\n");
                    TextBox textBox = new TextBox() { Text = LrcTempEditorText };
                    textBox.TextChanged += TextBox_TextChanged;
                    expend.Content = textBox;
                }
            }
            else if (lrcSercherItem.LrcUri != null)
            {//在网络存在
                downThread = new Thread((ThreadStart)delegate
                 {
                     Dispatcher.Invoke((ThreadStart)delegate
                     {
                         expend.Content = new TextBox() { Text = "正在下载中。。。" };
                     });
                     lrcSercherItem.DownloadLrc(LrcTempDownPath);
                     LrcTempEditorText = File.ReadAllText(LrcTempDownPath);
                     Dispatcher.Invoke((ThreadStart)delegate
                     {
                         TextBox textBox = new TextBox() { Text = LrcTempEditorText };
                         textBox.TextChanged += TextBox_TextChanged;
                         expend.Content = textBox;
                     });
                 });
                downThread.Start();
            }
            else
                LrcTempEditorText = "";

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LrcTempEditorText = ((TextBox)sender).Text;
        }

        private void SercherLocalLrcButton_MouseEnter(object sender, MouseEventArgs e)
        {
            SercherExplainTextBlock.Text = "使用本地的LRC文件夹搜索，将被用于关联和自动搜索";
        }

        private void SercherNetcoLrcButton_MouseEnter(object sender, MouseEventArgs e)
        {
            SercherExplainTextBlock.Text = "搜索云音乐的LRC库，如果是音乐来自云音乐将默认使用该歌曲的关联ID";
        }

        private void SercherQqjtLrcButton_MouseEnter(object sender, MouseEventArgs e)
        {
            SercherExplainTextBlock.Text = "搜索千千静听的LRC库";
        }

        private void SercherLocalLrcButton_MouseLeave(object sender, MouseEventArgs e)
        {
            SercherExplainTextBlock.Text = "使用“艺术家-歌曲”进行搜索和保存";
        }

        private void SercherNetcoLrcButton_MouseLeave(object sender, MouseEventArgs e)
        {
            SercherExplainTextBlock.Text = "使用“艺术家-歌曲”进行搜索和保存";
        }

        private void SercherQqjtLrcButton_MouseLeave(object sender, MouseEventArgs e)
        {
            SercherExplainTextBlock.Text = "使用“艺术家-歌曲”进行搜索和保存";
        }
    }
}
