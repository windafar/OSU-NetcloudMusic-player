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

namespace PlayProjectGame.Note
{
    /// <summary>
    /// SingleNote.xaml 的交互逻辑
    /// </summary>
    public partial class SingleNote : UserControl
    {
        NoteContentViewData viewData;
        
        public SingleNote(NoteContentViewData viewData)
        {
            this.viewData = viewData;
            InitializeComponent();
        }

        private void SingleNoteWidget_Loaded(object sender, RoutedEventArgs e)
        {
            SingleNoteWidget.DataContext = viewData;
        }

        private void notebutton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SingleNoteWidget_Click(object sender, RoutedEventArgs e)
        {
           TextBox textBox= Helper.UIhelper.FindChild<TextBox>((FrameworkElement)sender, "");
            textBox.Visibility = Visibility.Visible;
            Helper.UIhelper.FindChild<Button>((FrameworkElement)sender, "").Visibility = Visibility.Hidden;
        }
    }
}
