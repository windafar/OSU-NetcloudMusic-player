using PlayProjectGame.Data;
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


namespace PlayProjectGame.UserDefinedControl
{
   public class ConvaSongInfoToAlbum : IValueConverter
    {//转换歌曲路径为BitmapImage专辑图片
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            SongInfo sInfo =( value as SongInfoExpend).SongInfo;
            if (sInfo.SongType == 3)
            {
               string path= OsuLocalDataGeter.LoadOsuFileNameAs(1, sInfo.OsuPath);
                if(System.IO.File.Exists(path))
                return new BitmapImage(new Uri(path, UriKind.Absolute));
            }
            var result = new MusicTag().GetJPGFromStream(sInfo);
            if(result==null)
                return new BitmapImage(new Uri("./Resource/未找到专辑图片.jpg", UriKind.Relative));

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
