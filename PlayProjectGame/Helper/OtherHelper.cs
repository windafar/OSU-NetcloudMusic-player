using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PlayProjectGame.Helper
{
    static public class OtherHelper
    {
        
        /// <summary>
        /// 非法路径字符替换\/:*?"<>|
        /// </summary>
        /// <param name="fileName">文件名,不包含路径</param>
        /// <param name="c">要替换成的字符</param>
        /// <returns></returns>
        public static string ReplaceValidFileName(string fileName, char c)
        {
            string errChar = "\\/:*?\"<>|";  //
            for (int i = 0; i < errChar.Length && fileName.Length > 0; i++)
            {
                fileName=fileName.Replace(errChar[i], c);
            }
            return fileName;
        }
        /// <summary>
        /// 用于云音乐转换非法字符，使其能以“艺术家 - 歌曲”命名
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string ReplaceValidFileName(string fileName)
        {
            StringBuilder FileNameBud = new StringBuilder(fileName);
            char index;
            for (int i = 0; i < FileNameBud.Length; i++)
            {
                index = FileNameBud[i];
                if (FileNameBud[i] == '/') FileNameBud[i] = '／';
                if (FileNameBud[i] == ':') FileNameBud[i] = '：';
                if (FileNameBud[i] == '<') FileNameBud[i] = '＜';
                if (FileNameBud[i] == '>') FileNameBud[i] = '＞';
                if (FileNameBud[i] == '\\')FileNameBud[i] = ' ';
                if (FileNameBud[i] == '\r')FileNameBud[i] = ' ';
                if (FileNameBud[i] == '\n')FileNameBud[i] = ' ';
                if (FileNameBud[i] == '\t') FileNameBud[i] = ' ';
                if (FileNameBud[i] == '?') FileNameBud[i] = '？';
                if (FileNameBud[i] == '"') FileNameBud[i] = '＂';
                if (FileNameBud[i] == '*') FileNameBud[i] = '﹡';
            }
            return FileNameBud.ToString();
        }
        /// <summary>
        /// 判断文件类型函数
        /// </summary>
        /// <param name="file">文件对象</param>
        /// <returns></returns>
        static public int US_JuFileType(FileInfo file)
        {
            byte[] ByteBuff = new byte[7];

            try
            {
                (file.OpenRead()).Read(ByteBuff, 0, 7);
            }
            catch (Exception)
            {
                return 0;
            }

            if (ByteBuff[0] == 0x49 && ByteBuff[1] == 0x44 && ByteBuff[2] == 0x33)
                return 30;//mp3
            else if (ByteBuff[0] == 0x52 && ByteBuff[1] == 0x49 && ByteBuff[2] == 0x46 && ByteBuff[3] == 0x46)
                return 31;//RIFF
            else if (ByteBuff[0] == 0x66 && ByteBuff[1] == 0x4c && ByteBuff[2] == 0x61 && ByteBuff[3] == 0x43)
                return 32;//flac
            else if (ByteBuff[0] == 0xff && ByteBuff[1] == 0xfb && ByteBuff[2] == 0xe4 && ByteBuff[3] == 0x04)
                return 33;//mp3
            else if (ByteBuff[0] == 0xff && ByteBuff[1] == 0xfb && ByteBuff[2] == 0xd0 && ByteBuff[3] == 0x64)
                return 39;//lame_map3
            else if (ByteBuff[0] == 0xff && ByteBuff[1] == 0xd8 && ByteBuff[2] == 0xff)
                return 11;//jpg
            else if (ByteBuff[0] == 0x89 && ByteBuff[1] == 0x50 && ByteBuff[2] == 0x4e && ByteBuff[3] == 0x47)
                return 12;//png
            else if (ByteBuff[0] == 0x42 && ByteBuff[1] == 0x4d)
                return 13;//bmp
            else { return 0; }

        }

        /// <summary>
        /// 把对象序列化为字节数组
        /// </summary>
        public static byte[] SerializeObject(object obj)
        {
            if (obj == null)
                return null;
            MemoryStream ms = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(ms, obj);
            ms.Position = 0;
            byte[] bytes = new byte[ms.Length];
            ms.Read(bytes, 0, bytes.Length);
            ms.Close();
            return bytes;
        }

        /// <summary>
        /// 把字节数组反序列化成对象
        /// </summary>
        public static object DeserializeObject(byte[] bytes)
        {
            object obj = null;
            if (bytes == null)
                return obj;
            MemoryStream ms = new MemoryStream(bytes);
            ms.Position = 0;
            BinaryFormatter formatter = new BinaryFormatter();
            obj = formatter.Deserialize(ms);
            ms.Close();
            return obj;
        }


        /// <summary>
        /// 序列化对象
        /// </summary>
        /// <param name="obj">将要序列化的对象</param>
        /// <param name="T">将要序列化的对的类型</param>
        /// <param name="path">xml文件存放位置(覆盖)</param>
        static public void WriteXMLSerializer(object obj, Type T, string path)
        {
            XmlSerializer xs = new XmlSerializer(T);
            try
            {
                FileStream fs = File.Create(path);
                xs.Serialize(fs, obj);
                fs.Dispose();
            }
            catch (Exception)
            {
                System.Windows.MessageBox.Show("保存到磁盘失败，文件正在被使用，将在不被占用时自动保存", "文件保存");
            }

        }


        /// <summary>
        /// 使用反射获取类的属性值
        /// </summary>
        /// <typeparam name="T">所在类</typeparam>
        /// <param name="t">类的对象</param>
        /// <param name="propertyname">类中包含的属性</param>
        /// <returns></returns>
        public static string GetObjectPropertyValue<T>(T t, string propertyname)
        {
            Type type = typeof(T);

            PropertyInfo property = type.GetProperty(propertyname);

            if (property == null) return string.Empty;

            object o = property.GetValue(t, null);

            if (o == null) return string.Empty;

            return o.ToString();
        }

        public static Process StartAudioProcesses() {
            var pros = Process.GetProcesses().FirstOrDefault(x=>x.ProcessName== "AudioSTAProgress");
            if (pros == null)
            {
               string str= Environment.CurrentDirectory+"\\";
                pros = Process.Start(str+@"AudioSTAProgress.exe");
            }
            pros.PriorityClass = ProcessPriorityClass.High;
            return pros;
        }
    }
}