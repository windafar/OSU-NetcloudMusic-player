using PlayProjectGame.Helper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace PlayProjectGame.PlayList
{
   public class ConvaSongInfoToAlbum : IValueConverter
    {//转换歌曲路径为BitmapImage专辑图片
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //SongInfo sInfo = (value as SongInfoExpend).SongInfo;
            //if (sInfo.SongType == 3)
            //{
            //    string path = OsoDataGeter.LoadOsuFileNameAs(1, sInfo.OsuPath);
            //    if (System.IO.File.Exists(path))
            //    {
            //        //  return new BitmapImage(new Uri(path, UriKind.Absolute));
            //        BitmapImage c = new BitmapImage();
            //        new SalientRegionDetection().SetSRDFromPath(path, c
            //            , new System.Drawing.Rectangle(0, 0, 475, 120));
            //        return c;
            //    }
            //    else { return new BitmapImage(new Uri(@"..\AppResource\un.jpg", UriKind.Relative)); }
            //}
            //else
            //{
            //    var filestream = new MusicTag().GetJPGStream(sInfo.SongPath);
            //    BitmapImage c = new BitmapImage();
            //    if (filestream != null)
            //    {
            //        new SalientRegionDetection().SetSRDFromStream(filestream, c
            //            , new System.Drawing.Rectangle(0, 0, 475, 120));
            //        return c;
            //    }
            //    else return new BitmapImage(new Uri(@"..\AppResource\un.jpg", UriKind.Relative));
            //}
            return new BitmapImage(new Uri(@"..\AppResource\un.jpg", UriKind.Relative));

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
