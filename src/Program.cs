using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;           // ← 补上这一行即可解决 Mutex 报错
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using Microsoft.Win32;
using System.Security.Principal;
using System.Linq;

namespace IconController
{
    class Program
    {
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("kernel32.dll")] private static extern IntPtr GetConsoleWindow();
        private const int SW_HIDE = 0;

        private static readonly string APP_NAME = "IconController";
        private static readonly string REG_KEY  = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        private static Mutex      mutex;
        private static NotifyIcon trayIcon;
        public  static DebugWindow debugWindow;

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                IntPtr console = GetConsoleWindow();
                if (console != IntPtr.Zero) ShowWindow(console, SW_HIDE);

                debugWindow = new DebugWindow();
                Log("=== IconController 启动 ===");

                bool isElevated = args.Contains("--elevated");
                Log($"启动参数: {string.Join(" ", args)}");

                mutex = new Mutex(true, "Global\\" + APP_NAME, out bool createdNew);
                if (!createdNew)
                {
                    MessageBox.Show("程序已在运行中，请检查系统托盘", APP_NAME,
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (!isElevated && !IsRunAsAdmin())
                {
                    Log("需要管理员权限，请求提升...");
                    RequestAdminPrivileges();
                    return;
                }

                CreateTrayIcon();
                AddToStartup();
                Application.Run(new HiddenForm());
            }
            finally
            {
                mutex?.ReleaseMutex();
                mutex?.Dispose();
            }
        }

        private static bool IsRunAsAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity)
                   .IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void RequestAdminPrivileges()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName       = Application.ExecutablePath,
                    UseShellExecute = true,
                    Verb           = "runas",
                    Arguments      = "--elevated"
                });
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"请求管理员权限失败: {ex.Message}",
                                "权限错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void CreateTrayIcon()
        {
            trayIcon = new NotifyIcon
            {
                Icon  = SystemIcons.Shield,
                Text  = "桌面图标控制器 - Alt+Ctrl+Q",
                Visible = true
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add("调试窗口", null, (_, __) =>
            {
                debugWindow?.Show();
                debugWindow?.BringToFront();
            });
            menu.Items.Add("重启资源管理器", null, (_, __) =>
            {
                foreach (var p in Process.GetProcessesByName("explorer")) p.Kill();
                Process.Start("explorer.exe");
            });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("退出", null, (_, __) => Application.Exit());

            trayIcon.ContextMenuStrip = menu;
            trayIcon.DoubleClick += (_, __) =>
            {
                debugWindow?.Show();
                debugWindow?.BringToFront();
            };
        }

        private static void AddToStartup()
        {
            using var key = Registry.CurrentUser.OpenSubKey(REG_KEY, true);
            if (key?.GetValue(APP_NAME) == null)
                key?.SetValue(APP_NAME, $"\"{Application.ExecutablePath}\"");
        }

        private static void Log(string msg)
        {
            if (debugWindow != null) debugWindow.AddLog(msg);
        }
    }
}
