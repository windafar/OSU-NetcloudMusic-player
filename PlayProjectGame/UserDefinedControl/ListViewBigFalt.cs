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

namespace PlayProjectGame.UserDefinedControl
{
    /// <summary>
    /// ListViewBigFalt.xaml 的交互逻辑
    /// </summary>
    public partial class ListViewBigFalt : ListView
    {
        public static readonly DependencyProperty GridViewHearderBackgroudProperty = DependencyProperty.Register("GridViewHearderBackgroud", typeof(Brush), typeof(ListViewBigFalt), new PropertyMetadata(Brushes.White));
        public Brush GridViewHearderBackgroud
        {
            get => (Brush)(GetValue(GridViewHearderBackgroudProperty));
            set
            {
                SetValue(GridViewHearderBackgroudProperty, value);
            }
        }

        public ListViewBigFalt()
        {

        }

    }
}
