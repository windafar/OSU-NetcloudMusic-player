using ImageBasic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace VisualAttentionDetection
{
    static public class VisualAttentionDetectionClass
    {
        /// <summary>
        /// 实现功能： saliency is determined as the local contrast of an image region with respect to its neighborhood at various scales
        /// 参考论文： Salient Region Detection and Segmentation   Radhakrishna Achanta, Francisco Estrada, Patricia Wils, and Sabine SÄusstrunk   2008  , Page 4-5
        /// 参考laviewpbt的C实现 http://blog.csdn.net/laviewpbt/article/details/38357017
        /// </summary>
        /// <param name="srcBitmap"></param>
        /// <returns></returns>
        public static Bitmap SalientRegionDetectionBasedonLC(Bitmap srcBitmap)
        {
            int width = srcBitmap.Width, height = srcBitmap.Height;
            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData srcBmData = srcBitmap.LockBits(rect,
                      ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            Bitmap dstBitmap = BasicMethodClass.CreateGrayscaleImage(width, height);
            BitmapData dstBmData = dstBitmap.LockBits(rect,
                      ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);

            IntPtr srcScan = srcBmData.Scan0;
            IntPtr dstScan = dstBmData.Scan0;
            unsafe
            {
                byte* srcP = (byte*)(void*)srcScan;
                byte* dstP = (byte*)(void*)dstScan;
                int Value;

                int DistMaxValue = 0;//用于记录最大距离，用于最后归纳图片的有效颜色范围
                int IndexBit, CurIndex;//用于定位字节位和像素位置
                int[] Gray = new int[width * height];//用于保存计算完成了的灰度值
                int[] HistGram = new int[256];//用于直方图统计
                int[] Dist = new int[256];//用于记录每种颜色到各个颜色的距离
                //int[] DistMap = new int[width*height];//显著性图

                //创建直方图和灰度图
                for (int y = 0; y < height; y++)
                {
                    IndexBit = srcBmData.Stride * y;
                    CurIndex = width * y;
                    for (int x = 0; x < width; x++)
                    {
                        Value = (srcP[IndexBit] + srcP[IndexBit + 1] * 2 + srcP[IndexBit + 2]) / 4;
                        HistGram[Value]++;
                        Gray[CurIndex] = Value;
                        IndexBit += 3;
                        CurIndex++;
                    }
                }
                //记录颜色距离
                for (int Y = 0; Y < 256; Y++)
                {
                    Value = 0;
                    for (int X = 0; X < 256; X++)
                        Value += Math.Abs(Y - X) * HistGram[X];//Y点灰度与整张图的距离
                    Dist[Y] = Value;
                    if (DistMaxValue < Value) DistMaxValue = Value;
                }

                //计算用于减小距离为有效颜色值的倍数
                DistMaxValue = DistMaxValue / 256;

                //计算显著性图片
                for (int Y = 0; Y < height; Y++)
                {
                    CurIndex = Y * width;
                    IndexBit = Y * dstBmData.Stride;
                    for (int X = 0; X < width; X++)
                    {
                        Value = Dist[Gray[CurIndex]];        //    计算全图每个像素的显著性
                        dstP[IndexBit] = (byte)(Value / DistMaxValue);
                        CurIndex++;
                        IndexBit++;
                    }
                }
            }
            srcBitmap.UnlockBits(srcBmData);
            dstBitmap.UnlockBits(dstBmData);
            return dstBitmap;
        }
        /// <summary>
        ///  实现功能： 基于Frequency-tuned 的图像显著性检测
        ///    参考论文： Frequency-tuned Salient Region Detection， Radhakrishna Achantay， Page 4-5, 2009 CVPR 
        ///               http://ivrgwww.epfl.ch/supplementary_material/RK_CVPR09/   
        ///    参考论文作者RGB转LAB的浮点版本
        /// </summary>
        /// <param name="srcBitmap"></param>
        /// <returns></returns>
        public static Bitmap SalientRegionDetectionBasedOnFT(Bitmap srcBitmap)
        {
            int width = srcBitmap.Width, height = srcBitmap.Height;
            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData srcBmData = srcBitmap.LockBits(rect,
                      ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            Bitmap dstBitmap = BasicMethodClass.CreateGrayscaleImage(width, height);
            BitmapData dstBmData = dstBitmap.LockBits(rect,
                      ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);

            IntPtr srcScan = srcBmData.Scan0;
            IntPtr dstScan = dstBmData.Scan0;
            unsafe
            {
                double* srcP = (double*)(void*)srcScan;
                byte* dstP = (byte*)(void*)dstScan;
                int Index = 0, CurIndex = 0, srcStride = srcBmData.Stride, dstStride = dstBmData.Stride, X, Y;
                double MeanL = 0, MeanA = 0, MeanB = 0;
                double[] LabF;
                double[] DistMap = new double[dstStride * height];
                LabF = BasicMethodClass.sRGBtoXYZ(srcBmData, width, height, out MeanL, out MeanA, out MeanB);
                //     LabF = BasicMethodClass.GaussianSmooth(LabF, width, srcBmData.Stride, srcBmData.Height);
                double maxval = 0;
                double minval = double.MaxValue;
                int i = 0;
                for (Y = 0; Y < height; Y++)
                {
                    Index = Y * srcStride;

                    CurIndex = Y * dstStride;//
                    for (X = 0; X < width; X++)                                        //    计算像素的显著性
                    {//此处有时会出现数组越界，原因暂时未知(已解决，目标8位图有3字节的对齐)
                        double curValue = (MeanL - LabF[Index]) * (MeanL - LabF[Index]) + (MeanA - LabF[Index + 1]) * (MeanA - LabF[Index + 1]) + (MeanB - LabF[Index + 2]) * (MeanB - LabF[Index + 2]);
                        DistMap[CurIndex] = curValue;
                        Index += 3;
                        CurIndex++;
                        if (maxval < curValue) maxval = curValue;
                        if (minval > curValue) minval = curValue;
                        i++;
                    }
                }
                BasicMethodClass.Normalize(DistMap, dstBmData.Stride, height,maxval,minval, dstP);
                //写入序列化函数中。。
            }
            srcBitmap.UnlockBits(srcBmData);
            dstBitmap.UnlockBits(dstBmData);
            return dstBitmap;
        }

        public static Bitmap SalientRegionDetectionBasedOnFT(Bitmap srcBitmap,float distcof)
        {
            int width = srcBitmap.Width, height = srcBitmap.Height;
            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData srcBmData = srcBitmap.LockBits(rect,
                      ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            Bitmap dstBitmap = BasicMethodClass.CreateGrayscaleImage(width, height);
            BitmapData dstBmData = dstBitmap.LockBits(rect,
                      ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);

            IntPtr srcScan = srcBmData.Scan0;
            IntPtr dstScan = dstBmData.Scan0;
            unsafe
            {
                double* srcP = (double*)(void*)srcScan;
                byte* dstP = (byte*)(void*)dstScan;
                int Index = 0, CurIndex = 0, srcStride = srcBmData.Stride, dstStride = dstBmData.Stride, X, Y;
                double MeanL = 0, MeanA = 0, MeanB = 0;
                double[] LabF;
                double[] DistMap = new double[dstStride * height];
                LabF = BasicMethodClass.sRGBtoXYZ(srcBmData, width, height, out MeanL, out MeanA, out MeanB);
                //     LabF = BasicMethodClass.GaussianSmooth(LabF, width, srcBmData.Stride, srcBmData.Height);

                //加入显著位置权重
                int wk = (int)(width / 2), hk = (int)(height / 2);

                for (Y = 0; Y < height; Y++)
                {
                    Index = Y * srcStride;

                    CurIndex = Y * dstStride;//
                    for (X = 0; X < width; X++)                                        //    计算像素的显著性
                    {//此处有时会出现数组越界，原因暂时未知(已解决，目标8位图有3字节的对齐)
                        DistMap[CurIndex] = (MeanL - LabF[Index]) * (MeanL - LabF[Index]) + (MeanA - LabF[Index + 1]) * (MeanA - LabF[Index + 1]) + (MeanB - LabF[Index + 2]) * (MeanB - LabF[Index + 2]);
                        DistMap[CurIndex] *= Math.Max(1 - (Math.Abs(wk - X) + Math.Abs(hk - Y)) / (wk + hk + 1.0), distcof);
                        Index += 3;
                        CurIndex++;
                    }
                }
                DistMap = BasicMethodClass.Normalize(DistMap, dstBmData.Stride, height, dstP);
                //写入序列化函数中。。
            }
            srcBitmap.UnlockBits(srcBmData);
            dstBitmap.UnlockBits(dstBmData);
            return dstBitmap;
        }

    }


    static public class Test
    {
        /// <summary>
        /// 测试高斯模糊和RGB转LAB
        /// </summary>
        /// <param name="srcBitmap"></param>
        /// <returns></returns>
        public static Bitmap TestGaussianSmooth(Bitmap srcBitmap)
        {
            int width = srcBitmap.Width, height = srcBitmap.Height;
            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData srcBmData = srcBitmap.LockBits(rect,
                      ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            Bitmap dstBitmap = new Bitmap(width, height);
            BitmapData dstBmData = dstBitmap.LockBits(rect,
                      ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            IntPtr srcScan = srcBmData.Scan0;
            IntPtr dstScan = dstBmData.Scan0;
            unsafe
            {
                double* srcP = (double*)(void*)srcScan;
                byte* dstP = (byte*)(void*)dstScan;
                int Index = 0, CurIndex = 0, srcStride = srcBmData.Stride, dstStride = dstBmData.Stride, X, Y;
                double MeanL = 0, MeanA = 0, MeanB = 0;
                double[] LabF;
                LabF = BasicMethodClass.sRGBtoXYZ(srcBmData, width, height, out MeanL, out MeanA, out MeanB);
                LabF = BasicMethodClass.GaussianSmooth(LabF, width, srcBmData.Stride, srcBmData.Height);
                for (Y = 0; Y < height; Y++)
                {
                    Index = Y * srcStride;

                    CurIndex = Y * dstStride;
                    for (X = 0; X < width; X++)
                    { double R, G, B;
                        Lab2RGB(LabF[Index], LabF[Index + 1], LabF[Index + 2], out R, out G, out B);
                        dstP[CurIndex] = (byte)R;
                        dstP[CurIndex + 1] = (byte)G;
                        dstP[CurIndex + 2] = (byte)B;

                        Index += 3;
                        CurIndex += 3;
                    }

                }
                srcBitmap.UnlockBits(srcBmData);
                dstBitmap.UnlockBits(dstBmData);
                return dstBitmap;
            }

        }
        public static void Lab2RGB(double L, double a, double b,out double R, out double G, out double B)
        {
            double X, Y, Z, fX, fY, fZ;
            double RR, GG, BB;

            fY = Math.Pow((L + 16.0) / 116.0, 3.0);
            if (fY < 0.008856)
                fY = L / 903.3;
            Y = fY;

            if (fY > 0.008856)
                fY = Math.Pow(fY, 1.0 / 3.0);
            else
                fY = 7.787 * fY + 16.0 / 116.0;

            fX = a / 500.0 + fY;
            if (fX > 0.206893)
                X =Math.Pow(fX, 3.0);
            else
                X = (fX - 16.0 / 116.0) / 7.787;

            fZ = fY - b / 200.0;
            if (fZ > 0.206893)
                Z = Math.Pow(fZ, 3.0);
            else
                Z = (fZ - 16.0 / 116.0) / 7.787;

            X *= (0.950456 * 255);
            Y *= 255;
            Z *= (1.088754 * 255);

            RR = 3.240479 * X - 1.537150 * Y - 0.498535 * Z;
            GG = -0.969256 * X + 1.875992 * Y + 0.041556 * Z;
            BB = 0.055648 * X - 0.204043 * Y + 1.057311 * Z;

            R = (float)(RR < 0 ? 0 : RR > 255 ? 255 : RR);
            G = (float)(GG < 0 ? 0 : GG > 255 ? 255 : GG);
            B = (float)(BB < 0 ? 0 : BB > 255 ? 255 : BB);
        }

    }
}
