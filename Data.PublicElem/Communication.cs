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

        static public void SendMsg(MyMsgType myMsgType, string msgValue, AudioProgressCommunicationDataType datatype = AudioProgressCommunicationDataType.UNKNOWN)
        {
            string sendString = msgValue;
            byte[] sarr = System.Text.Encoding.Default.GetBytes(sendString);
            int len = sarr.Length;
            COPYDATASTRUCT cds;
            cds.dwData = (IntPtr)datatype;
            cds.cbData = len + 1;
            cds.lpData = sendString;
            //if ((int)MainWindow.CurMainWindowInstence.AudioProcess.MainWindowHandle == 0) throw new Exception("没找到音频进程");
            //SendMessage(MainWindow.CurMainWindowInstence.AudioProcess.MainWindowHandle, WM_COPYDATA, (int)myMsgType, ref cds);
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

        /// <summary>
        /// 用于和局域网的客服端通话[局域网播放功能未做]
        /// </summary>
        public class SocketCommunication
        {
            public class SocketServer
            {
                // 创建一个和客户端通信的套接字
                static Socket socketwatch = null;
                static NetworkStream networkStream;
                //定义一个集合，存储客户端信息
                static Dictionary<string, Socket> clientConnectionItems = new Dictionary<string, Socket> { };

                public static void Start(string[] args)
                {
                    //定义一个套接字用于监听客户端发来的消息，包含三个参数（IP4寻址协议，流式连接，Tcp协议）  
                    socketwatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    //服务端发送信息需要一个IP地址和端口号  
                    IPAddress address = IPAddress.Parse("127.0.0.1");
                    //将IP地址和端口号绑定到网络节点point上  
                    IPEndPoint point = new IPEndPoint(address, 8098);
                    //此端口专门用来监听的  

                    //监听绑定的网络节点  
                    socketwatch.Bind(point);

                    //将套接字的监听队列长度限制为20  
                    socketwatch.Listen(20);

                    //负责监听客户端的线程:创建一个监听线程  
                    Thread threadwatch = new Thread(watchconnecting);

                    //将窗体线程设置为与后台同步，随着主线程结束而结束  
                    threadwatch.IsBackground = true;

                    //启动线程     
                    threadwatch.Start();

                    Console.WriteLine("开启监听。。。");
                    Console.WriteLine("点击输入任意数据回车退出程序。。。");
                    Console.ReadKey();
                    Console.WriteLine("退出监听，并关闭程序。");
                }

                //监听客户端发来的请求  
                static void watchconnecting()
                {
                    Socket connection = null;

                    //持续不断监听客户端发来的请求     
                    while (true)
                    {
                        try
                        {
                            connection = socketwatch.Accept();
                        }
                        catch (Exception ex)
                        {
                            //提示套接字监听异常     
                            Console.WriteLine(ex.Message);
                            break;
                        }

                        //获取客户端的IP和端口号  
                        IPAddress clientIP = (connection.RemoteEndPoint as IPEndPoint).Address;
                        int clientPort = (connection.RemoteEndPoint as IPEndPoint).Port;

                        //让客户显示"连接成功的"的信息  
                        string sendmsg = "连接服务端成功！\r\n" + "本地IP:" + clientIP + "，本地端口" + clientPort.ToString();
                        byte[] arrSendMsg = Encoding.UTF8.GetBytes(sendmsg);
                        connection.Send(arrSendMsg);



                        //客户端网络结点号  
                        string remoteEndPoint = connection.RemoteEndPoint.ToString();
                        //显示与客户端连接情况
                        Console.WriteLine("成功与" + remoteEndPoint + "客户端建立连接！\t\n");
                        //添加客户端信息  
                        clientConnectionItems.Add(remoteEndPoint, connection);

                        //IPEndPoint netpoint = new IPEndPoint(clientIP,clientPort); 
                        IPEndPoint netpoint = connection.RemoteEndPoint as IPEndPoint;

                        //创建一个通信线程      
                        ParameterizedThreadStart pts = new ParameterizedThreadStart(recv);
                        Thread thread = new Thread(pts);
                        //设置为后台线程，随着主线程退出而退出 
                        thread.IsBackground = true;
                        //启动线程     
                        thread.Start(connection);
                    }
                }

                /// <summary>
                /// 接收客户端发来的信息，客户端套接字对象
                /// </summary>
                /// <param name="socketclientpara"></param>    
                static void recv(object socketclientpara)
                {
                    Socket socketServer = socketclientpara as Socket;

                    while (true)
                    {
                        //创建一个内存缓冲区，其大小为1024*1024字节  即1M     
                        byte[] arrServerRecMsg = new byte[1024 * 1024];
                        //将接收到的信息存入到内存缓冲区，并返回其字节数组的长度    
                        try
                        {
                            int length = socketServer.Receive(arrServerRecMsg);


                            //将机器接受到的字节数组转换为人可以读懂的字符串     
                            string strSRecMsg = Encoding.UTF8.GetString(arrServerRecMsg, 0, length);

                            //将发送的字符串信息附加到文本框txtMsg上     
                            Console.WriteLine("客户端:" + socketServer.RemoteEndPoint + ",time:" + GetCurrentTime() + "\r\n" + strSRecMsg + "\r\n\n");



                            socketServer.Send(Encoding.UTF8.GetBytes("测试server 是否可以发送数据给client "));
                        }
                        catch (Exception ex)
                        {
                            clientConnectionItems.Remove(socketServer.RemoteEndPoint.ToString());

                            Console.WriteLine("Client Count:" + clientConnectionItems.Count);

                            //提示套接字监听异常  
                            Console.WriteLine("客户端" + socketServer.RemoteEndPoint + "已经中断连接" + "\r\n" + ex.Message + "\r\n" + ex.StackTrace + "\r\n");
                            //关闭之前accept出来的和客户端进行通信的套接字 
                            socketServer.Close();
                            break;
                        }
                    }
                }

                ///      
                /// 获取当前系统时间的方法    
                /// 当前时间     
                static DateTime GetCurrentTime()
                {
                    DateTime currentTime = new DateTime();
                    currentTime = DateTime.Now;
                    return currentTime;
                }
            }

            public class SocketClient
            {
                //创建 1个客户端套接字 和1个负责监听服务端请求的线程  
                Thread threadclient = null;
                Socket socketclient = null;
                static NetworkStream networkStream;
                Func<Stream, bool> func;

                public SocketClient(Func<Stream, bool>  func)
                {
                    this.func = func;
                }

                private void Start(object sender, EventArgs e)
                {
                    //定义一个套接字监听  
                    socketclient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    //获取文本框中的IP地址  
                    IPAddress address = IPAddress.Parse("127.0.0.1");

                    //将获取的IP地址和端口号绑定在网络节点上  
                    IPEndPoint point = new IPEndPoint(address, 8098);

                    try
                    {
                        //客户端套接字连接到网络节点上，用的是Connect  
                        socketclient.Connect(point);
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine("连接失败\r\n");

                        return;
                    }

                    threadclient = new Thread(recv);
                    threadclient.IsBackground = true;
                    threadclient.Start();
                }

                // 接收服务端发来信息的方法    
                void recv()
                {
                    int x = 0;
                    //持续监听服务端发来的消息 
                    while (true)
                    {
                        try
                        {
                            //定义一个1M的内存缓冲区，用于临时性存储接收到的消息  
                            byte[] arrRecvmsg = new byte[1024 * 1024*50];

                            //将客户端套接字接收到的数据存入内存缓冲区，并获取长度
                            int length = socketclient.Receive(arrRecvmsg);

                            //将套接字获取到的字符数组转换为人可以看懂的字符串
                            string strRevMsg = Encoding.UTF8.GetString(arrRecvmsg, 0, length);
                            Debug.WriteLine("已收到流" + "\r\n\n");
                                x = 1;

                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("远程服务器已经中断连接" + "\r\n\n");
                            Debug.WriteLine("远程服务器已经中断连接" + "\r\n");
                            break;
                        }
                    }
                }

                //获取当前系统时间  
                DateTime GetCurrentTime()
                {
                    DateTime currentTime = new DateTime();
                    currentTime = DateTime.Now;
                    return currentTime;
                }

                //发送字符信息到服务端的方法  
                void ClientSendMsg(string sendMsg)
                {
                    //将输入的内容字符串转换为机器可以识别的字节数组     
                    byte[] arrClientSendMsg = Encoding.UTF8.GetBytes(sendMsg);
                    //调用客户端套接字发送字节数组     
                    socketclient.Send(arrClientSendMsg);
                    //将发送的信息追加到聊天内容文本框中     
                    Debug.WriteLine("hello...." + ": " + GetCurrentTime() + "\r\n" + sendMsg + "\r\n\n");
                }

            }

            public void Init() { }

            //需要同步xml文件，
            //需要即时获取歌曲流，
            //那几个循环很可能用不上，毕竟自同步监听
        }
    }
}
