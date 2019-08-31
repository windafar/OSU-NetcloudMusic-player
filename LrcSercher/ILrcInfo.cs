using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AddIn.LrcSercher
{
    public interface ILrcSercherInfoItem
    {
        string Artist { get; set; }
        string FilePath { get; set; }
        string Id { get; set; }
        string LrcUri { get; set; }
        string Title { get; set; }
        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="downLoadPath">当FilePath属性有效时，指定downLoadPath为空可以直接覆盖下载到FilePath</param>
        /// <returns></returns>
        bool DownloadLrc(string downLoadPath=null);
    }
}
