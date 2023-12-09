using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageBasic;

namespace ImageFilter
{
    public static class SketchFilter
    {
        /// <summary>
        /// 素描
        /// </summary>
        /// <param name="SourceBmp">原图</param>
        /// <param name="Contrast">预置对比度</param>
        /// <param name="Brightness">预置明度</param>
        /// <param name="MaxGrayVlue">生成图的最大灰度（色度）值</param>
        /// <param name="IsGray">是否生成灰度图</param>
        /// <remarks>注意应该从返回中取结果，而不是形参中，因为输入图引用有可能已经改变</remarks>
        static public  Bitmap Sketch(Bitmap SourceBmp, double Contrast=0.1, double Brightness=0,byte MaxGrayVlue=0,bool IsGray=true)
        {
            int X, Y;
            int SourceWidth, SourceHeight, SourceStride, DestStride, DestHeight;
            int SourceYIndex, DestYindex, SpeedTwo, SpeedThree;
            int BlueOne, BlueTwo, GreenOne, GreenTwo, RedOne, RedTwo;
            int PowerRed, PowerGreen, PowerBlue;
            byte B, G, R;
            if (SourceBmp.PixelFormat != System.Drawing.Imaging.PixelFormat.Format24bppRgb) {
                SourceBmp = BasicMethodClass.ConverImageTo24bit(SourceBmp);
                if (SourceBmp.PixelFormat != System.Drawing.Imaging.PixelFormat.Format24bppRgb) throw new FormatException();
            }

            byte[] SqrValue = BasicMethodClass.Cach.SqrValue;

            SourceWidth = SourceBmp.Width; SourceHeight = SourceBmp.Height; SourceStride = (int)((SourceBmp.Width * 3 + 3) & 0XFFFFFFFC);//gs:即使二进制的倒数第三位以后的置为0，整个数目以4的倍数递增
            DestStride = (SourceWidth + 2) * 3; DestHeight = SourceHeight + 2;// 宽度和高度都扩展2个像素

            byte[] SourceImageData = new byte[SourceStride * SourceHeight];// 用于保存图像数据，（处理前后的都为他)
            byte[] SourceImageDataTemp = new byte[SourceStride * SourceHeight];  //用于叠加处理DestImageData
            byte[] DestImageData = new byte[DestStride * DestHeight];// 用于保存扩展后的图像数据
            unsafe
            {
                fixed (byte* SourceP = &SourceImageData[0], SourcePT = &SourceImageDataTemp[0], DestP = &DestImageData[0], LP = &SqrValue[0])
                {
                    byte* SourceDataP = SourceP, SourceDataPT = SourcePT, DestDataP = DestP, LutP = LP;
                    BitmapData SourceBmpData = new BitmapData();
                    SourceBmpData.Scan0 = (IntPtr)SourceDataP;//  设置为字节数组的的第一个元素在内存中的地址
                    SourceBmpData.Stride = SourceStride;
                    SourceBmp.LockBits(new System.Drawing.Rectangle(0, 0, SourceBmp.Width, SourceBmp.Height), ImageLockMode.ReadWrite | ImageLockMode.UserInputBuffer, System.Drawing.Imaging.PixelFormat.Format24bppRgb, SourceBmpData);

                    Stopwatch Sw = new Stopwatch();//  只获取计算用时
                    Sw.Start();
                    for (Y = 0; Y < SourceHeight; Y++)
                    {
                        SourceYIndex = Y * SourceStride;
                        for (X = 0; X < SourceWidth; X++)
                        {
                            //备份原图
                            SourceDataPT[SourceYIndex] = SourceDataP[SourceYIndex];
                            SourceDataPT[SourceYIndex + 1] = SourceDataP[SourceYIndex + 1];
                            SourceDataPT[SourceYIndex + 2] = SourceDataP[SourceYIndex + 2];
                            //调节对比度
                            SourceDataP[SourceYIndex] = (byte)Math.Min(SourceDataP[SourceYIndex] * Contrast + Brightness, 255);                     //  查表
                            SourceDataP[SourceYIndex + 1] = (byte)Math.Min(SourceDataP[SourceYIndex + 1] * Contrast + Brightness, 255);
                            SourceDataP[SourceYIndex + 2] = (byte)Math.Min(SourceDataP[SourceYIndex + 2] * Contrast + Brightness, 255);

                            SourceYIndex += 3;                                         //  跳往下一个像素
                        }
                    }
                    for (Y = 0; Y < SourceHeight; Y++)
                    {//拓展边缘
                        System.Buffer.BlockCopy(SourceImageData, SourceStride * Y, DestImageData, DestStride * (Y + 1), 3);        // 填充扩展图的左侧第一列像素(不包括第一个和最后一个点)
                        System.Buffer.BlockCopy(SourceImageData, SourceStride * Y + (SourceWidth - 1) * 3, DestImageData, DestStride * (Y + 1) + (SourceWidth + 1) * 3, 3);  // 填充最右侧那一列的数据
                        System.Buffer.BlockCopy(SourceImageData, SourceStride * Y, DestImageData, DestStride * (Y + 1) + 3, SourceWidth * 3);
                    }
                    System.Buffer.BlockCopy(DestImageData, DestStride, DestImageData, 0, DestStride);              //  第一行
                    System.Buffer.BlockCopy(DestImageData, (DestHeight - 2) * DestStride, DestImageData, (DestHeight - 1) * DestStride, DestStride);    //  最后一行               

                    for (Y = 0; Y < SourceHeight; Y++)
                    {
                        SourceYIndex = Y * SourceStride;
                        DestYindex = DestStride * Y;
                        for (X = 0; X < SourceWidth; X++)
                        {
                            SpeedTwo = DestYindex + DestStride;          //  尽量减少计算
                            SpeedThree = SpeedTwo + DestStride;
                            //  下面的就是严格的按照Sobel算字进行计算，代码中的*2一般会优化为移位或者两个Add指令的，如果你不放心，当然可以直接改成移位
                            RedOne = DestDataP[DestYindex]
                                    + 2 * DestDataP[SpeedTwo]
                                    + DestDataP[SpeedThree]
                                    - DestDataP[DestYindex + 6]
                                    - 2 * DestDataP[SpeedTwo + 6]
                                    - DestDataP[SpeedThree + 6];

                            GreenOne = DestDataP[DestYindex + 1]
                                    + 2 * DestDataP[SpeedTwo + 1]
                                    + DestDataP[SpeedThree + 1]
                                    - DestDataP[DestYindex + 7]
                                    - 2 * DestDataP[SpeedTwo + 7]
                                    - DestDataP[SpeedThree + 7];

                            BlueOne = DestDataP[DestYindex + 2]
                                    + 2 * DestDataP[SpeedTwo + 2]
                                    + DestDataP[SpeedThree + 2]
                                    - DestDataP[DestYindex + 8]
                                    - 2 * DestDataP[SpeedTwo + 8]
                                    - DestDataP[SpeedThree + 8];

                            RedTwo = DestDataP[DestYindex]
                                    + 2 * DestDataP[DestYindex + 3]
                                    + DestDataP[DestYindex + 6]
                                    - DestDataP[SpeedThree]
                                    - 2 * DestDataP[SpeedThree + 3]
                                    - DestDataP[SpeedThree + 6];

                            GreenTwo = DestDataP[DestYindex + 1]
                                    + 2 * DestDataP[DestYindex + 4]
                                    + DestDataP[DestYindex + 7]
                                    - DestDataP[SpeedThree + 1]
                                    - 2 * DestDataP[SpeedThree + 4]
                                    - DestDataP[SpeedThree + 7];

                            BlueTwo = DestDataP[DestYindex + 2]
                                    + 2 * DestDataP[DestYindex + 5]
                                    + DestDataP[DestYindex + 8]
                                    - DestDataP[SpeedThree + 2]
                                    - 2 * DestDataP[SpeedThree + 5]
                                    - DestDataP[SpeedThree + 8];

                            PowerRed = RedOne * RedOne + RedTwo * RedTwo;
                            PowerGreen = GreenOne * GreenOne + GreenTwo * GreenTwo;
                            PowerBlue = BlueOne * BlueOne + BlueTwo * BlueTwo;

                            if (PowerRed > 65025) PowerRed = 65025;//  处理掉溢出值
                            if (PowerGreen > 65025) PowerGreen = 65025;
                            if (PowerBlue > 65025) PowerBlue = 65025;
                            //变亮模式叠加结果图到原图
                            R= Math.Max(LutP[PowerRed], SourceDataPT[SourceYIndex]);
                            G= Math.Max(LutP[PowerGreen], SourceDataPT[SourceYIndex]);
                            B= Math.Max(LutP[PowerBlue], SourceDataPT[SourceYIndex]);
                            //R = LutP[PowerRed];
                            //G = LutP[PowerGreen];
                            //B = LutP[PowerBlue];
                            if (IsGray)
                            {
                                G = (byte)(.299 * R + .587 * G + .114 * B);
                                if (G < MaxGrayVlue) G = MaxGrayVlue;//值越大越亮
                                SourceDataP[SourceYIndex] = G;
                                SourceDataP[SourceYIndex + 1] = G;
                                SourceDataP[SourceYIndex + 2] = G;
                            }
                            else
                            {
                                SourceDataP[SourceYIndex] =R<MaxGrayVlue?MaxGrayVlue:R;
                                SourceDataP[SourceYIndex + 1] = G < MaxGrayVlue ? MaxGrayVlue : G;
                                SourceDataP[SourceYIndex + 2] = B < MaxGrayVlue ? MaxGrayVlue : B;
                            }

                            SourceYIndex += 3;
                            DestYindex += 3;
                        }
                    }
                    Sw.Stop();
                    Debug.Print("计算用时: " + Sw.ElapsedMilliseconds.ToString() + " ms");
                    
                    SourceBmp.UnlockBits(SourceBmpData);
                }
            }
            return SourceBmp;
        }
    }
}
