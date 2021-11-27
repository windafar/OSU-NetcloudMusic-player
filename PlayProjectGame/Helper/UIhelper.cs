using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace PlayProjectGame.Helper
{
   static public class UIhelper
    {
        public static void SetBlurBitmapImage(System.Windows.Controls.Image SecBK, System.Windows.Controls.Image FirBK, Bitmap bitmap)
        {

            Thread Blur_Thread = new Thread(delegate ()
            {
                try
                {
                    BitmapImage img = UIhelper.Blur(bitmap, 25);
                    img.Freeze();
                    bitmap.Dispose();
                    SecBK.Dispatcher.BeginInvoke((ThreadStart)delegate
                    {
                        SecBK.Opacity = 0;
                        SecBK.Source = img;
                        Storyboard STRORY = new Storyboard();

                        //AimageBK_Opacity渐现动画
                        DoubleAnimation OpeAnimation = new DoubleAnimation();
                        OpeAnimation.From = 0;
                        OpeAnimation.To = 1;
                        OpeAnimation.Duration = TimeSpan.FromSeconds(1);
                        Storyboard.SetTarget(OpeAnimation, SecBK);
                        Storyboard.SetTargetProperty(OpeAnimation, new PropertyPath("Opacity"));
                        STRORY.Children.Add(OpeAnimation);
                        STRORY.Completed += (sender, e)=> 
                        {
                            FirBK.Source = img;
                            ((ClockGroup)sender).Controller.Remove();
                            //((ClockGroup)sender).Completed -= OpacityAminComple;
                            STRORY.Remove();
                        };
                        STRORY.Begin();
                        //STRORY.Children.Clear();
                    });
                }
                catch
                {
                    //  AimageBK.Source = null;
                    Thread.CurrentThread.Abort();
                }
            });
            Blur_Thread.Start();
        }

        public static BitmapImage ConverTotBitmapImage(Bitmap bitmap) {
            if (bitmap == null) return null;
            MemoryStream ms = new MemoryStream();
            ms.Seek(0, SeekOrigin.Begin);
            try
            {
                bitmap.Save(ms, ImageFormat.Jpeg);
            }
            catch(Exception e) {
                Debug.Print(e.Message);
                ms.Dispose();
                bitmap.Dispose();
                return null;
            }
            bitmap.Dispose();
            ms.Seek(0, SeekOrigin.Begin);
            var bitImage = new BitmapImage();
            bitImage.BeginInit();
            bitImage.StreamSource = ms;
            bitImage.EndInit();
            return bitImage;
        }
        /// <summary>
        /// 转换BitmapImage成Bitmap
        /// </summary>
        /// <param name="bt"></param>
        /// <returns></returns>
        public static Bitmap ConveBitmapImage(BitmapImage bt) //BitmapImage
        {
            if (bt.StreamSource != null)
            {
                Stream stream = ((BitmapImage)bt).StreamSource;
                return new Bitmap(stream);
            }
            else if (bt.UriSource.LocalPath != null)
                return new Bitmap(bt.UriSource.LocalPath);

            else
                return null;
        }

        public static BitmapImage ConveBitmapImage(Stream stream)
        {
            Bitmap bit = new Bitmap(stream);

            return ConverTotBitmapImage(bit);
            ////BitmapImage bitmap = new BitmapImage();
            ////bitmap.BeginInit();
            ////stream.Seek(0, SeekOrigin.Begin);

            ////bitmap.StreamSource = stream;
            ////try
            ////{
            ////    try
            ////    {
            ////        stream.Seek(0, SeekOrigin.Current);
            ////        bitmap.EndInit();
            ////    }
            ////    catch (NotSupportedException)
            ////    {
            ////        MemoryStream saveStream = new MemoryStream();
            ////        if (!MakeThumbnail(stream, saveStream, 602, 602, "W", "jpg"))
            ////            return null;
            ////        bitmap = new BitmapImage();
            ////        bitmap.BeginInit();
            ////        bitmap.CacheOption = BitmapCacheOption.OnLoad;
            ////        saveStream.Seek(0, SeekOrigin.Begin);
            ////        saveStream.Seek(0, SeekOrigin.Current); bitmap.StreamSource = saveStream;
            ////        bitmap.EndInit();
            ////        bitmap.Freeze();
            ////        saveStream.Dispose();
            ////    }
            ////    bitmap.Freeze();

            ////}
            ////catch (Exception)
            ////{

            ////    return null;
            ////}


            //return bitmap;
        }

        /// <summary>
        /// 模糊一个Bitmap，传回BitmapImage
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns></returns>
        public static BitmapImage Blur(Bitmap bmp, int od = 50)
        {

            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            GdipEffect.Effect.GaussianBlur(bmp, ref rect, od, false);
            try
            {     
                return ConverTotBitmapImage(bmp);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
                return null;
            }

            // return new BitmapImage(new Uri(@"G:\Back\Desktop\blur.bmp", UriKind.Absolute));

        }

        public static Bitmap Copy(Bitmap bitmap) {
            return new Bitmap(bitmap);
        }
        public static T FindChild<T>(DependencyObject parent, string childName)
where T : DependencyObject
        {
            if (parent == null)
                return null;
            T foundChild = null;
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // 如果子控件不是需查找的控件类型 
                T childType = child as T;
                if (childType == null)
                {// 在下一级控件中递归查找 
                    foundChild = FindChild<T>(child, childName);

                    // 找到控件就可以中断递归操作  
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // 如果控件名称符合参数条件 
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // 查找到了控件 
                    foundChild = (T)child;
                    break;
                }
            }
            return foundChild;
        }

        public static T FindPrent<T>(DependencyObject Child,int maxcount=1024) where T : DependencyObject
        {
            object FondChild = null;
            while (maxcount-->0&&Child != null&&(FondChild=Child = VisualTreeHelper.GetParent(Child)) as T == null) ;
            return FondChild as T;
        }

        static Rect rcnormal;//定义一个全局rect记录还原状态下窗口的位置和大小。
        public static bool IsMaxWindow = false;
        /// <summary>
        /// 解决窗口最大化和Border阴影效果的不和谐
        /// </summary>
        static public void WindowMaxEx(MainWindow THIS,Thickness thickness)
        {
            if (!IsMaxWindow)
            {//最大化
                rcnormal = new Rect(THIS.Left, THIS.Top, THIS.Width, THIS.Height);//保存下当前位置与大小
                THIS.Left = 0- thickness.Left;//设置位置
                THIS.Top = 0-thickness.Top;
                Rect rc = SystemParameters.WorkArea;//获取工作区大小
                THIS.Width = rc.Width+ thickness.Right+ thickness.Left;
                THIS.Height = rc.Height+thickness.Bottom + thickness.Top;
                IsMaxWindow = true;
            }
            else
            {//还原
                WindowSizeRestore(THIS);
            }
        }

        static public void WindowSizeRestore(MainWindow THIS)
        {
            if (IsMaxWindow)
            {
                THIS.Left = rcnormal.Left;
                THIS.Top = rcnormal.Top;
                THIS.Width = rcnormal.Width;
                THIS.Height = rcnormal.Height;
                IsMaxWindow = false;
            }
         //   THIS.Visibility = Visibility.Collapsed;
        }

        static public Rectangle GetSystemDpi()
        {
            float dpiX, dpiY;
            Graphics graphics = Graphics.FromImage(new Bitmap(1,1));
            dpiX = graphics.DpiX;
            dpiY = graphics.DpiY;
            return new Rectangle(0, 0, (int)dpiX, (int)dpiY);
        }
        static Timer timer;
        static public void Throttle(Action action,int sec)
        {
           timer = new Timer((obj) =>
              {
                      action();
              }, null, Timeout.Infinite, sec);
        }

        /// <summary>
        /// SelectFile对话框
        /// </summary>
        /// <param name="type">type：text，audio</param>
        /// <returns></returns>
        public static string[] SelectFile(string type)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>
            {
                { "text" , "文档文件(*.doc,*.xls,*.txt)|*.doc;*.xls;*.txt|All files (*.*)|*.*"},
                { "audio","音频文件(*.mp3,*.flac,*.wav)|*.mp3;*.flac;*.wav|All files (*.*)|*.*"}
            };

        var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = dic[type],
                Multiselect = true,//: \"图像文件(*.bmp, *.jpg)|*.bmp;*.jpg|所有文件(*.*)|*.*\"”

            };
            var result = openFileDialog.ShowDialog();
            if (result == true)
            {
                return openFileDialog.FileNames;
            }
            else
            {
                return null;
            }
        }


    }
}
