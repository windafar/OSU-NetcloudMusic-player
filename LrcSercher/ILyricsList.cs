using System.Collections.ObjectModel;
namespace AddIn.LrcSercher
{
    public interface ILyricsSercher
    {
        string SercherName { get; }
        /// <summary>
        /// 获取保存的LRC列表（如果有）
        /// </summary>
       ObservableCollection<ILrcSercherInfoItem> SercherResultList { get; }
        /// <summary>
        /// 在派生类中，从网络中获取Lrc列表
        /// </summary>
        /// <param name="artist">艺术家</param>
        /// <param name="title">歌曲名</param>
        /// <param name="filesavepath">文件保存路径</param>
        /// <param name="misicid">歌曲ID（可选，目前用于直接获取网易的Lrc）</param>
        /// <returns></returns>
        ObservableCollection<ILrcSercherInfoItem> GetLrcInfoSercherList(string artist, string title, string filesavepath,string misicid=null);
    }
}