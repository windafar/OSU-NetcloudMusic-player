using System;
using System.Collections;
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
using PlayProjectGame.Helper;

namespace PlayProjectGame.UserDefinedControl
{
    /// <summary>
    /// Hor_FisheyeEff_ListBox.xaml 的交互逻辑
    /// </summary>
    public partial class Hor_FisheyeEff_ListBox : UserControl
    {
        public Hor_FisheyeEff_ListBox()
        {
            InitializeComponent();
        }
        

        public IEnumerable ItemsSource
        {
            get
            {
                return ImageListBox.ItemsSource;
            }
            set
            {
                ImageListBox.ItemsSource = value;
            }
        }
        public object SelectedItem
        {
            get
            {
                return ImageListBox.SelectedItem;
            }
            set
            {
                ImageListBox.SelectedItem = value;
            }
        }
         public void ScrollViewToCur(object obj)
            {
            ImageListBox.ScrollIntoView(obj);
            }
        public double itemImageWidth=-1;
        /// <summary>
        /// 递减系数，影响最小图片宽度，默认0.2
        /// </summary>
        public double DecRatio = 0.2;

        private void PlayListAlbumImageShowListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ////处理选择的项，同时给后面继续处理项提供条件
            ////获取选中项的项控件
            //DependencyObject SlectedItem = PlayListAlbumImageShowListBox.ItemContainerGenerator.ContainerFromItem(PlayListAlbumImageShowListBox.SelectedItem);
            ////获取项控件下的Image控件
            //Image SelectItemImageControl = UIhelper.FindChild<Image>(SlectedItem,null);
            ////获取父控件的高度
            //double ItemHeigh = PlayListAlbumImageShowListBox.Height;
            ////设置选择Image的高度为父控件高度
            //SelectItemImageControl.Width = ItemHeigh;
            ////获取选择项宽度备用（不行就自己计算宽度）
            //double ItemWidth = SelectItemImageControl.Width;

            //int selectedIndex= PlayListAlbumImageShowListBox.Items.IndexOf(PlayListAlbumImageShowListBox.SelectedItem);
            //int totalItems = PlayListAlbumImageShowListBox.Items.Count;
            //int setUpdateItemCount = (int)(PlayListAlbumImageShowListBox.ActualWidth / SelectItemImageControl.ActualWidth );
            ////后续项处理
        
        }

        private void Image_Loaded(object sender, RoutedEventArgs e)
        {

            
        }

        double NoteMaxVlaue = 0;
        ScrollViewer scroller;
        private void Image_ScrollChanged(object sender, RoutedEventArgs e)
        {
            //scroller = (ScrollViewer)e.OriginalSource;
            //for (int i = 0; i < ImageListBox.Items.Count; i++)
            //{
            //    DependencyObject Item = ImageListBox.ItemContainerGenerator.ContainerFromIndex(i);
            //    if (Item != null)
            //    {//依赖虚拟化对视口元素进行处理
            //        Image ItemImageControl = UIhelper.FindChild<Image>(Item, null);
            //        //获取PlayListAlbumImageShowListBox实际宽度的一半，
            //        //减去ItemImageControl中心点在PlayListAlbumImageShowListBox中心点的相对位置
            //        double X = Math.Abs(ImageListBox.ActualWidth / 2 - ItemImageControl.TranslatePoint(new Point(ItemImageControl.ActualWidth / 2, 0), ImageListBox).X);
            //        double Xscale = (X) / ((ImageListBox.ActualWidth / 2));
            //        Xscale -= DecRatio;//0.2是系数，用来设置最小项目大小：0.2*原始宽度
            //        if (Xscale > 1) continue;
            //          //  Xscale = 0.5; //处理超出视口后的元素，因为布局原因（这些元素的尺寸会对之前或者之后的Item的位置造成影响），没有直接中断循环
            //        ItemImageControl.Width = itemImageWidth * (1 - Xscale);//110是指的原始图片控件的大小，后面的系数表示在此基础上进行的递减值
            //        if (NoteMaxVlaue < ItemImageControl.Width)
            //            NoteMaxVlaue = ItemImageControl.Width;
            //    }
            //}
        }//另外可以使用二分法或者索引对集合的遍历进行优化

