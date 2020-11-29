using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace PlayProjectGame.PublicConver
{
    class FilePathToFileInfo : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = (string)value;
            string param = ((string)parameter).ToLower();
            FileInfo fileInfo = new FileInfo(path);
            if (param == "fullname")
                return fileInfo.FullName;
            else if (param == "exists" || !fileInfo.Exists)
                return fileInfo.Exists;
            else if (param == "name")
                return fileInfo.Name;
            else if (param == "length")
                return fileInfo.Length / 1.0 / 1048576+" Mb";
            else if (param == "creationtime")
                return fileInfo.CreationTime.ToLongTimeString();
            else if (param == "lastwritetime")
                return fileInfo.LastWriteTime.ToLongTimeString();
            else if (param == "samplerate") 
            {
                var info = PlayList.PlayListBase.player.GetCodecInfo(path, param).FirstOrDefault(x => x.Key.ToLower() == param);
                if (info.Key == null) return "";
                return info.Value;
            }
            else if (param == "bytespersecond")
            {
                var info = PlayList.PlayListBase.player.GetCodecInfo(path, param).FirstOrDefault(x => x.Key.ToLower() == param);
                if (info.Key == null) return "";
                return info.Value;
            }
            else if (param == "bitspersample")
            {
                var info = PlayList.PlayListBase.player.GetCodecInfo(path, param).FirstOrDefault(x => x.Key.ToLower() == param);
                if (info.Key == null) return "";
                return info.Value;
            }
            else return fileInfo;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
