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
    /// CreateSongListWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CreateSongListWindow : Window
    {
        public CreateSongListWindow()
        {
            InitializeComponent();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            
            //Data.CouldMusicLocalDataGeter.AppendManyNewSongListToXML()//这个函数又不是不能用，我不想再搞了就这个用着吧
        }
    }
}
