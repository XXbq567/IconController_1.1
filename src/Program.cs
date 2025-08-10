using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using Microsoft.Win32;

namespace IconController
{
    class Program
    {
        // Win32 API
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        
        private static readonly string APP_NAME = "IconController";
        private static readonly string REG_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        
        private static Mutex mutex;
        private static NotifyIcon trayIcon;
        
        [STAThread]
        static void Main(string[] args)
        {
            // 隐藏控制台窗口
            IntPtr consoleWindow = GetConsoleWindow();
            if (consoleWindow != IntPtr.Zero)
            {
                ShowWindow(consoleWindow, SW_HIDE);
            }
            
            // 单实例检查
            mutex = new Mutex(true, APP_NAME, out bool createdNew);
            if (!createdNew) return;
            
            // 创建托盘图标
            CreateTrayIcon();
            
            // 添加到开机启动
            AddToStartup();
            
            // 创建隐藏窗口并运行
            Application.Run(new HiddenForm());
        }
        
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        
        private static void CreateTrayIcon()
        {
            try
            {
                trayIcon = new NotifyIcon();
                trayIcon.Icon = SystemIcons.Application;
                trayIcon.Text = "桌面图标控制器 - Alt+Ctrl+Q";
                trayIcon.Visible = true;
                
                // 右键菜单
                ContextMenuStrip menu = new ContextMenuStrip();
                
                ToolStripMenuItem exitItem = new ToolStripMenuItem("退出");
                exitItem.Click += (s, e) => Application.Exit();
                menu.Items.Add(exitItem);
                
                trayIcon.ContextMenuStrip = menu;
            }
            catch { }
        }
        
        private static void AddToStartup()
        {
            try
            {
                string exePath = Application.ExecutablePath;
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REG_KEY, true))
                {
                    if (key?.GetValue(APP_NAME) == null)
                    {
                        key?.SetValue(APP_NAME, exePath);
                    }
                }
            }
            catch { }
        }
    }
}
