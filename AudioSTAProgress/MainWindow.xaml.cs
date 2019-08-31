
using AddIn.Audio;
using Communication;
using System.Windows;

namespace AudioSTAProgress
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        //const int WM_COPYDATA = 0x004A;
        //[DllImport("User32.dll", EntryPoint = "SendMessage")]
        //private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, ref COPYDATASTRUCT lParam);
        //private HwndSource hwndSource;

        //static public IAudioPlayer naudioplayer = NAudioPlayer.GetNAudioPlayer();
        //static public IAudioPlayer cscplayer = CSCorePlayer.GetCSCorePlayer();
        //static public IAudioPlayer player = naudioplayer;
        //static public IntPtr MainhWnd;
        //static public Timer PlayTimer;
        public MainWindow()
        {
            InitializeComponent();
            Communication.Communication.IPCCommunication.InitServer<CSCorePlayer>();
        }

        ////private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        ////{
        ////    // Handle messages...
        ////    if (msg == 0x004A)
        ////    {
        ////        var obj = Marshal.PtrToStructure<COPYDATASTRUCT>(lParam);

        ////        switch ((int)wParam)
        ////        {
        ////            case (int)MyMsgType.Startplay:
        ////                PlayThread(obj.lpData);
        ////                break;
        ////            case (int)MyMsgType.Drop:
        ////                var t= double.Parse(obj.lpData);
        ////                player.Position = TimeSpan.FromSeconds(t);
        ////                break;
        ////            case (int)MyMsgType.Volume:
        ////                player.Volume = int.Parse(obj.lpData);
        ////                break;
        ////            case (int)MyMsgType.Replay:
        ////                player.Play();
        ////                break;
        ////            case (int)MyMsgType.Suspend:
        ////                if(!player.IsPause)
        ////                    player.Pause();
        ////                break;
        ////            case (int)MyMsgType.Stop:
        ////                player.Stop();
        ////                break;
        ////            case (int)MyMsgType.Dispose:
        ////                player.Dispose();
        ////                break;
        ////        }

        ////    }
        ////        return IntPtr.Zero;
        ////}

        //static void PlayThread(string file)
        //{
        //    player.Open(file);
        //    player.Play();
        //    Process[] pros = Process.GetProcesses(); //获取本机所有进程
        //    Process pro = new Process();
        //    //for (int i = 0; i < pros.Length; i++)
        //    //{
        //    //    if (pros[i].ProcessName == "PlayProjectGame") //名称为ProcessCommunication的进程
        //    //    {
        //    //        MainhWnd = pros[i].MainWindowHandle; //获取ProcessCommunication.exe主窗口句柄
        //    //        pro = pros[i];
        //    //        break;
        //    //    }
        //    //}
        //    //SendMsg(MyMsgType.Startplay, player.TotalTime.Ticks.ToString());
        //    //PlayTimer = new Timer(delegate
        //    //{
        //    //    SendMsg(MyMsgType.Playing, player.Position.Ticks.ToString());
        //    //}, null, 250, 250);

        //}
        //static public void SendMsg(MyMsgType myMsgType, string msgValue)
        //{
        //    string sendString = msgValue;
        //    byte[] sarr = System.Text.Encoding.Default.GetBytes(sendString);
        //    int len = sarr.Length;
        //    COPYDATASTRUCT cds;
        //    cds.dwData = (IntPtr)0;
        //    cds.cbData = len + 1;
        //    cds.lpData = sendString;
        //    SendMessage(MainhWnd, WM_COPYDATA, (int)myMsgType, ref cds);

        //}

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            //hwndSource.AddHook(WndProc);
            //communicat.InitServer();

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Communication.Communication.IPCCommunication.getAudioObj<CSCorePlayer>().Dispose();
        }
    }
}
