using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using WindowDemo;

namespace PlayProjectGame
{
    public partial class MainWindow
    {
        public System.Windows.Forms.NotifyIcon iNotifyIcon { get; private set; }
        /// <summary>
        /// 执行NotifyIcon功能
        /// </summary>
        void acc()
        {
            iNotifyIcon = new System.Windows.Forms.NotifyIcon();
            this.iNotifyIcon.Visible = false;

            Stream st = System.Windows.Application.GetResourceStream(new Uri("favicon-20180512030811818.ico", UriKind.Relative)).Stream;
            if (st != null)
            {
                try
                {
                    this.iNotifyIcon.Icon = new System.Drawing.Icon(st);

                    System.Windows.Forms.MenuItem miExit = new System.Windows.Forms.MenuItem("退出", OnNotifyIconExitLick);
                    System.Windows.Forms.MenuItem miOpenWindow = new System.Windows.Forms.MenuItem("打开窗口", OnNotifyIconOpenWindow);
                    iNotifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu();
                    iNotifyIcon.ContextMenu.MenuItems.Add(miExit);
                    iNotifyIcon.ContextMenu.MenuItems.Add(miOpenWindow);
                    iNotifyIcon.DoubleClick += new EventHandler(OnNotifyIconOpenWindow);
                    iNotifyIcon.Text = this.Title;

                }
                catch
                {
                    iNotifyIcon = null;
                }
            }

        }
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (iNotifyIcon != null && this.WindowState == WindowState.Minimized)
            {
                this.iNotifyIcon.Visible = true;
                this.ShowInTaskbar = false;
            }
        }
        public void OnNotifyIconOpenWindow(object sender, EventArgs e)
        {
            this.iNotifyIcon.Visible = false;
            this.ShowInTaskbar = true;
            this.WindowState = WindowState.Normal;
            this.Activate();
        }
        private void OnNotifyIconExitLick(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}
