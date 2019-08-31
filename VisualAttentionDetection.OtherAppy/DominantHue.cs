using ImageBasic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisualAttentionDetection;
using VisualAttentionDetection.CutImage;

namespace VisualAttentionDetection.OtherAppy
{
    /// <summary>
    /// 基于视觉显著性区域寻找主色调
    /// </summary>
    public class DominantHue:IDisposable
    {
        struct myrgbcolor {
           public double R;public double G;public double B;
        }

        Bitmap desBitmap;

        ///1，提出显著性区域
        ///2，求取区域色调
        public DominantHue(Bitmap source) => this.desBitmap = source;

        public Color GetDominantHue(double S=0.66,double L=0.66,int AttentionValue=200)
        {
            Color result;
            var destinatBitmap = new Bitmap(desBitmap);
            var VisualAttentionBitmap = VisualAttentionDetectionClass.SalientRegionDetectionBasedOnFT(destinatBitmap);
            int width = VisualAttentionBitmap.Width, height = VisualAttentionBitmap.Height;
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, width, height);
            BitmapData VisualAttentionBmData = VisualAttentionBitmap.LockBits(rect, ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

            CutImageClass cuter = new CutImageClass(VisualAttentionBitmap, new System.Drawing.Rectangle(0, 0, 700, 200), AttentionValue);
            us_PixlPoint[] AreaArr = cuter.FindArea(VisualAttentionBmData);
            var fx = cuter.fx;
            var MAXAreaArrNumbler = cuter.MAXAreaArrNumbler;

            List<myrgbcolor> list = new List<myrgbcolor>();
            int desWidth = desBitmap.Width;
            int desHeight = desBitmap.Height;
            //Bitmap dstBitmap = BasicMethodClass.CreateGrayscaleImage(desWidth, desHeight);
            BitmapData dstBmData = destinatBitmap.LockBits(new Rectangle(0, 0, desWidth, desHeight),
                      ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            //IntPtr srcScan = srcBmData.Scan0;
            IntPtr dstScan = dstBmData.Scan0;
            unsafe
            {
                //byte* srcP = (byte*)srcScan;
                byte* dstP = (byte*)dstScan;
                int SourceIndex = 0;
                int DstIndex = 0;
                int colorCount = 0;
                //if (MaxAreaArrValue == 0) throw new ArgumentException("Tolerance过低");
                //int x_start = desWidth, x_end = 0, y_start = desHeight, y_end = 0;
                for (int y = 1; y < desHeight - 1; y++)
                {
                    DstIndex = y * dstBmData.Stride;
                    SourceIndex = y * VisualAttentionBmData.Stride;
                    for (int x = 1; x < desWidth - 1; x++)
                    {
                        int Sx = fx[SourceIndex].AreaNum;
                        //  if (Sx != 0) throw new Exception();
                        if (Sx == MAXAreaArrNumbler)
                        {
                            list.Add(new myrgbcolor
                            {
                                R = dstP[DstIndex],
                                G = dstP[DstIndex + 1],
                                B = dstP[DstIndex + 2]
                            });
                        }
                        DstIndex += 3;
                        SourceIndex++;
                    }
                }
                destinatBitmap.UnlockBits(dstBmData);

                double R =0, G=0, B=0;
                foreach (var c in list) {
                    R += c.R / list.Count();
                    G += c.G / list.Count();
                    B += c.B / list.Count();
                }
                var HSL= BasicMethodClass.ColorHelper.RgbToHsl(new BasicMethodClass.ColorHelper.ColorRGB((int)R, (int)G, (int)B));
                if (HSL.L < 254 && HSL.L > 2)//屏蔽当亮度为254，取到H为0时的值
                {
                    HSL.L = (int)(255 * L); HSL.S = (int)(255 * S);
                }
                else {
                }
                var RGB = BasicMethodClass.ColorHelper.HslToRgb(HSL);

                result= Color.FromArgb(RGB.R, RGB.G, RGB.B);
            }

            destinatBitmap.Dispose();
            VisualAttentionBitmap.Dispose();
            desBitmap.Dispose();
            destinatBitmap = null; VisualAttentionBitmap = null;destinatBitmap = null;
            return result;
        }

        private us_AreaCount GetMaxValue(us_AreaCount[] LIST) {
            us_AreaCount result = new us_AreaCount();
            int count=0;
            for (int i = 0; i < LIST.Count();i++) {
                if (LIST[i].Count > count) { count = LIST[i].Count; result = LIST[i]; }
                else continue;
            }

            return result;
        }

        #region IDisposable Support
        private bool disposedValue = false; // 要检测冗余调用

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)。
                }

                // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
                desBitmap.Dispose();
                desBitmap = null; ;

                // TODO: 将大型字段设置为 null。

                disposedValue = true;
            }
        }

        //TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
         ~DominantHue()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(false);
        }

        // 添加此代码以正确实现可处置模式。
        public void Dispose()
        {
            // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
            Dispose(true);
            // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
             GC.SuppressFinalize(this);
        }
        #endregion

    }
}


///
/// 注意：使用模糊（如果不行直接固定固定色调）方式解决单色问题
///
