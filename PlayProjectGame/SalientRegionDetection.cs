using ImageBasic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VisualAttentionDetection;
using VisualAttentionDetection.CutImage;

namespace PlayProjectGame.PlayList
{
    class SalientRegionDetection
    {
        /// <summary>
        /// 获取显著图片
        /// </summary>
        /// <param name="filepath">图片文件流</param>
        /// <param name="prewidth">预处理后的宽</param>
        /// <param name="preheight">预处理后的高</param>
        /// <param name="premode">预处理时的压缩方式</param>
        /// <param name="pretype">预处理图片后的类型</param>
        /// <param name="R">最终切割大小</param>
        /// <param name="Tolerance">切割阙值（切图时判断是否连续的亮度边界，越大越严格）</param>
        /// <returns></returns>
        public Bitmap GetSRDFromStream(Stream InImageStream,
            double prewidth, double preheight, string premode, string pretype,
            Rectangle R,
            int Tolerance = 200)
        {
            if (R == null) R = new System.Drawing.Rectangle(0, 0, 256, 256);
            MemoryStream ms_out = new MemoryStream();
            Bitmap Srcbitmap = new Bitmap(InImageStream);
            //压缩尺寸以加快速度
            BasicMethodClass.MakeThumbnail(Srcbitmap, ms_out,Math.Min(prewidth,Srcbitmap.Width),Math.Min(preheight,Srcbitmap.Height), premode, pretype);
            if (InImageStream == null) throw new Exception("not expectation result");
            if (ms_out == null) return null ;
            var vdcSrcBitmap = new Bitmap(ms_out);
            ms_out.Dispose();

            var vdcmap= VisualAttentionDetectionClass.SalientRegionDetectionBasedOnFT(vdcSrcBitmap);
            if (R.Width > Srcbitmap.Width||R.Height>Srcbitmap.Height)
            {//格式化输出图片尺寸
                ms_out = new MemoryStream();
                BasicMethodClass.MakeThumbnail(Srcbitmap, ms_out, R.Width, R.Height, premode, pretype);
                Srcbitmap.Dispose();
                Srcbitmap = new Bitmap(ms_out);
                ms_out.Dispose();
            }
            CutImageClass cuter = new CutImageClass(vdcmap, R, Tolerance);
            var GenerImage = cuter.GCSsimp_getLightPointFromSource(Srcbitmap);
            vdcSrcBitmap.Dispose();
            vdcmap.Dispose();
            Srcbitmap.Dispose();

            return GenerImage;
        }

        /// <summary>
        /// 获取显著图片
        /// </summary>
        /// <param name="filepath">图片文件路径</param>
        /// <param name="prewidth">预处理后的宽</param>
        /// <param name="preheight">预处理后的高</param>
        /// <param name="premode">预处理时的压缩方式</param>
        /// <param name="pretype">预处理图片后的类型</param>
        /// <param name="R">最终切割大小</param>
        /// <param name="Tolerance">切割阙值（切图时判断是否连续的亮度边界，越大越严格）</param>
        /// <returns></returns>
        public Bitmap GetSRDFromPath(string filepath,
            double prewidth,double preheight,string premode,string pretype,
            Rectangle R,
            int Tolerance=200
            )
        {
            if (R == null) R = new System.Drawing.Rectangle(0, 0, 256, 256);
            MemoryStream ms_out = new MemoryStream();
            if(!File.Exists(filepath))return null;
            Bitmap Srcbitmap = new Bitmap(filepath);
            //压缩尺寸以加快速度
            BasicMethodClass.MakeThumbnail(Srcbitmap, ms_out, Math.Min(prewidth, Srcbitmap.Width), Math.Min(preheight, Srcbitmap.Height), premode, pretype);
            if (ms_out == null) return null;
            var vdcSrcBitmap = new Bitmap(ms_out);
            ms_out.Dispose();

            var vdcmap = VisualAttentionDetectionClass.SalientRegionDetectionBasedOnFT(vdcSrcBitmap);
            if (R.Width > Srcbitmap.Width || R.Height > Srcbitmap.Height)
            {//如果以高为缩放标准，而原图又是高度大于宽度，又要求输出图宽度大于高度的话就会出现问题，暂时没空解决这个问题，使用强制改变输出值
                
                //格式化输出图片尺寸
                ms_out = new MemoryStream();
                BasicMethodClass.MakeThumbnail(Srcbitmap, ms_out, R.Width*1, R.Height*1, premode, pretype);
                Srcbitmap.Dispose();
                Srcbitmap = new Bitmap(ms_out);
                ms_out.Dispose();
            }
            CutImageClass cuter = new CutImageClass(vdcmap, R, Tolerance);
            var GenerImage = cuter.GCSsimp_getLightPointFromSource(Srcbitmap);
            vdcSrcBitmap.Dispose();
            vdcmap.Dispose();
            Srcbitmap.Dispose();

            return GenerImage;
        }

    }
}

