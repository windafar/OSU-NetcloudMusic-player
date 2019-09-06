using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ImageBasic
{
    static public class BasicMethodClass
    {
        #region winAPI声明

        [System.Runtime.InteropServices.DllImportAttribute("gdi32.dll")]
        private static extern bool BitBlt
        (
            IntPtr hdcDest,    //目标DC的句柄
            int nXDest,        //目标DC的矩形区域的左上角的x坐标
            int nYDest,        //目标DC的矩形区域的左上角的y坐标
            int nWidth,        //目标DC的句型区域的宽度值
            int nHeight,       //目标DC的句型区域的高度值
            IntPtr hdcSrc,     //源DC的句柄
            int nXSrc,         //源DC的矩形区域的左上角的x坐标
            int nYSrc,         //源DC的矩形区域的左上角的y坐标
            System.Int32 dwRo  //光栅的处理数值
        );

        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        private extern static IntPtr FindWindow(string lpClassName, string lpWindowName);


        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        private extern static IntPtr GetDC(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        private extern static int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rectangle rect);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(
         IntPtr hdc // handle to DC
);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(
         IntPtr hdc,         // handle to DC
         int nWidth,      // width of bitmap, in pixels
         int nHeight      // height of bitmap, in pixels
         );
        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(
         IntPtr hdc,           // handle to DC
         IntPtr hgdiobj    // handle to object
         );
        [DllImport("gdi32.dll")]
        private static extern int DeleteDC(
         IntPtr hdc           // handle to DC
         );
        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject
        );

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(
         IntPtr hwnd,                // Window to copy,Handle to the window that will be copied.
         IntPtr hdcBlt,              // HDC to print into,Handle to the device context.
         UInt32 nFlags               // Optional flags,Specifies the drawing options. It can be one of the following values.
         );
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(
         IntPtr hwnd
         );
        // FROM TernaryRasterOperations枚举
        static int SRCCOPY = 0x00CC0020; /* dest = source */

        #endregion

        #region 设备相关
        ///<summary>
        /// Create and initialize grayscale image
        ///</summary>
        public static Bitmap CreateGrayscaleImage(int width, int height)
        {
            // create new image
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format8bppIndexed);
            // set palette to grayscale
            Set8bitGrayscalePalette(bmp);
            // return new image
            return bmp;
        }
        ///<summary>
        /// Set pallete of the image to grayscale of 8 bit(Every PixelPoint 8bit)
        ///</summary>
        public static void Set8bitGrayscalePalette(Bitmap srcImg)
        {
            // check pixel format
            if (srcImg.PixelFormat != PixelFormat.Format8bppIndexed)
                throw new ArgumentException();
            // get palette
            ColorPalette cp = srcImg.Palette;
            // init palette
            for (int i = 0; i < 256; i++)
            {
                cp.Entries[i] = Color.FromArgb(i, i, i);
            }
            srcImg.Palette = cp;
        }
        /// <summary>
        /// 使用PrintWindow进行窗口截图
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        public static Bitmap GetWindowCapture(IntPtr hWnd)
        {
            IntPtr hscrdc = GetWindowDC(hWnd);
            Rectangle windowRect = new Rectangle();
            GetWindowRect(hWnd, ref windowRect);
            int width = Math.Abs(windowRect.X - windowRect.Width);
            int height = Math.Abs(windowRect.Y - windowRect.Height);
            IntPtr hbitmap = CreateCompatibleBitmap(hscrdc, width, height);
            IntPtr hmemdc = CreateCompatibleDC(hscrdc);
            IntPtr olderImage= SelectObject(hmemdc, hbitmap);
            PrintWindow(hWnd, hmemdc, 0);
            Bitmap bmp = Image.FromHbitmap(hbitmap);
            DeleteObject(hbitmap);//删除用过的对象
            ReleaseDC(hWnd, hscrdc);
            //DeleteDC(hscrdc);//删除用过的对象
            DeleteObject(olderImage);
            DeleteDC(hmemdc);//删除用过的对象
            return bmp;
        }
        /// <summary>
        /// 使用BitBlt进行窗口截图
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Bitmap GetBitmapFromDC(IntPtr hwnd, int width, int height)
        {
            IntPtr windc = GetDC(hwnd);//获取窗口DC
            IntPtr hDCMem = CreateCompatibleDC(windc);//为设备描述表创建兼容的内存设备描述表
            IntPtr hbitmap = CreateCompatibleBitmap(windc, width, height);
            IntPtr hOldBitmap = (IntPtr)SelectObject(hDCMem, hbitmap);
            BitBlt(hDCMem, 0, 0, width, height, windc, 0, 0, SRCCOPY);
            hbitmap = (IntPtr)SelectObject(hDCMem, hOldBitmap);
            Bitmap bitmap = Bitmap.FromHbitmap(hbitmap);
            DeleteObject(hbitmap);//删除用过的对象
            DeleteObject(hOldBitmap);//删除用过的对象
            DeleteDC(hDCMem);//删除用过的对象
            ReleaseDC(hwnd, windc);
            return bitmap;
        }

        #endregion

        #region 颜色转换
        /// <summary>
        /// 获取24位深RGB到8位灰阶图
        /// </summary>
        /// <param name="srcBitmap"></param>
        /// <returns></returns>
        public static Bitmap RGB2Gray(Bitmap srcBitmap)
        {
            int wide = srcBitmap.Width;
            int height = srcBitmap.Height;
            Rectangle rect = new Rectangle(0, 0, wide, height);
            BitmapData srcBmData = srcBitmap.LockBits(rect,
                      ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            Bitmap dstBitmap = CreateGrayscaleImage(wide, height);
            BitmapData dstBmData = dstBitmap.LockBits(rect,
                      ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
            System.IntPtr srcScan = srcBmData.Scan0;
            System.IntPtr dstScan = dstBmData.Scan0;
            unsafe //启动不安全代码
            {
                byte* srcP = (byte*)(void*)srcScan;
                byte* dstP = (byte*)(void*)dstScan;
                int srcOffset = srcBmData.Stride - wide * 3;
                int dstOffset = dstBmData.Stride - wide;
                byte red, green, blue;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < wide; x++, srcP += 3, dstP++)
                    {
                        blue = srcP[0];
                        green = srcP[1];
                        red = srcP[2];
                        *dstP = (byte)(.299 * red + .587 * green + .114 * blue);
                    }
                    srcP += srcOffset;
                    dstP += dstOffset;
                }
            }
            srcBitmap.UnlockBits(srcBmData);
            dstBitmap.UnlockBits(dstBmData);
            return dstBitmap;
        }
        /// <summary>
        /// 转换24位RGB到LAB空间，同时输出平均L、A、B值，返回double[]{L,A,B}
        /// </summary>
        /// <param name="srcbitampData">源bitmap的BitmapData</param>
        /// <param name="srcWidth">源图宽度</param>
        /// <param name="srcHeight">原图高度</param>
        /// <param name="MeanL">输出平均L</param>
        /// <param name="MeanA">输出平均A</param>
        /// <param name="MeanB">输出平均B</param>
        /// <returns></returns>
        public static double[] sRGBtoXYZ(BitmapData srcbitampData, int srcWidth, int srcHeight, out double MeanL, out double MeanA, out double MeanB)
        {
            MeanL = 0; MeanA = 0; MeanB = 0;
            IntPtr srcScan = srcbitampData.Scan0;
            //int srcWidth=srcbitampData.Width,srcHeight = srcbitampData.Height;
            int srcStride = srcbitampData.Stride;
            double R, G, B;
            double[] LabArr = new double[srcbitampData.Height * srcStride];
            int index;
            unsafe //启动不安全代码
            {
                byte* srcP = (byte*)srcScan;
                fixed (double* pLab = LabArr)
                {
                    for (int y = 0; y < srcHeight; y++)
                    {
                        index = y * srcStride;
                        for (int x = 0; x < srcWidth; x++)
                        {
                            R = srcP[index] / 255.0;
                            G = srcP[index + 1] / 255.0;
                            B = srcP[index + 2] / 255.0;

                            double r, g, b;

                            if (R <= 0.04045) r = R / 12.92;
                            else r = Math.Pow((R + 0.055) / 1.055, 2.4);
                            if (G <= 0.04045) g = G / 12.92;
                            else g = Math.Pow((G + 0.055) / 1.055, 2.4);
                            if (B <= 0.04045) b = B / 12.92;
                            else b = Math.Pow((B + 0.055) / 1.055, 2.4);

                            double X = r * 0.4124564 + g * 0.3575761 + b * 0.1804375;
                            double Y = r * 0.2126729 + g * 0.7151522 + b * 0.0721750;
                            double Z = r * 0.0193339 + g * 0.1191920 + b * 0.9503041;
                            //------------------------
                            // XYZ to LAB conversion
                            //------------------------
                            double epsilon = 0.008856;  //actual CIE standard
                            double kappa = 903.3;       //actual CIE standard

                            double Xr = 0.950456;   //reference white
                            double Yr = 1.0;        //reference white
                            double Zr = 1.088754;   //reference white

                            double xr = X / Xr;
                            double yr = Y / Yr;
                            double zr = Z / Zr;

                            double fx, fy, fz;
                            if (xr > epsilon) fx = Math.Pow(xr, 1.0 / 3.0);
                            else fx = (kappa * xr + 16.0) / 116.0;
                            if (yr > epsilon) fy = Math.Pow(yr, 1.0 / 3.0);
                            else fy = (kappa * yr + 16.0) / 116.0;
                            if (zr > epsilon) fz = Math.Pow(zr, 1.0 / 3.0);
                            else fz = (kappa * zr + 16.0) / 116.0;

                            MeanL += (pLab[index] = 116.0 * fy - 16.0);
                            MeanA += (pLab[index + 1] = 500.0 * (fx - fy));
                            MeanB += (pLab[index + 2] = 200.0 * (fy - fz));

                            index += 3;
                        }//for x in width
                    }//for y in height
                }//fixed
            }//unsafe
            MeanL /= (srcWidth * srcHeight);                                            //    求LAB空间的平均值
            MeanA /= (srcWidth * srcHeight);
            MeanB /= (srcWidth * srcHeight);
            return LabArr;
        }
        public static System.Drawing.Bitmap ConverImageTo24bit(Bitmap sourceImage)
        {
            switch (sourceImage.PixelFormat)
            {
                case PixelFormat.Format24bppRgb:
                    return sourceImage;
                default:
                    MemoryStream ms = new MemoryStream();
                    SaveImageForSpecifiedQuality(sourceImage, ms, 90, ImageFormat.Jpeg);
                    sourceImage.Dispose();
                    return new System.Drawing.Bitmap(ms);
            }
        }

        /// <summary>
        /// LAB-RGB的快速转换
        /// <see cref="https://www.cnblogs.com/xiaotie/archive/2011/01/13/1934170.html"/>
        /// </summary>
        public sealed class UnmanagedImageConverter
        {
            public partial struct Rgb24
            {
                public static Rgb24 WHITE = new Rgb24 { Red = 255, Green = 255, Blue = 255 };
                public static Rgb24 BLACK = new Rgb24();
                public static Rgb24 RED = new Rgb24 { Red = 255 };
                public static Rgb24 BLUE = new Rgb24 { Blue = 255 };
                public static Rgb24 GREEN = new Rgb24 { Green = 255 };

                public Byte Blue;
                public Byte Green;
                public Byte Red;

                public Rgb24(int red, int green, int blue)
                {
                    Red = (byte)red;
                    Green = (byte)green;
                    Blue = (byte)blue;
                }

                public Rgb24(byte red, byte green, byte blue)
                {
                    Red = red;
                    Green = green;
                    Blue = blue;
                }
            }

            public partial struct Lab24
            {
                public byte L;
                public byte A;
                public byte B;

                public Lab24(byte l, byte a, byte b)
                {
                    L = l;
                    A = a;
                    B = b;
                }

                public Lab24(int l, int a, int b)
                {
                    L = (byte)l;
                    A = (byte)a;
                    B = (byte)b;
                }
            }

            /* 1024*(([0..511]./255)**(1./3)) */
            static ushort[] icvLabCubeRootTab = new ushort[] {
    0,161,203,232,256,276,293,308,322,335,347,359,369,379,389,398,
    406,415,423,430,438,445,452,459,465,472,478,484,490,496,501,507,
    512,517,523,528,533,538,542,547,552,556,561,565,570,574,578,582,
    586,590,594,598,602,606,610,614,617,621,625,628,632,635,639,642,
    645,649,652,655,659,662,665,668,671,674,677,680,684,686,689,692,
    695,698,701,704,707,710,712,715,718,720,723,726,728,731,734,736,
    739,741,744,747,749,752,754,756,759,761,764,766,769,771,773,776,
    778,780,782,785,787,789,792,794,796,798,800,803,805,807,809,811,
    813,815,818,820,822,824,826,828,830,832,834,836,838,840,842,844,
    846,848,850,852,854,856,857,859,861,863,865,867,869,871,872,874,
    876,878,880,882,883,885,887,889,891,892,894,896,898,899,901,903,
    904,906,908,910,911,913,915,916,918,920,921,923,925,926,928,929,
    931,933,934,936,938,939,941,942,944,945,947,949,950,952,953,955,
    956,958,959,961,962,964,965,967,968,970,971,973,974,976,977,979,
    980,982,983,985,986,987,989,990,992,993,995,996,997,999,1000,1002,
    1003,1004,1006,1007,1009,1010,1011,1013,1014,1015,1017,1018,1019,1021,1022,1024,
    1025,1026,1028,1029,1030,1031,1033,1034,1035,1037,1038,1039,1041,1042,1043,1044,
    1046,1047,1048,1050,1051,1052,1053,1055,1056,1057,1058,1060,1061,1062,1063,1065,
    1066,1067,1068,1070,1071,1072,1073,1074,1076,1077,1078,1079,1081,1082,1083,1084,
    1085,1086,1088,1089,1090,1091,1092,1094,1095,1096,1097,1098,1099,1101,1102,1103,
    1104,1105,1106,1107,1109,1110,1111,1112,1113,1114,1115,1117,1118,1119,1120,1121,
    1122,1123,1124,1125,1127,1128,1129,1130,1131,1132,1133,1134,1135,1136,1138,1139,
    1140,1141,1142,1143,1144,1145,1146,1147,1148,1149,1150,1151,1152,1154,1155,1156,
    1157,1158,1159,1160,1161,1162,1163,1164,1165,1166,1167,1168,1169,1170,1171,1172,
    1173,1174,1175,1176,1177,1178,1179,1180,1181,1182,1183,1184,1185,1186,1187,1188,
    1189,1190,1191,1192,1193,1194,1195,1196,1197,1198,1199,1200,1201,1202,1203,1204,
    1205,1206,1207,1208,1209,1210,1211,1212,1213,1214,1215,1215,1216,1217,1218,1219,
    1220,1221,1222,1223,1224,1225,1226,1227,1228,1229,1230,1230,1231,1232,1233,1234,
    1235,1236,1237,1238,1239,1240,1241,1242,1242,1243,1244,1245,1246,1247,1248,1249,
    1250,1251,1251,1252,1253,1254,1255,1256,1257,1258,1259,1259,1260,1261,1262,1263,
    1264,1265,1266,1266,1267,1268,1269,1270,1271,1272,1273,1273,1274,1275,1276,1277,
    1278,1279,1279,1280,1281,1282,1283,1284,1285,1285,1286,1287,1288,1289,1290,1291
    };

            const float labXr_32f = 0.433953f /* = xyzXr_32f / 0.950456 */;
            const float labXg_32f = 0.376219f /* = xyzXg_32f / 0.950456 */;
            const float labXb_32f = 0.189828f /* = xyzXb_32f / 0.950456 */;

            const float labYr_32f = 0.212671f /* = xyzYr_32f */;
            const float labYg_32f = 0.715160f /* = xyzYg_32f */;
            const float labYb_32f = 0.072169f /* = xyzYb_32f */;

            const float labZr_32f = 0.017758f /* = xyzZr_32f / 1.088754 */;
            const float labZg_32f = 0.109477f /* = xyzZg_32f / 1.088754 */;
            const float labZb_32f = 0.872766f /* = xyzZb_32f / 1.088754 */;

            const float labRx_32f = 3.0799327f  /* = xyzRx_32f * 0.950456 */;
            const float labRy_32f = (-1.53715f) /* = xyzRy_32f */;
            const float labRz_32f = (-0.542782f)/* = xyzRz_32f * 1.088754 */;

            const float labGx_32f = (-0.921235f)/* = xyzGx_32f * 0.950456 */;
            const float labGy_32f = 1.875991f   /* = xyzGy_32f */ ;
            const float labGz_32f = 0.04524426f /* = xyzGz_32f * 1.088754 */;

            const float labBx_32f = 0.0528909755f /* = xyzBx_32f * 0.950456 */;
            const float labBy_32f = (-0.204043f)  /* = xyzBy_32f */;
            const float labBz_32f = 1.15115158f   /* = xyzBz_32f * 1.088754 */;

            const float labT_32f = 0.008856f;

            const int lab_shift = 10;

            const float labLScale2_32f = 903.3f;

            const int labXr = (int)((labXr_32f) * (1 << (lab_shift)) + 0.5);
            const int labXg = (int)((labXg_32f) * (1 << (lab_shift)) + 0.5);
            const int labXb = (int)((labXb_32f) * (1 << (lab_shift)) + 0.5);

            const int labYr = (int)((labYr_32f) * (1 << (lab_shift)) + 0.5);
            const int labYg = (int)((labYg_32f) * (1 << (lab_shift)) + 0.5);
            const int labYb = (int)((labYb_32f) * (1 << (lab_shift)) + 0.5);

            const int labZr = (int)((labZr_32f) * (1 << (lab_shift)) + 0.5);
            const int labZg = (int)((labZg_32f) * (1 << (lab_shift)) + 0.5);
            const int labZb = (int)((labZb_32f) * (1 << (lab_shift)) + 0.5);

            const float labLScale_32f = 116.0f;
            const float labLShift_32f = 16.0f;

            const int labSmallScale = (int)((31.27 /* labSmallScale_32f*(1<<lab_shift)/255 */ ) * (1 << (lab_shift)) + 0.5);

            const int labSmallShift = (int)((141.24138 /* labSmallScale_32f*(1<<lab) */ ) * (1 << (lab_shift)) + 0.5);

            const int labT = (int)((labT_32f * 255) * (1 << (lab_shift)) + 0.5);

            const int labLScale = (int)((295.8) * (1 << (lab_shift)) + 0.5);
            const int labLShift = (int)((41779.2) * (1 << (lab_shift)) + 0.5);
            const int labLScale2 = (int)((labLScale2_32f * 0.01) * (1 << (lab_shift)) + 0.5);

            public static unsafe void ToLab24(Rgb24* from, Lab24* to)
            {
                ToLab24(from, to, 1);
            }

            public static unsafe void ToLab24(Rgb24* from, Lab24* to, int length)
            {
                // 使用 OpenCV 中的算法实现          

                if (length < 1) return;

                Rgb24* end = from + length;

                int x, y, z;
                int l, a, b;
                bool flag;

                while (from != end)
                {
                    Byte red = from->Red;
                    Byte green = from->Green;
                    Byte blue = from->Blue;

                    x = blue * labXb + green * labXg + red * labXr;
                    y = blue * labYb + green * labYg + red * labYr;
                    z = blue * labZb + green * labZg + red * labZr;

                    flag = x > labT;

                    x = (((x) + (1 << ((lab_shift) - 1))) >> (lab_shift));

                    if (flag)
                        x = icvLabCubeRootTab[x];
                    else
                        x = (((x * labSmallScale + labSmallShift) + (1 << ((lab_shift) - 1))) >> (lab_shift));

                    flag = z > labT;
                    z = (((z) + (1 << ((lab_shift) - 1))) >> (lab_shift));

                    if (flag == true)
                        z = icvLabCubeRootTab[z];
                    else
                        z = (((z * labSmallScale + labSmallShift) + (1 << ((lab_shift) - 1))) >> (lab_shift));

                    flag = y > labT;
                    y = (((y) + (1 << ((lab_shift) - 1))) >> (lab_shift));

                    if (flag == true)
                    {
                        y = icvLabCubeRootTab[y];
                        l = (((y * labLScale - labLShift) + (1 << ((2 * lab_shift) - 1))) >> (2 * lab_shift));
                    }
                    else
                    {
                        l = (((y * labLScale2) + (1 << ((lab_shift) - 1))) >> (lab_shift));
                        y = (((y * labSmallScale + labSmallShift) + (1 << ((lab_shift) - 1))) >> (lab_shift));
                    }

                    a = (((500 * (x - y)) + (1 << ((lab_shift) - 1))) >> (lab_shift)) + 129;
                    b = (((200 * (y - z)) + (1 << ((lab_shift) - 1))) >> (lab_shift)) + 128;

                    l = l > 255 ? 255 : l < 0 ? 0 : l;
                    a = a > 255 ? 255 : a < 0 ? 0 : a;
                    b = b > 255 ? 255 : b < 0 ? 0 : b;

                    to->L = (byte)l;
                    to->A = (byte)a;
                    to->B = (byte)b;

                    from++;
                    to++;
                }
            }

            public static unsafe void ToRgb24(Lab24* from, Rgb24* to)
            {
                ToRgb24(from, to, 1);
            }
            public static unsafe Rgb24 ToRgb24(Lab24 from)
            {
                IntPtr destination = Marshal.AllocHGlobal(12);
                IntPtr from1 = Marshal.AllocHGlobal(12);
                byte[] from1Arr = new byte[3];
                from1Arr[0] = from.L; from1Arr[1] = from.A; from1Arr[2] = from.B;
                Marshal.Copy(from1Arr, 0, from1, 3);

                ToRgb24((Lab24*)from1, (Rgb24*)destination, 1);
                Marshal.Copy(destination, from1Arr, 0, 3);

                return new Rgb24 { Red = from1Arr[0], Green = from1Arr[1], Blue = from1Arr[2] };
            }

            public static unsafe void ToRgb24(Lab24* from, Rgb24* to, int length)
            {
                if (length < 1) return;

                // 使用 OpenCV 中的算法实现          
                const float coeff0 = 0.39215686274509809f;
                const float coeff1 = 0.0f;
                const float coeff2 = 1.0f;
                const float coeff3 = (-128.0f);
                const float coeff4 = 1.0f;
                const float coeff5 = (-128.0f);

                if (length < 1) return;

                Lab24* end = from + length;
                float x, y, z, l, a, b;
                int blue, green, red;

                while (from != end)
                {
                    l = from->L * coeff0 + coeff1;
                    a = from->A * coeff2 + coeff3;
                    b = from->B * coeff4 + coeff5;

                    l = (l + labLShift_32f) * (1.0f / labLScale_32f);
                    x = (l + a * 0.002f);
                    z = (l - b * 0.005f);

                    y = l * l * l;
                    x = x * x * x;
                    z = z * z * z;

                    blue = (int)((x * labBx_32f + y * labBy_32f + z * labBz_32f) * 255 + 0.5);
                    green = (int)((x * labGx_32f + y * labGy_32f + z * labGz_32f) * 255 + 0.5);
                    red = (int)((x * labRx_32f + y * labRy_32f + z * labRz_32f) * 255 + 0.5);

                    red = red < 0 ? 0 : red > 255 ? 255 : red;
                    green = green < 0 ? 0 : green > 255 ? 255 : green;
                    blue = blue < 0 ? 0 : blue > 255 ? 255 : blue;

                    to->Red = (byte)red;
                    to->Green = (byte)green;
                    to->Blue = (byte)blue;

                    from++;
                    to++;
                }
            }
        }
        /// <summary>  
        /// 类      名：ColorHelper  
        /// 功      能：提供从RGB到HSV/HSL色彩空间的相互转换  
        /// 日      期：2015-02-08  
        /// 修      改：2015-03-20  
        /// 作      者：ls9512  
        /// </summary>  
        public static class ColorHelper
        {
            /// <summary>  
            /// 类      名：ColorHSL  
            /// 功      能：H 色相 \ S 饱和度(纯度) \ L 亮度 颜色模型   
            /// 日      期：2015-02-08  
            /// 修      改：2015-03-20  
            /// 作      者：ls9512  
            /// </summary>  
            public class ColorHSL
            {
                public ColorHSL(int h, int s, int l)
                {
                    this._h = h;
                    this._s = s;
                    this._l = l;
                }

                private int _h;
                private int _s;
                private int _l;

                /// <summary>  
                /// 色相  
                /// </summary>  
                public int H
                {
                    get { return this._h; }
                    set
                    {
                        this._h = value;
                        this._h = this._h > 360 ? 360 : this._h;
                        this._h = this._h < 0 ? 0 : this._h;
                    }
                }

                /// <summary>  
                /// 饱和度(纯度)  
                /// </summary>  
                public int S
                {
                    get { return this._s; }
                    set
                    {
                        this._s = value;
                        this._s = this._s > 255 ? 255 : this._s;
                        this._s = this._s < 0 ? 0 : this._s;
                    }
                }

                /// <summary>  
                /// 亮度 
                /// </summary>  
                public int L
                {
                    get { return this._l; }
                    set
                    {
                        this._l = value;
                        this._l = this._l > 255 ? 255 : this._l;
                        this._l = this._l < 0 ? 0 : this._l;
                    }
                }
            }

            /// <summary>  
            /// 类      名：ColorHSV  
            /// 功      能：H 色相 \ S 饱和度(纯度) \ V 明度 颜色模型   
            /// 日      期：2015-01-22  
            /// 修      改：2015-03-20  
            /// 作      者：ls9512  
            /// </summary>  
            public class ColorHSV
            {
                /// <summary>  
                /// 构造方法  
                /// </summary>  
                /// <param name="h"></param>  
                /// <param name="s"></param>  
                /// <param name="v"></param>  
                public ColorHSV(int h, int s, int v)
                {
                    this._h = h;
                    this._s = s;
                    this._v = v;
                }

                private int _h;
                private int _s;
                private int _v;

                /// <summary>  
                /// 色相  
                /// </summary>  
                public int H
                {
                    get { return this._h; }
                    set
                    {
                        this._h = value;
                        this._h = this._h > 360 ? 360 : this._h;
                        this._h = this._h < 0 ? 0 : this._h;
                    }
                }

                /// <summary>  
                /// 饱和度(纯度)  
                /// </summary>  
                public int S
                {
                    get { return this._s; }
                    set
                    {
                        this._s = value;
                        this._s = this._s > 255 ? 255 : this._s;
                        this._s = this._s < 0 ? 0 : this._s;
                    }
                }

                /// <summary>  
                /// 明度  
                /// </summary>  
                public int V
                {
                    get { return this._v; }
                    set
                    {
                        this._v = value;
                        this._v = this._v > 255 ? 255 : this._v;
                        this._v = this._v < 0 ? 0 : this._v;
                    }
                }
            }

            /// <summary>  
            /// 类      名：ColorRGB  
            /// 功      能：R 红色 \ G 绿色 \ B 蓝色 颜色模型  
            ///                 所有颜色模型的基类，RGB是用于输出到屏幕的颜色模式，所以所有模型都将转换成RGB输出  
            /// 日      期：2015-01-22  
            /// 修      改：2015-03-20  
            /// 作      者：ls9512  
            /// </summary>  
            public class ColorRGB
            {
                /// <summary>  
                /// 构造方法  
                /// </summary>  
                /// <param name="r"></param>  
                /// <param name="g"></param>  
                /// <param name="b"></param>  
                public ColorRGB(int r, int g, int b)
                {
                    this._r = r;
                    this._g = g;
                    this._b = b;
                }

                private int _r;
                private int _g;
                private int _b;

                /// <summary>  
                /// 红色  
                /// </summary>  
                public int R
                {
                    get { return this._r; }
                    set
                    {
                        this._r = value;
                        this._r = this._r > 255 ? 255 : this._r;
                        this._r = this._r < 0 ? 0 : this._r;
                    }
                }

                /// <summary>  
                /// 绿色  
                /// </summary>  
                public int G
                {
                    get { return this._g; }
                    set
                    {
                        this._g = value;
                        this._g = this._g > 255 ? 255 : this._g;
                        this._g = this._g < 0 ? 0 : this._g;
                    }
                }

                /// <summary>  
                /// 蓝色  
                /// </summary>  
                public int B
                {
                    get { return this._b; }
                    set
                    {
                        this._b = value;
                        this._b = this._b > 255 ? 255 : this._b;
                        this._b = this._b < 0 ? 0 : this._b;
                    }
                }

                /// <summary>  
                /// 获取实际颜色  
                /// </summary>  
                /// <returns></returns>  
                public Color GetColor()
                {
                    return Color.FromArgb(this._r, this._g, this._b);
                }
            }

            /// <summary>  
            /// RGB转换HSV  
            /// </summary>  
            /// <param name="rgb"></param>  
            /// <returns></returns>  
            public static ColorHSV RgbToHsv(ColorRGB rgb)
            {
                float min, max, tmp, H, S, V;
                float R = rgb.R * 1.0f / 255, G = rgb.G * 1.0f / 255, B = rgb.B * 1.0f / 255;
                tmp = Math.Min(R, G);
                min = Math.Min(tmp, B);
                tmp = Math.Max(R, G);
                max = Math.Max(tmp, B);
                // H  
                H = 0;
                if (max == min)
                {
                    H = 0;
                }
                else if (max == R && G > B)
                {
                    H = 60 * (G - B) * 1.0f / (max - min) + 0;
                }
                else if (max == R && G < B)
                {
                    H = 60 * (G - B) * 1.0f / (max - min) + 360;
                }
                else if (max == G)
                {
                    H = H = 60 * (B - R) * 1.0f / (max - min) + 120;
                }
                else if (max == B)
                {
                    H = H = 60 * (R - G) * 1.0f / (max - min) + 240;
                }
                // S  
                if (max == 0)
                {
                    S = 0;
                }
                else
                {
                    S = (max - min) * 1.0f / max;
                }
                // V  
                V = max;
                return new ColorHSV((int)H, (int)(S * 255), (int)(V * 255));
            }

            /// <summary>  
            /// HSV转换RGB  
            /// </summary>  
            /// <param name="hsv"></param>  
            /// <returns></returns>  
            public static ColorRGB HsvToRgb(ColorHSV hsv)
            {
                if (hsv.H == 360) hsv.H = 359; // 360为全黑，原因不明  
                float R = 0f, G = 0f, B = 0f;
                if (hsv.S == 0)
                {
                    return new ColorRGB(hsv.V, hsv.V, hsv.V);
                }
                float S = hsv.S * 1.0f / 255, V = hsv.V * 1.0f / 255;
                int H1 = (int)(hsv.H * 1.0f / 60), H = hsv.H;
                float F = H * 1.0f / 60 - H1;
                float P = V * (1.0f - S);
                float Q = V * (1.0f - F * S);
                float T = V * (1.0f - (1.0f - F) * S);
                switch (H1)
                {
                    case 0: R = V; G = T; B = P; break;
                    case 1: R = Q; G = V; B = P; break;
                    case 2: R = P; G = V; B = T; break;
                    case 3: R = P; G = Q; B = V; break;
                    case 4: R = T; G = P; B = V; break;
                    case 5: R = V; G = P; B = Q; break;
                }
                R = R * 255;
                G = G * 255;
                B = B * 255;
                while (R > 255) R -= 255;
                while (R < 0) R += 255;
                while (G > 255) G -= 255;
                while (G < 0) G += 255;
                while (B > 255) B -= 255;
                while (B < 0) B += 255;
                return new ColorRGB((int)R, (int)G, (int)B);
            }

            /// <summary>  
            /// RGB转换HSL  
            /// </summary>  
            /// <param name="rgb"></param>  
            /// <returns></returns>  
            public static ColorHSL RgbToHsl(ColorRGB rgb)
            {
                float min, max, tmp, H, S, L;
                float R = rgb.R * 1.0f / 255, G = rgb.G * 1.0f / 255, B = rgb.B * 1.0f / 255;
                tmp = Math.Min(R, G);
                min = Math.Min(tmp, B);
                tmp = Math.Max(R, G);
                max = Math.Max(tmp, B);
                // H  
                H = 0;
                if (max == min)
                {
                    H = 0;  // 此时H应为未定义，通常写为0  
                }
                else if (max == R && G > B)
                {
                    H = 60 * (G - B) * 1.0f / (max - min) + 0;
                }
                else if (max == R && G < B)
                {
                    H = 60 * (G - B) * 1.0f / (max - min) + 360;
                }
                else if (max == G)
                {
                    H = H = 60 * (B - R) * 1.0f / (max - min) + 120;
                }
                else if (max == B)
                {
                    H = H = 60 * (R - G) * 1.0f / (max - min) + 240;
                }
                // L   
                L = 0.5f * (max + min);
                // S  
                S = 0;
                if (L == 0 || max == min)
                {
                    S = 0;
                }
                else if (0 < L && L < 0.5)
                {
                    S = (max - min) / (L * 2);
                }
                else if (L > 0.5)
                {
                    S = (max - min) / (2 - 2 * L);
                }
                return new ColorHSL((int)H, (int)(S * 255), (int)(L * 255));
            }

            /// <summary>  
            /// HSL转换RGB  
            /// </summary>  
            /// <param name="hsl"></param>  
            /// <returns></returns>  
            public static ColorRGB HslToRgb(ColorHSL hsl)
            {
                double R = 0f, G = 0f, B = 0f;
                double S = hsl.S * 1.0f / 255, L = hsl.L * 1.0f / 255;
                double temp1, temp2, temp3;
                if (S == 0f) // 灰色  
                {
                    R = L;
                    G = L;
                    B = L;
                }
                else
                {
                    if (L < 0.5f)
                    {
                        temp2 = L * (1.0f + S);
                    }
                    else
                    {
                        temp2 = L + S - L * S;
                    }
                    temp1 = 2.0f * L - temp2;
                    float H = hsl.H * 1.0f / 360;
                    // R  
                    temp3 = H + 1.0f / 3.0f;
                    if (temp3 < 0) temp3 += 1.0f;
                    if (temp3 > 1) temp3 -= 1.0f;
                    if (6.0 * temp3 < 1) R = temp1 + (temp2 - temp1) * 6.0 * temp3;
                    else if (2.0 * temp3 < 1) R = temp2;
                    else if (3.0 * temp3 < 2) R = temp1 + (temp2 - temp1) * ((2.0 / 3.0) - temp3) * 6.0;
                    else R = temp1;
                    // G  
                    temp3 = H;
                    if (temp3 < 0) temp3 += 1.0f;
                    if (temp3 > 1) temp3 -= 1.0f;
                    if (6.0 * temp3 < 1) G = temp1 + (temp2 - temp1) * 6.0 * temp3;
                    else if (2.0 * temp3 < 1) G = temp2;
                    else if (3.0 * temp3 < 2) G = temp1 + (temp2 - temp1) * ((2.0 / 3.0) - temp3) * 6.0;
                    else G = temp1;
                    // B  
                    temp3 = H - 1.0f / 3.0f;
                    if (temp3 < 0) temp3 += 1.0f;
                    if (temp3 > 1) temp3 -= 1.0f;
                    if (6.0 * temp3 < 1) B = temp1 + (temp2 - temp1) * 6.0 * temp3;
                    else if (2.0 * temp3 < 1) B = temp2;
                    else if (3.0 * temp3 < 2) B = temp1 + (temp2 - temp1) * ((2.0 / 3.0) - temp3) * 6.0;
                    else B = temp1;
                }
                R = R * 255;
                G = G * 255;
                B = B * 255;
                return new ColorRGB((int)R, (int)G, (int)B);
            }
        }
        #endregion

        #region 基础操作
        /// <summary>
        /// 序列化图片并写入Bitmap指针，返回序列化后的颜色距离
        /// </summary>
        /// <param name="InputMap">输入颜色距离MAP</param>
        /// <param name="ByteWidth">一行字节宽度</param>
        /// <param name="Height">一列字节高度</param>
        /// <param name="p">目标bitmap指针</param>
        /// <param name="normrange">默认序列化单个像素颜色数</param>
        /// <returns></returns>
        public unsafe static double[] Normalize(double[] InputMap, int ByteWidth, int Height, byte* p, int normrange = 255)
        {
            double maxval = 0;
            double minval = double.MaxValue;
            int i = 0;
            double[] OutputMap = new double[ByteWidth * Height];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < ByteWidth; x++)
                {
                    if (maxval < InputMap[i]) maxval = InputMap[i];
                    if (minval > InputMap[i]) minval = InputMap[i];
                    i++;
                }
            }
            double range = maxval - minval;
            if (0 == range) range = 1;
            i = 0;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < ByteWidth; x++)
                {
                    OutputMap[i] = ((normrange * (InputMap[i] - minval)) / range);
                    p[i] = (byte)(OutputMap[i]);
                    i++;
                }
            }
            return OutputMap;
        }
        /// <summary>
        /// 序列化图片并写入Bitmap指针，返回序列化后的颜色距离
        /// </summary>
        /// <param name="InputMap">输入颜色距离MAP</param>
        /// <param name="ByteWidth">一行字节宽度</param>
        /// <param name="Height">一列字节高度</param>
        /// <param name="p">目标bitmap指针</param>
        /// <param name="normrange">默认序列化单个像素颜色数</param>
        /// <returns></returns>
        public unsafe static void Normalize(double[] InputMap, int ByteWidth, int Height, double maxval, double minval, byte* p, int normrange = 255)
        {
            int i = 0;
            double range = maxval - minval;
            if (0 == range) range = 1;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < ByteWidth; x++)
                {
                    p[i] = (byte)(((normrange * (InputMap[i] - minval)) / range));
                    i++;
                }
            }
        }

        /// <summary>
        /// 裁剪图片
        /// </summary>
        /// <param name="srcbit"></param>
        /// <param name="startx"></param>
        /// <param name="starty"></param>
        /// <param name="distwidth"></param>
        /// <param name="distheight"></param>
        /// <returns></returns>
        public static Bitmap CutImage(Bitmap srcbit, double startx, double starty, double distwidth, double distheight)
        {
            if (srcbit == null)
            {
                return null;
            }
            int w = srcbit.Width;
            int h = srcbit.Height;
            if (startx >= w || starty >= h)
            {
                return null;
            }
            if (startx + distwidth > w)
            {
                distwidth = w - startx;
            }
            if (starty + distheight > h)
            {
                distheight = h - starty;
            }
            try
            {
                Bitmap bmpOut = new Bitmap((int)distwidth, (int)distheight, PixelFormat.Format24bppRgb);
                Graphics g = Graphics.FromImage(bmpOut);
                g.DrawImage(srcbit, new Rectangle(0, 0, (int)distwidth, (int)distheight), new Rectangle((int)startx, (int)starty, (int)distwidth, (int)distheight), GraphicsUnit.Pixel);
                g.Dispose();
                return bmpOut;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public static Bitmap CutImage(Bitmap srcbit, Rectangle rectangle)
        {
            return CutImage(srcbit, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        }
        ///<summary> 
        /// 生成缩略图方法
        ///</summary> 
        ///<param name="originalImagePath">源图路径（物理路径）</param> 
        ///<param name="thumbnailPath">缩略图路径（物理路径）</param> 
        ///<param name="width">缩略图宽度px/%</param> 
        ///<param name="height">缩略图高度px/%</param> 
        ///<param name="mode">生成缩略图的方式W,H,CUT,SIZE(kb),ATUO（当指定为ATUO且源图宽高皆小于目标图时不会生成新图）</param>
        ///<param name="type">生成图片的类型“jpg”or“png”</param>
        ///<param name="size">制订生成图片文件的最大大小，不改变图片尺寸</param>
        ///<returns>生成失败、生成结果不满足size参数为flase否则true</returns>
        ///<remarks>尝试20%-75%的压缩级别</remarks>
        static public bool MakeThumbnail(string originalPath, string SavePath, double width, double height, string mode, string type, int size = int.MaxValue)
        {
            System.Drawing.Image originalImage;
            try
            {
                byte[] originalBuf = File.ReadAllBytes(originalPath);
                MemoryStream original_ms = new MemoryStream(originalBuf, 0, originalBuf.Length);
                originalImage = new Bitmap(original_ms);
                original_ms.Dispose();
                if (width < 0 || height < 0) return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            System.Drawing.Imaging.ImageFormat imgtype = System.Drawing.Imaging.ImageFormat.Jpeg;
            if (type == "png" || type == ".png")
            {
                imgtype = System.Drawing.Imaging.ImageFormat.Png;
            }
            if (type == "jpg" || type == ".jpg")
            {
                imgtype = System.Drawing.Imaging.ImageFormat.Jpeg;
            }

            int towidth = (int)(width <= 1 ? originalImage.Width * width : width);
            int toheight = (int)(height <= 1 ? originalImage.Height * height : height);
            width = towidth; height = toheight;
            int x = 0;
            int y = 0;
            int ow = originalImage.Width;
            int oh = originalImage.Height;
            switch (mode)
            {
                case "ATUO":
                    if (width <= originalImage.Width)
                        toheight = (int)(originalImage.Height * width / originalImage.Width);
                    else if (height <= originalImage.Height)
                        toheight = (int)(originalImage.Height * width / originalImage.Width);
                    else
                        originalImage.Save(SavePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                    break;
                case "HW"://指定高宽缩放（可能变形）                 
                    break;
                case "W"://指定宽，高按比例                     
                    toheight = (int)(originalImage.Height * width / originalImage.Width);
                    break;
                case "H"://指定高，宽按比例 
                    towidth = (int)(originalImage.Width * height / originalImage.Height);
                    break;
                case "Cut"://指定高宽裁减（不变形）                 
                    if ((double)originalImage.Width / (double)originalImage.Height > (double)towidth / (double)toheight)
                    {
                        oh = originalImage.Height;
                        ow = originalImage.Height * towidth / toheight;
                        y = 0;
                        x = (originalImage.Width - ow) / 2;
                    }
                    else
                    {
                        ow = originalImage.Width;
                        oh = (int)(originalImage.Width * height / towidth);
                        x = 0;
                        y = (originalImage.Height - oh) / 2;
                    }
                    break;
                default:
                    break;
            }
            //新建一个bmp图片 
            System.Drawing.Image bitmap_Dist = new System.Drawing.Bitmap(towidth, toheight);
            //新建一个画板 
            Graphics g = System.Drawing.Graphics.FromImage(bitmap_Dist);
            //设置高质量插值法 
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            //设置高质量,低速度呈现平滑程度 
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            //清空画布并以透明背景色填充 
            g.Clear(System.Drawing.Color.Transparent);
            //在指定位置并且按指定大小绘制原图片的指定部分 
            g.DrawImage(originalImage, new System.Drawing.Rectangle(0, 0, towidth, toheight), new System.Drawing.Rectangle(x, y, ow, oh), GraphicsUnit.Pixel);
            try
            {
                //保存缩略图到流
                MemoryStream distImage_ms = new MemoryStream();
                distImage_ms.Seek(0, SeekOrigin.Begin);
                bitmap_Dist.Save(distImage_ms, imgtype);
                bool result = false;
                int q = 75;//压缩质量
                if (distImage_ms.Length / 1024 < size) { result = true; }
                else
                {//不满足size参数则尝试更差的质量
                    int i = 0;

                    for (i = 1; i < 4; i++)
                    {
                        if (distImage_ms.Length / 1024 > size)
                        {
                            q = 75 - 15 * i;
                            distImage_ms.Dispose(); distImage_ms = new MemoryStream(); distImage_ms.Seek(0, SeekOrigin.Begin);
                            SaveImageForSpecifiedQuality(bitmap_Dist, distImage_ms, q, imgtype);
                            continue;
                        }
                        if (distImage_ms.Length / 1024 < size)
                        {
                            break;
                        }
                    }
                    if (i < 4)
                    {
                        result = true;
                    }
                    result = false;
                }
                //  if (result)
                //  { 
                SaveImageForSpecifiedQuality(bitmap_Dist, SavePath, q, imgtype);
                //  }
                distImage_ms.Dispose();
                return result;
            }
            catch (System.Exception)
            {
                return false;
            }
            finally
            {

                originalImage.Dispose();
                bitmap_Dist.Dispose();
                g.Dispose();
            }
        }
        static public bool MakeThumbnail(string originalPath, Stream SaveStream, double width, double height, string mode, string type, int size = int.MaxValue)
        {
            if (SaveStream == null) SaveStream = new MemoryStream();
            System.Drawing.Image originalImage;
            try
            {
                byte[] originalBuf = File.ReadAllBytes(originalPath);
                MemoryStream original_ms = new MemoryStream(originalBuf, 0, originalBuf.Length);
                originalImage = new Bitmap(original_ms);
                original_ms.Dispose();
                if (width < 0 || height < 0) return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            System.Drawing.Imaging.ImageFormat imgtype = System.Drawing.Imaging.ImageFormat.Jpeg;
            if (type == "png" || type == ".png")
            {
                imgtype = System.Drawing.Imaging.ImageFormat.Png;
            }
            if (type == "jpg" || type == ".jpg")
            {
                imgtype = System.Drawing.Imaging.ImageFormat.Jpeg;
            }

            int towidth = (int)(width <= 1 ? originalImage.Width * width : width);
            int toheight = (int)(height <= 1 ? originalImage.Height * height : height);
            width = towidth; height = toheight;
            int x = 0;
            int y = 0;
            int ow = originalImage.Width;
            int oh = originalImage.Height;
            switch (mode)
            {
                case "ATUO":
                    if (width <= originalImage.Width)
                        toheight = (int)(originalImage.Height * width / originalImage.Width);
                    else if (height <= originalImage.Height)
                        toheight = (int)(originalImage.Height * width / originalImage.Width);
                    else
                        originalImage.Save(SaveStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    break;
                case "HW"://指定高宽缩放（可能变形）                 
                    break;
                case "W"://指定宽，高按比例                     
                    toheight = (int)(originalImage.Height * width / originalImage.Width);
                    break;
                case "H"://指定高，宽按比例 
                    towidth = (int)(originalImage.Width * height / originalImage.Height);
                    break;
                case "Cut"://指定高宽裁减（不变形）                 
                    if ((double)originalImage.Width / (double)originalImage.Height > (double)towidth / (double)toheight)
                    {
                        oh = originalImage.Height;
                        ow = originalImage.Height * towidth / toheight;
                        y = 0;
                        x = (originalImage.Width - ow) / 2;
                    }
                    else
                    {
                        ow = originalImage.Width;
                        oh = (int)(originalImage.Width * height / towidth);
                        x = 0;
                        y = (originalImage.Height - oh) / 2;
                    }
                    break;
                default:
                    break;
            }
            //新建一个bmp图片 
            System.Drawing.Image bitmap_Dist = new System.Drawing.Bitmap(towidth, toheight);
            //新建一个画板 
            Graphics g = System.Drawing.Graphics.FromImage(bitmap_Dist);
            //设置高质量插值法 
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            //设置高质量,低速度呈现平滑程度 
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            //清空画布并以透明背景色填充 
            g.Clear(System.Drawing.Color.Transparent);
            //在指定位置并且按指定大小绘制原图片的指定部分 
            g.DrawImage(originalImage, new System.Drawing.Rectangle(0, 0, towidth, toheight), new System.Drawing.Rectangle(x, y, ow, oh), GraphicsUnit.Pixel);
            try
            {
                //保存缩略图到流
                MemoryStream distImage_ms = new MemoryStream();
                distImage_ms.Seek(0, SeekOrigin.Begin);
                bitmap_Dist.Save(distImage_ms, imgtype);
                bool result = false;
                int q = 75;//压缩质量
                if (distImage_ms.Length / 1024 < size)
                {
                    distImage_ms.Seek(0, SeekOrigin.Begin);
                    distImage_ms.CopyTo(SaveStream);
                    result = true;
                }
                else
                {//不满足size参数则尝试更差的质量
                    int i = 0;

                    for (i = 1; i < 4; i++)
                    {
                        if (distImage_ms.Length / 1024 > size)
                        {
                            q = 75 - 15 * i;
                            distImage_ms.Dispose(); distImage_ms = new MemoryStream(); distImage_ms.Seek(0, SeekOrigin.Begin);
                            SaveImageForSpecifiedQuality(bitmap_Dist, distImage_ms, q, imgtype);
                            continue;
                        }
                        if (distImage_ms.Length / 1024 < size)
                        {
                            break;
                        }
                    }
                    if (i < 4)
                    {
                        result = true;
                    }
                    result = false;
                    SaveImageForSpecifiedQuality(bitmap_Dist, SaveStream, q, imgtype);
                }
                distImage_ms.Dispose();
                return result;
            }
            catch (System.Exception)
            {
                return false;
            }
            finally
            {

                originalImage.Dispose();
                bitmap_Dist.Dispose();
                g.Dispose();
            }
        }
        static public bool MakeThumbnail(MemoryStream original_ms, Stream SaveStream, double width, double height, string mode, string type, int size = int.MaxValue)
        {
            if (SaveStream == null) SaveStream = new MemoryStream();
            System.Drawing.Image originalImage;
            try
            {
                originalImage = new Bitmap(original_ms);
                if (width < 0 || height < 0) return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            System.Drawing.Imaging.ImageFormat imgtype = System.Drawing.Imaging.ImageFormat.Jpeg;
            if (type == "png" || type == ".png")
            {
                imgtype = System.Drawing.Imaging.ImageFormat.Png;
            }
            if (type == "jpg" || type == ".jpg")
            {
                imgtype = System.Drawing.Imaging.ImageFormat.Jpeg;
            }

            int towidth = (int)(width <= 1 ? originalImage.Width * width : width);
            int toheight = (int)(height <= 1 ? originalImage.Height * height : height);
            width = towidth; height = toheight;
            int x = 0;
            int y = 0;
            int ow = originalImage.Width;
            int oh = originalImage.Height;
            switch (mode)
            {
                case "ATUO":
                    if (width <= originalImage.Width)
                        toheight = (int)(originalImage.Height * width / originalImage.Width);
                    else if (height <= originalImage.Height)
                        toheight = (int)(originalImage.Height * width / originalImage.Width);
                    else
                        originalImage.Save(SaveStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    break;
                case "HW"://指定高宽缩放（可能变形）                 
                    break;
                case "W"://指定宽，高按比例                     
                    toheight = (int)(originalImage.Height * width / originalImage.Width);
                    break;
                case "H"://指定高，宽按比例 
                    towidth = (int)(originalImage.Width * height / originalImage.Height);
                    break;
                case "Cut"://指定高宽裁减（不变形）                 
                    if ((double)originalImage.Width / (double)originalImage.Height > (double)towidth / (double)toheight)
                    {
                        oh = originalImage.Height;
                        ow = originalImage.Height * towidth / toheight;
                        y = 0;
                        x = (originalImage.Width - ow) / 2;
                    }
                    else
                    {
                        ow = originalImage.Width;
                        oh = (int)(originalImage.Width * height / towidth);
                        x = 0;
                        y = (originalImage.Height - oh) / 2;
                    }
                    break;
                default:
                    break;
            }
            //新建一个bmp图片 
            System.Drawing.Image bitmap_Dist = new System.Drawing.Bitmap(towidth, toheight);
            //新建一个画板 
            Graphics g = System.Drawing.Graphics.FromImage(bitmap_Dist);
            //设置高质量插值法 
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            //设置高质量,低速度呈现平滑程度 
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            //清空画布并以透明背景色填充 
            g.Clear(System.Drawing.Color.Transparent);
            //在指定位置并且按指定大小绘制原图片的指定部分 
            g.DrawImage(originalImage, new System.Drawing.Rectangle(0, 0, towidth, toheight), new System.Drawing.Rectangle(x, y, ow, oh), GraphicsUnit.Pixel);
            try
            {
                //保存缩略图到流
                MemoryStream distImage_ms = new MemoryStream();
                distImage_ms.Seek(0, SeekOrigin.Begin);
                bitmap_Dist.Save(distImage_ms, imgtype);
                bool result = false;
                int q = 75;//压缩质量
                if (distImage_ms.Length / 1024 < size)
                {
                    distImage_ms.Seek(0, SeekOrigin.Begin);
                    distImage_ms.CopyTo(SaveStream);
                    result = true;
                }
                else
                {//不满足size参数则尝试更差的质量
                    int i = 0;

                    for (i = 1; i < 4; i++)
                    {
                        if (distImage_ms.Length / 1024 > size)
                        {
                            q = 75 - 15 * i;
                            distImage_ms.Dispose(); distImage_ms = new MemoryStream(); distImage_ms.Seek(0, SeekOrigin.Begin);
                            SaveImageForSpecifiedQuality(bitmap_Dist, distImage_ms, q, imgtype);
                            continue;
                        }
                        if (distImage_ms.Length / 1024 < size)
                        {
                            break;
                        }
                    }
                    if (i < 4)
                    {
                        result = true;
                    }
                    result = false;
                    SaveImageForSpecifiedQuality(bitmap_Dist, SaveStream, q, imgtype);
                }
                distImage_ms.Dispose();
                return result;
            }
            catch (System.Exception)
            {
                return false;
            }
            finally
            {

                originalImage.Dispose();
                bitmap_Dist.Dispose();
                g.Dispose();
            }
        }
        static public bool MakeThumbnail(Bitmap originalImage, Stream SaveStream, double width, double height, string mode, string type, int size = int.MaxValue)
        {
            System.Drawing.Imaging.ImageFormat imgtype = System.Drawing.Imaging.ImageFormat.Jpeg;
            if (type == "png" || type == ".png")
            {
                imgtype = System.Drawing.Imaging.ImageFormat.Png;
            }
            if (type == "jpg" || type == ".jpg")
            {
                imgtype = System.Drawing.Imaging.ImageFormat.Jpeg;
            }

            int towidth = (int)(width <= 1 ? originalImage.Width * width : width);
            int toheight = (int)(height <= 1 ? originalImage.Height * height : height);
            width = towidth; height = toheight;
            int x = 0;
            int y = 0;
            int ow = originalImage.Width;
            int oh = originalImage.Height;
            switch (mode)
            {
                case "ATUO":
                    if (width <= originalImage.Width)
                        toheight = (int)(originalImage.Height * width / originalImage.Width);
                    else if (height <= originalImage.Height)
                        toheight = (int)(originalImage.Height * width / originalImage.Width);
                    else
                        originalImage.Save(SaveStream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    break;
                case "HW"://指定高宽缩放（可能变形）                 
                    break;
                case "W"://指定宽，高按比例                     
                    toheight = (int)(originalImage.Height * width / originalImage.Width);
                    break;
                case "H"://指定高，宽按比例 
                    towidth = (int)(originalImage.Width * height / originalImage.Height);
                    break;
                case "Cut"://指定高宽裁减（不变形）                 
                    if ((double)originalImage.Width / (double)originalImage.Height > (double)towidth / (double)toheight)
                    {
                        oh = originalImage.Height;
                        ow = originalImage.Height * towidth / toheight;
                        y = 0;
                        x = (originalImage.Width - ow) / 2;
                    }
                    else
                    {
                        ow = originalImage.Width;
                        oh = (int)(originalImage.Width * height / towidth);
                        x = 0;
                        y = (originalImage.Height - oh) / 2;
                    }
                    break;
                default:
                    break;
            }
            //新建一个bmp图片 
            System.Drawing.Image bitmap_Dist = new System.Drawing.Bitmap(towidth, toheight);
            //新建一个画板 
            Graphics g = System.Drawing.Graphics.FromImage(bitmap_Dist);
            //设置高质量插值法 
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            //设置高质量,低速度呈现平滑程度 
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            //清空画布并以透明背景色填充 
            g.Clear(System.Drawing.Color.Transparent);
            //在指定位置并且按指定大小绘制原图片的指定部分 
            g.DrawImage(originalImage, new System.Drawing.Rectangle(0, 0, towidth, toheight), new System.Drawing.Rectangle(x, y, ow, oh), GraphicsUnit.Pixel);
            try
            {
                //保存缩略图到流
                MemoryStream distImage_ms = new MemoryStream();
                distImage_ms.Seek(0, SeekOrigin.Begin);
                bitmap_Dist.Save(distImage_ms, imgtype);
                bool result = false;
                int q = 75;//压缩质量
                if (distImage_ms.Length / 1024 < size)
                {
                    distImage_ms.Seek(0, SeekOrigin.Begin);
                    distImage_ms.CopyTo(SaveStream);
                    result = true;
                }
                else
                {//不满足size参数则尝试更差的质量
                    int i = 0;

                    for (i = 1; i < 4; i++)
                    {
                        if (distImage_ms.Length / 1024 > size)
                        {
                            q = 75 - 15 * i;
                            distImage_ms.Dispose(); distImage_ms = new MemoryStream(); distImage_ms.Seek(0, SeekOrigin.Begin);
                            SaveImageForSpecifiedQuality(bitmap_Dist, distImage_ms, q, imgtype);
                            continue;
                        }
                        if (distImage_ms.Length / 1024 < size)
                        {
                            break;
                        }
                    }
                    if (i < 4)
                    {
                        result = true;
                    }
                    result = false;
                    SaveImageForSpecifiedQuality(bitmap_Dist, SaveStream, q, imgtype);
                }
                distImage_ms.Dispose();
                return result;
            }
            catch (System.Exception)
            {
                return false;
            }
            finally
            {
                bitmap_Dist.Dispose();
                g.Dispose();
            }
        }
        /// <summary>
        /// 按指定的压缩质量及格式保存图片（微软的Image.Save方法保存到图片压缩质量为75)
        /// </summary>
        /// <param name="sourceImage">要保存的图片的Image对象</param>
        /// <param name="savePath">图片要保存的绝对路径</param>
        /// <param name="imageQualityValue">图片要保存的压缩质量，该参数的值为1至100的整数，数值越大，保存质量越好</param>
        /// <returns>保存成功，返回true；反之，返回false</returns>
        public static bool SaveImageForSpecifiedQuality(System.Drawing.Image sourceImage, Stream saveStream, int imageQualityValue, System.Drawing.Imaging.ImageFormat imgtype)
        {
            //以下代码为保存图片时，设置压缩质量
            EncoderParameters encoderParameters = new EncoderParameters(1);
            EncoderParameter encoderParameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, imageQualityValue);
            encoderParameters.Param[0] = encoderParameter;
            try
            {
                ImageCodecInfo[] ImageCodecInfoArray = ImageCodecInfo.GetImageEncoders();
                ImageCodecInfo imageCodecInfo = null;
                for (int i = 0; i < ImageCodecInfoArray.Length; i++)
                {
                    if (ImageCodecInfoArray[i].FormatID == imgtype.Guid)
                    {
                        imageCodecInfo = ImageCodecInfoArray[i];
                        break;
                    }
                }
                sourceImage.Save(saveStream, imageCodecInfo, encoderParameters);
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// 按指定的压缩质量及格式保存图片（微软的Image.Save方法保存到图片压缩质量为75)
        /// </summary>
        /// <param name="sourceImage">要保存的图片的Image对象</param>
        /// <param name="savePath">图片要保存的绝对路径</param>
        /// <param name="imageQualityValue">图片要保存的压缩质量，该参数的值为1至100的整数，数值越大，保存质量越好</param>
        /// <returns>保存成功，返回true；反之，返回false</returns>
        public static bool SaveImageForSpecifiedQuality(System.Drawing.Image sourceImage, string savePath, int imageQualityValue, System.Drawing.Imaging.ImageFormat imgtype)
        {
            // 以下代码为保存图片时，设置压缩质量  
            EncoderParameters encoderParameters = new EncoderParameters(1);
            EncoderParameter encoderParameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, imageQualityValue);
            encoderParameters.Param[0] = encoderParameter;
            try
            {
                ImageCodecInfo[] ImageCodecInfoArray = ImageCodecInfo.GetImageEncoders();
                ImageCodecInfo imageCodecInfo = null;
                for (int i = 0; i < ImageCodecInfoArray.Length; i++)
                {
                    if (ImageCodecInfoArray[i].FormatID == imgtype.Guid)
                    {
                        imageCodecInfo = ImageCodecInfoArray[i];
                        break;
                    }
                }
                sourceImage.Save(savePath, imageCodecInfo, encoderParameters);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static bool IsIdentical(Bitmap srcBitmap, Bitmap dstBitmap)
        {
            if (srcBitmap.Height != dstBitmap.Height) return false;
            if (srcBitmap.Width != dstBitmap.Width) return false;
            int width = srcBitmap.Width, height = srcBitmap.Height;
            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData srcBmData = srcBitmap.LockBits(rect,
                      ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            BitmapData dstBmData = dstBitmap.LockBits(rect,
                      ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            IntPtr srcScan = srcBmData.Scan0;
            IntPtr dstScan = dstBmData.Scan0;
            unsafe
            {
                double* srcP = (double*)(void*)srcScan;
                byte* dstP = (byte*)(void*)dstScan;
                int CurIndex = 0, srcStride = srcBmData.Stride, dstStride = dstBmData.Stride, X, Y;
                for (Y = 0; Y < height; Y++)
                {
                    CurIndex = Y * dstStride;
                    for (X = 0; X < width; X++)                                        //    计算像素的显著性
                    {
                        if (srcP[CurIndex] != dstP[CurIndex])
                        {
                            srcBitmap.UnlockBits(srcBmData);
                            dstBitmap.UnlockBits(dstBmData);
                            return false;
                        }
                        CurIndex++;
                    }
                }
            }
            srcBitmap.UnlockBits(srcBmData);
            dstBitmap.UnlockBits(dstBmData);
            return true;
        }

            #endregion

        #region 应用操作
        /// <summary>
            /// 高斯模糊（kernel fixed），该函数可以考虑优化
            /// </summary>
            /// <param name="InputLab">Lab数组</param>
            /// <param name="ByteWidth">一行字节数</param>
            /// <param name="Height">行数</param>
            /// <returns></returns>
        public static double[] GaussianSmooth(double[] InputLab, int Width, int ByteWidth, int Height)
        {

            //kernel可以是[1 2 1]or[1 4 6 4 1],此代码是[1 4 6 4 1]
            double[] kernel = { 1.0, 4.0, 6.0, 4.0, 1.0 };
            int center = kernel.Count() / 2;

            double[] temp = new double[ByteWidth * Height];
            double[] OutputLab = new double[ByteWidth * Height];
            unsafe
            {
                //double L, A, B;
                double kernelNum = 0, vlaue = 0;
                fixed (double* inputImg = InputLab, tempim = temp, smoothImg = OutputLab)
                {
                    for (int y = 0; y < Height; y++)
                    {//X方向模糊
                        int index = y * ByteWidth;
                        for (int x = 0; x < Width; x++)
                        {
                            if (x - center >= 0 && x + center < Width)
                            {
                                for (int color = 0; color < 3; color++)
                                {//RGB
                                    for (int c = 0; c < kernel.Count(); c++)
                                    {
                                        vlaue += inputImg[index + color + c * 3] * kernel[c];
                                        kernelNum += kernel[c];
                                    }
                                    tempim[index + color] = vlaue / kernelNum;
                                    vlaue = 0; kernelNum = 0;
                                }
                            }
                            else
                            {
                                tempim[index] = inputImg[index];
                                tempim[index + 1] = inputImg[index + 1];
                                tempim[index + 2] = inputImg[index + 2];
                            }
                            index += 3;
                        }
                    }

                    for (int y = 0; y < Height; y++)
                    {//Y方向模糊
                        int index = y * ByteWidth;
                        for (int x = 0; x < Width; x++)
                        {
                            //if (y - center >= 0 && y + center < Height)
                            if (y - center >= 0 && y + kernel.Count() < Height)//临时修改
                            {
                                for (int color = 0; color < 3; color++)
                                {//此处有问题，大图会溢出，(原因：这个模糊是向XY偏移的模糊即模糊最终写入核心的-2位置)
                                    for (int c = 0; c < kernel.Count(); c++)
                                    {
                                        //vlaue += tempim[c * ByteWidth + index + color] * kernel[c];
                                        vlaue += tempim[(c + y) * ByteWidth + 3 * x + color] * kernel[c];

                                        kernelNum += kernel[c];
                                    }
                                    smoothImg[index + color] = vlaue / kernelNum;
                                    vlaue = 0; kernelNum = 0;
                                }
                            }
                            else
                            {
                                smoothImg[index] = tempim[index];
                                smoothImg[index + 1] = tempim[index + 1];
                                smoothImg[index + 2] = tempim[index + 2];
                            }
                            index += 3;
                        }
                    }
                    return OutputLab;
                }
            }
        }

        /// <summary>
        /// 基于位屏蔽的颜色聚类
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static Bitmap PCluster(Bitmap a)
        {
            try
            {
                Rectangle rect = new Rectangle(0, 0, a.Width, a.Height);
                System.Drawing.Imaging.BitmapData bmpData = a.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                int stride = bmpData.Stride;
                unsafe
                {
                    byte* pIn = (byte*)bmpData.Scan0.ToPointer();
                    byte* P;
                    int R, G, B;
                    for (int y = 0; y < a.Height; y++)
                    {
                        for (int x = 0; x < a.Width; x++)
                        {
                            P = pIn;
                            B = P[0];
                            G = P[1];
                            R = P[2];
                            P[0] = (byte)(B & 192);  //屏蔽末6位
                            P[1] = (byte)(G & 192);
                            P[2] = (byte)(R & 192);
                            pIn += 3;

                        }
                        pIn += stride - a.Width * 3;
                    }


                }
                a.UnlockBits(bmpData);
                return a;
            }
            catch (Exception)
            {
                return null;
            }
        }
        /// <summary>
        /// 重叠两张图片
        /// </summary>
        /// <param name="source1"></param>
        /// <param name="source2"></param>
        /// <returns></returns>
        public static Bitmap Overlap(Bitmap source1, Bitmap source2)
        {
            Graphics g = Graphics.FromImage(source1);
            g.DrawImage(source2, new Rectangle(0, 0, source1.Width, source1.Height));
            g.Dispose();
            return source1;

        }
        public static Bitmap AddSpace(Bitmap source, int left, int top, int right, int bottom)
        {
            if (source.PixelFormat == PixelFormat.DontCare) return null;
            Bitmap bitmap = new Bitmap((int)source.Width + left + right, source.Height + top + right, PixelFormat.Format24bppRgb);
            Graphics g = Graphics.FromImage(bitmap);
            g.Clear(Color.White);
            g.DrawImage(source, new Rectangle(left, top, source.Width, source.Height));
            g.Dispose();
            source.Dispose();
            return bitmap;

        }
        public static Bitmap WriteEdgeGradual(Bitmap SourceBmp)
        {
            if (SourceBmp.PixelFormat == PixelFormat.DontCare) return null;

            int X, Y;
            int SourceWidth, SourceHeight, SourceStride;
            int SourceYIndex;

            SourceWidth = SourceBmp.Width; SourceHeight = SourceBmp.Height;
            SourceStride = (int)((SourceBmp.Width * 3 + 3) & 0XFFFFFFFC);//gs:即使二进制的倒数第三位以后的置为0，整个数目以4的倍数递增

            byte[] SourceImageData = new byte[SourceStride * SourceHeight];// 用于保存图像数据，（处理前后的都为他)
            unsafe
            {
                fixed (byte* SourceP = &SourceImageData[0])
                {
                    byte* SourceDataP = SourceP;
                    BitmapData SourceBmpData = new BitmapData();
                    SourceBmpData.Scan0 = (IntPtr)SourceDataP;//  设置为字节数组的的第一个元素在内存中的地址
                    SourceBmpData.Stride = SourceStride;
                    SourceBmp.LockBits(new System.Drawing.Rectangle(0, 0, SourceBmp.Width, SourceBmp.Height), ImageLockMode.ReadWrite | ImageLockMode.UserInputBuffer, System.Drawing.Imaging.PixelFormat.Format24bppRgb, SourceBmpData);

                    int gradualCount = 10;

                    for (Y = SourceHeight - 9; Y < SourceHeight; Y++)
                    {
                        SourceYIndex = Y * SourceStride;
                        gradualCount--;
                        for (X = 0; X < SourceWidth; X++)
                        {
                            SourceDataP[SourceYIndex] = (byte)Math.Min(SourceDataP[SourceYIndex] * gradualCount * (1 + 3 / gradualCount), 255);
                            SourceDataP[SourceYIndex + 1] = (byte)Math.Min(SourceDataP[SourceYIndex + 1] * gradualCount * (1 + 3 / gradualCount), 255);
                            SourceDataP[SourceYIndex + 2] = (byte)Math.Min(SourceDataP[SourceYIndex + 2] * gradualCount * (1 + 3 / gradualCount), 255);
                            SourceYIndex += 3;
                        }
                    }
                    SourceBmp.UnlockBits(SourceBmpData);
                }
            }
            return SourceBmp;
        }
        /// <summary>
        /// 基于相似hash的比较算法
        /// </summary>
        public class SimilarPhoto
        {
            Image SourceImg;

            public SimilarPhoto(string filePath)
            {
                SourceImg = Image.FromFile(filePath);
            }

            public SimilarPhoto(Stream stream)
            {
                SourceImg = Image.FromStream(stream);
            }

            public SimilarPhoto(Bitmap bitmap)
            {
                SourceImg = bitmap;
            }

            public String GetHash()
            {
                Image image = ReduceSize();
                Byte[] grayValues = ReduceColor(image);
                Byte average = CalcAverage(grayValues);
                String reslut = ComputeBits(grayValues, average);
                return reslut;
            }

            // Step 1 : Reduce size to 8*8
            private Image ReduceSize(int width = 8, int height = 8)
            {
                Image image = SourceImg.GetThumbnailImage(width, height, () => { return false; }, IntPtr.Zero);
                return image;
            }

            // Step 2 : Reduce Color
            private Byte[] ReduceColor(Image image)
            {
                Bitmap bitMap = new Bitmap(image);
                Byte[] grayValues = new Byte[image.Width * image.Height];

                for (int x = 0; x < image.Width; x++)
                    for (int y = 0; y < image.Height; y++)
                    {
                        Color color = bitMap.GetPixel(x, y);
                        byte grayValue = (byte)((color.R * 30 + color.G * 59 + color.B * 11) / 100);
                        grayValues[x * image.Width + y] = grayValue;
                    }
                return grayValues;
            }

            // Step 3 : Average the colors
            private Byte CalcAverage(byte[] values)
            {
                int sum = 0;
                for (int i = 0; i < values.Length; i++)
                    sum += (int)values[i];
                return Convert.ToByte(sum / values.Length);
            }

            // Step 4 : Compute the bits
            private String ComputeBits(byte[] values, byte averageValue)
            {
                char[] result = new char[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i] < averageValue)
                        result[i] = '0';
                    else
                        result[i] = '1';
                }
                return new String(result);
            }

            /// <summary>
            /// 计算图片相似度
            /// </summary>
            /// <param name="a">相似哈希a</param>
            /// <param name="b">相似哈希b</param>
            /// <returns></returns>
            public static Int32 CalcSimilarDegree(string a, string b)
            {
                if (a.Length != b.Length)
                    throw new ArgumentException();
                int count = 0;
                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i] != b[i])
                        count++;
                }
                return count;
            }

            /// <summary>
            /// 计算相似度
            /// </summary>
            /// <param name="streamA">图片A</param>
            /// <param name="streamB">图片B</param>
            /// <returns></returns>
            public static Int32 CalcSimilarDegree(Bitmap bitmapA, Bitmap bitmapB)
            {
                if (bitmapA == null || bitmapB == null)
                    return 9999;
                return CalcSimilarDegree(new SimilarPhoto(bitmapA).GetHash(), new SimilarPhoto(bitmapB).GetHash());
            }
        }

        #endregion

    }

}