        private void ImageListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectIndex = ImageListBox.SelectedIndex;
            if (selectIndex == -1) return;
            var TotalWidth = ImageListBox.ActualWidth;
            var Height = ImageListBox.ActualHeight;
            double Width;
            int ImageCount;
            DependencyObject Item = ImageListBox.ItemContainerGenerator.ContainerFromIndex(0);
            if (itemImageWidth == -1 && Item != null)
            {
                itemImageWidth = Width = UIhelper.FindChild<Image>(Item, null).Width;
            }
            else Width = itemImageWidth;
            ImageCount = (int)(TotalWidth / Width);

            //Width += 50;
            for (int i = 0; i < selectIndex; i++) {
                Item = ImageListBox.ItemContainerGenerator.ContainerFromIndex(i);
                if (Item == null) break;
                Image ItemImageControl = UIhelper.FindChild<Image>(Item, null);
                ItemImageControl.Width = Width + Width * (i+1) / (selectIndex+1);
            }
            for (int i = selectIndex; i <= ImageCount; i++)
            {
                Item = ImageListBox.ItemContainerGenerator.ContainerFromIndex(i);
                if (Item == null) break;

                Image ItemImageControl = UIhelper.FindChild<Image>(Item, null);
                ItemImageControl.Width = Width + Width * (i + 1) / (selectIndex + 1);
            }
        }

        double ScrOff = 0;
        private void ImageListBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (scroller == null)
                scroller = Helper.UIhelper.FindChild<ScrollViewer>((ListBox)sender, null);
            ScrOff = scroller.ContentHorizontalOffset-e.Delta * 0.009;
            scroller.ScrollToHorizontalOffset(ScrOff);
            //for (int i = 0; i < ImageListBox.Items.Count; i++)
            //{
            //    DependencyObject Item = ImageListBox.ItemContainerGenerator.ContainerFromIndex(i);
            //    if (Item != null)
            //    {//依赖虚拟化对视口元素进行处理
            //        Image ItemImageControl = UIhelper.FindChild<Image>(Item, null);
            //        //获取PlayListAlbumImageShowListBox实际宽度的一半，
            //        //减去ItemImageControl中心点在PlayListAlbumImageShowListBox中心点的相对位置
            //        double X = Math.Abs(ImageListBox.ActualWidth / 2 - ItemImageControl.TranslatePoint(new Point(ItemImageControl.ActualWidth / 2, 0), ImageListBox).X);
            //        double Xscale = (X) / ((ImageListBox.ActualWidth / 2));
            //        Xscale -= DecRatio;//0.2是系数，用来设置最小项目大小：0.2*原始宽度
            //        if (Xscale > 1)
            //            Xscale = B_Xscale; //处理超出视口后的元素，因为布局原因（这些元素的尺寸会对之前或者之后的Item的位置造成影响），没有直接中断循环
            //        else
            //            B_Xscale = Xscale;
            //        ItemImageControl.Width = itemImageWidth * (1 - Xscale);//110是指的原始图片控件的大小，后面的系数表示在此基础上进行的递减值
            //        if (NoteMaxVlaue < ItemImageControl.Width)
            //            NoteMaxVlaue = ItemImageControl.Width;
            //    }
            //}
        }
        /// <summary>
        /// 平滑函数y=at+b/ct+d【y(0)=b/d,y(∞)=a/c】的导数
        /// </summary>
        /// <param name="sourceWidth"></param>
        /// <param name="LmitValue">极限值</param>
        /// <param name="SmValue">平滑值</param>
        /// <param name="x">x</param>
        private double CountImageWidth(double sourceWidth, double LmitValue, double SmValue,int x) {
            // double value = LmitValue / (x + SmValue) +
            //            (-LmitValue * x - (sourceWidth / 2.0) * SmValue) / Math.Pow(x + SmValue, 2);

            double xMin = sourceWidth / 2.0;
            double value = (LmitValue * x + xMin * SmValue) / (x + SmValue);
            return value;
        }
    }
}
