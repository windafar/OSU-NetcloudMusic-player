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

namespace WindowDemo
{
    /// <summary>
    /// TestWindow.xaml 的交互逻辑
    /// </summary>
    public partial class TestWindow 
    {
        public TestWindow()
        {
            InitializeComponent();
            
        }
        public static readonly DependencyProperty CurrentThemeBrushProperty = DependencyProperty.Register("CurrentThemeBrush", typeof(Brush), typeof(MainWindow), new PropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 230, 230, 230))));
        public static readonly DependencyProperty BottomControlVisibilityProperty = DependencyProperty.Register("BottomControlVisibility", typeof(Visibility), typeof(MainWindow), new PropertyMetadata(Visibility.Collapsed));

        public Brush CurrentThemeBrush
        {
            get => (Brush)(GetValue(CurrentThemeBrushProperty));
            set
            {
                SetValue(CurrentThemeBrushProperty, value);

            }
        }
        public Visibility BottomControlVisibility
        {
            get => (Visibility)(GetValue(BottomControlVisibilityProperty));
            set
            {
                SetValue(BottomControlVisibilityProperty, value);

            }
        }


        private void OnTest(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Test");
        }

       
    }
}
