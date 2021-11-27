using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Communication
{
    public class Communication
    {
        public const int WM_COPYDATA = 0x004A;
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, ref COPYDATASTRUCT lParam);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostThreadMessage(int threadId, uint msg, IntPtr wParam, IntPtr lParam);

        static public void SendMsg(IntPtr hand,MyMsgType myMsgType, string msgValue, AudioProgressCommunicationDataType datatype = AudioProgressCommunicationDataType.UNKNOWN)
        {
            string sendString = msgValue;
            byte[] sarr = System.Text.Encoding.Default.GetBytes(sendString);
            int len = sarr.Length;
            COPYDATASTRUCT cds;
            cds.dwData = (IntPtr)datatype;
            cds.cbData = len + 1;
            cds.lpData = sendString;
            SendMessage(hand, WM_COPYDATA, (int)myMsgType, ref cds);
        }

        /// <summary>
        /// 用于和音频线程通话
        /// </summary>
        public class IPCCommunication
        {
            Type type;
            static IPCCommunication _t;
            private IPCCommunication() { }
            static public IPCCommunication InitClient<T>()
            {
                if (_t != null) return _t;
                _t = new IPCCommunication();
                IpcChannel channel = new IpcChannel();
                ChannelServices.RegisterChannel(channel, false);
                WellKnownClientTypeEntry remotEntry = new WellKnownClientTypeEntry(_t.type=typeof(T), "ipc://AudioChannel/Player");
                RemotingConfiguration.RegisterWellKnownClientType(remotEntry);
                return _t;
            }
            static public void InitServer<T>()
            {
                IpcChannel serverchannel = new IpcChannel("AudioChannel");
                ChannelServices.RegisterChannel(serverchannel, false);
                RemotingConfiguration.RegisterWellKnownServiceType(typeof(T), "Player", WellKnownObjectMode.Singleton);
            }
            public  object getAudioObj()
            {
                return Activator.GetObject(type, "ipc://AudioChannel/Player");
            }
            public static T getAudioObj<T>()
            {
                return (T)Activator.GetObject(typeof(T), "ipc://AudioChannel/Player");
            }
        }
    }
}
