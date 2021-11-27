using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Communication
{
    public struct COPYDATASTRUCT
    {
        public IntPtr dwData; //可以是任意值
        public int cbData;    //指定lpData内存区域的字节数
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpData; //发送给目录窗口所在进程的数据
    }
    public enum MyMsgType {
        MainWindow_Show,
        audio_Startplay,
        audio_Playing,
        audio_Suspend,
        audio_Replay,
        audio_Stop,
        audio_Drop,
        audio_Volume,
        audio_Dispose, 
        Unknown }
    public enum PlayState { playing, stop, suspend, droping }
    public enum AudioProgressCommunicationDataType
    {
        UNKNOWN,
        /// <summary>
        /// double描述的秒
        /// </summary>
        DoubleSec,
        /// <summary>
        /// int描述的百分比
        /// </summary>
        IntPercent,
        /// <summary>
        /// string描述的文字
        /// </summary>
        StringText,
    }

    public class IPCCommunicationDataFromt : MarshalByRefObject
    {
        //private MyMsgType myMsgType;
        //private long currentPlayTime;
        //private long curPlayTotalTime;
        //private int currentVolum;
        //private PlayState curPlayState;
        //private string value;
    }

    //public class ThreadMsgFilter : IMessageFilter
    //{
    //    private Func<MyMsgType, string, AudioProgressCommunicationDataType, bool> dosomething;

    //    public ThreadMsgFilter(Func<MyMsgType,string, AudioProgressCommunicationDataType, bool> dosomething)
    //    {
    //        this.dosomething = dosomething;
    //    }
    //    public bool PreFilterMessage(ref Message m)
    //    {
    //        if (m.Msg == 0x91FC)
    //        {
    //            MyMsgType msgType;

    //            switch ((int)m.WParam)
    //            {
    //                case (int)MyMsgType.Startplay:
    //                    msgType = MyMsgType.Startplay;
    //                    break;
    //                case (int)MyMsgType.Drop:
    //                    msgType = MyMsgType.Drop;
    //                    break;
    //                case (int)MyMsgType.Volume:
    //                    msgType = MyMsgType.Volume;
    //                    break;
    //                case (int)MyMsgType.Replay:
    //                    msgType = MyMsgType.Replay;
    //                    break;
    //                case (int)MyMsgType.Suspend:
    //                    msgType = MyMsgType.Suspend;
    //                    break;
    //                case (int)MyMsgType.Stop:
    //                    msgType = MyMsgType.Stop;
    //                    break;
    //                case (int)MyMsgType.Dispose:
    //                    msgType = MyMsgType.Dispose;
    //                    break;
    //                default:
    //                    msgType = MyMsgType.Unknown;
    //                    break;
    //            }

    //            dosomething(msgType,); //显示窗口或其它事
    //            return true;
    //        }
    //        return false;
    //    }
    //}

}
