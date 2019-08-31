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
            BasicMethodClass.MakeThumbnail((MemoryStream)InImageStream, ms_out, prewidth, preheight, premode, pretype);

            if (InImageStream == null|| ms_out == null) return null ;
            var srcBitmap = new Bitmap(ms_out);
            ms_out.Dispose();
            var vdcmap= VisualAttentionDetectionClass.SalientRegionDetectionBasedOnFT(srcBitmap);

            CutImageClass cuter = new CutImageClass(vdcmap, R, Tolerance);
            var GenerImage = cuter.GCSsimp_getLightPointFromSource(srcBitmap);
            srcBitmap.Dispose();
            if (GenerImage == null) throw new Exception();
            vdcmap.Dispose();
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
            MemoryStream mss = new MemoryStream();
            BasicMethodClass.MakeThumbnail(filepath, mss,prewidth, preheight, premode, pretype);
            if (mss.Length == 0) return null;
            var srcBitmap = new Bitmap(mss);
            //var vdcmap = VisualAttentionDetectionClass.SalientRegionDetectionBasedOnFT(srcBitmap, 0.75f);
            var vdcmap = VisualAttentionDetectionClass.SalientRegionDetectionBasedOnFT(srcBitmap);
            mss.Dispose();
            CutImageClass cuter = new CutImageClass(vdcmap, R, Tolerance);
            var GenerImage = cuter.GCSsimp_getLightPointFromSource(srcBitmap);
            srcBitmap.Dispose();
            vdcmap.Dispose();
            return GenerImage;
        }

    }
}

