using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
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
        // Win32 API
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        
        private static readonly string APP_NAME = "IconController";
        private static readonly string REG_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        
        private static Mutex mutex;
        private static NotifyIcon trayIcon;
        public static DebugWindow debugWindow;
        
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                // 隐藏控制台窗口
                IntPtr consoleWindow = GetConsoleWindow();
                if (consoleWindow != IntPtr.Zero)
                {
                    ShowWindow(consoleWindow, SW_HIDE);
                }
                
                // 创建调试窗口
                debugWindow = new DebugWindow();
                Log("=== IconController 启动 ===");
                Log($"版本: {Application.ProductVersion}");
                Log($"系统: {Environment.OSVersion.VersionString}");
                
                // 检查是否已经是提升的实例
                bool isElevated = args.Contains("--elevated");
                Log($"启动参数: {string.Join(" ", args)}");
                Log($"提升权限启动: {(isElevated ? "是" : "否")}");
                
                // 单实例检查
                mutex = new Mutex(true, "Global\\" + APP_NAME, out bool createdNew);
                if (!createdNew)
                {
                    Log("错误: 程序已在运行中");
                    MessageBox.Show("程序已在运行中，请检查系统托盘", APP_NAME, 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                Log("单实例检查通过");
                
                // 检查管理员权限（如果是通过--elevated启动则跳过）
                if (!isElevated && !IsRunAsAdmin())
                {
                    Log("需要管理员权限，请求提升...");
                    RequestAdminPrivileges();
                    return;
                }
                
                Log($"管理员权限: {(IsRunAsAdmin() ? "是" : "否")}");
                
                // 创建托盘图标
                CreateTrayIcon();
                
                // 添加到开机启动
                AddToStartup();
                
                Log("创建隐藏窗口...");
                
                // 创建隐藏窗口并运行
                Application.Run(new HiddenForm());
            }
            catch (Exception ex)
            {
                string error = $"启动失败: {ex.Message}\n\n{ex.StackTrace}";
                Log(error);
                MessageBox.Show(error, "严重错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Log("程序退出");
                
                // 修复空条件赋值问题
                if (trayIcon != null)
                {
                    trayIcon.Visible = false;
                    trayIcon.Dispose();
                }
                
                if (mutex != null)
                {
                    mutex.ReleaseMutex();
                    mutex.Dispose();
                }
            }
        }
        
        public static void Log(string message)
        {
            if (debugWindow != null)
            {
                debugWindow.AddLog(message);
            }
        }
        
        // 检查是否以管理员权限运行
        private static bool IsRunAsAdmin()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }
        
        // 请求管理员权限
        private static void RequestAdminPrivileges()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    UseShellExecute = true,
                    Verb = "runas",
                    Arguments = "--elevated"
                };
                
                Process.Start(startInfo);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Log($"请求管理员权限失败: {ex.Message}");
                MessageBox.Show($"请求管理员权限失败: {ex.Message}\n\n请右键点击程序，选择'以管理员身份运行'", 
                    "权限错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private static void CreateTrayIcon()
        {
            try
            {
                trayIcon = new NotifyIcon();
                trayIcon.Icon = SystemIcons.Shield;
                trayIcon.Text = "桌面图标控制器 - Alt+Ctrl+Q";
                trayIcon.Visible = true;
                
                // 右键菜单
                ContextMenuStrip menu = new ContextMenuStrip();
                
                ToolStripMenuItem debugItem = new ToolStripMenuItem("调试窗口");
                debugItem.Click += (s, e) => 
                {
                    if (debugWindow != null)
                    {
                        debugWindow.Show();
                        debugWindow.BringToFront();
                    }
                };
                menu.Items.Add(debugItem);
                
                ToolStripMenuItem restartItem = new ToolStripMenuItem("重启资源管理器");
                restartItem.Click += (s, e) => 
                {
                    try
                    {
                        Log("手动重启资源管理器...");
                        foreach (var process in Process.GetProcessesByName("explorer"))
                        {
                            process.Kill();
                        }
                        Process.Start("explorer.exe");
                        Log("资源管理器已重启");
                    }
                    catch (Exception ex)
                    {
                        Log($"重启资源管理器失败: {ex.Message}");
                    }
                };
                menu.Items.Add(restartItem);
                
                menu.Items.Add(new ToolStripSeparator());
                
                ToolStripMenuItem exitItem = new ToolStripMenuItem("退出");
                exitItem.Click += (s, e) => Application.Exit();
                menu.Items.Add(exitItem);
                
                trayIcon.ContextMenuStrip = menu;
                
                // 双击托盘图标显示调试窗口
                trayIcon.DoubleClick += (s, e) => 
                {
                    if (debugWindow != null)
                    {
                        debugWindow.Show();
                        debugWindow.BringToFront();
                    }
                };
                
                Log("托盘图标创建成功");
            }
            catch (Exception ex)
            {
                Log($"创建托盘图标失败: {ex.Message}");
            }
        }
        
        private static void AddToStartup()
        {
            try
            {
                string exePath = Application.ExecutablePath;
                Log($"程序路径: {exePath}");
                
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REG_KEY, true))
                {
                    if (key != null)
                    {
                        if (key.GetValue(APP_NAME) == null)
                        {
                            key.SetValue(APP_NAME, $"\"{exePath}\"");
                            Log("已添加到开机启动");
                        }
                        else
                        {
                            Log("开机启动项已存在");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"添加开机启动失败: {ex.Message}");
            }
        }
    }
}
