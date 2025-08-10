using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;
using System.Drawing;

namespace IconController
{
    public partial class HiddenForm : Form
    {
        // 定义所有必需的常量
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9000;
        private const int MOD_ALT = 0x0001;
        private const int MOD_CONTROL = 0x0002;
        private const int VK_Q = 0x51;
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        
        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);
        
        [DllImport("user32.dll")]
        private static extern int GetLastError();
        
        // 正确的刷新参数
        private const int SHCNE_ASSOCCHANGED = 0x08000000;
        private const int SHCNF_IDLIST = 0x0000;
        private const int SHCNF_FLUSH = 0x1000;
        
        public HiddenForm()
        {
            // 创建完全隐藏的窗口
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(0, 0);
            
            if (Program.debugWindow != null)
            {
                Program.debugWindow.AddLog("隐藏窗口初始化完成");
            }
            
            // 注册事件处理程序
            this.Load += new EventHandler(OnFormLoad);
            this.FormClosing += new FormClosingEventHandler(OnFormClosing);
        }
        
        private void OnFormLoad(object sender, EventArgs e)
        {
            if (Program.debugWindow != null)
            {
                Program.debugWindow.AddLog("尝试注册热键: Alt+Ctrl+Q");
            }
            
            // 注册Alt+Ctrl+Q热键
            bool success = RegisterHotKey(this.Handle, HOTKEY_ID, MOD_ALT | MOD_CONTROL, VK_Q);
            
            if (success)
            {
                if (Program.debugWindow != null)
                {
                    Program.debugWindow.AddLog("热键注册成功");
                }
            }
            else
            {
                int errorCode = Marshal.GetLastWin32Error();
                string errorMsg = GetHotkeyError(errorCode);
                if (Program.debugWindow != null)
                {
                    Program.debugWindow.AddLog($"热键注册失败! 错误代码: {errorCode} - {errorMsg}");
                }
                
                MessageBox.Show($"无法注册热键 Alt+Ctrl+Q\n\n原因: {errorMsg}\n\n请尝试更换快捷键组合", 
                    "热键错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                bool success = UnregisterHotKey(this.Handle, HOTKEY_ID);
                if (Program.debugWindow != null)
                {
                    Program.debugWindow.AddLog($"热键取消注册: {(success ? "成功" : "失败")}");
                }
            }
            catch (Exception ex)
            {
                if (Program.debugWindow != null)
                {
                    Program.debugWindow.AddLog($"取消注册热键异常: {ex.Message}");
                }
            }
        }
        
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                if (Program.debugWindow != null)
                {
                    Program.debugWindow.AddLog("检测到热键按下");
                }
                ToggleDesktopIcons();
            }
            base.WndProc(ref m);
        }
        
        private void ToggleDesktopIcons()
        {
            if (Program.debugWindow != null)
            {
                Program.debugWindow.AddLog("开始切换桌面图标...");
            }
            
            try
            {
                const string regPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
                
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(regPath, true))
                {
                    if (key != null)
                    {
                        object currentValue = key.GetValue("HideIcons");
                        if (Program.debugWindow != null)
                        {
                            Program.debugWindow.AddLog($"当前 HideIcons 值: {currentValue ?? "null"}");
                        }
                        
                        int newValue = (currentValue != null && (int)currentValue == 1) ? 0 : 1;
                        if (Program.debugWindow != null)
                        {
                            Program.debugWindow.AddLog($"设置新值: {newValue}");
                        }
                        
                        key.SetValue("HideIcons", newValue, RegistryValueKind.DWord);
                        if (Program.debugWindow != null)
                        {
                            Program.debugWindow.AddLog("注册表写入成功");
                        }
                        
                        // 使用更可靠的刷新方法
                        RefreshDesktop();
                        
                        string status = newValue == 1 ? "隐藏" : "显示";
                        if (Program.debugWindow != null)
                        {
                            Program.debugWindow.AddLog($"桌面图标已{status}");
                        }
                    }
                    else
                    {
                        if (Program.debugWindow != null)
                        {
                            Program.debugWindow.AddLog("无法打开注册表项");
                        }
                        MessageBox.Show("无法访问注册表设置", "注册表错误", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                if (Program.debugWindow != null)
                {
                    Program.debugWindow.AddLog($"切换桌面图标异常: {ex.Message}");
                }
                MessageBox.Show($"操作失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        // 更可靠的桌面刷新方法
        private void RefreshDesktop()
        {
            try
            {
                // 方法1: 发送桌面刷新通知
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST | SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
                if (Program.debugWindow != null)
                {
                    Program.debugWindow.AddLog("发送桌面刷新通知 (SHChangeNotify)");
                }
                
                // 方法2: 安全重启资源管理器
                SafeRestartExplorer();
            }
            catch (Exception ex)
            {
                if (Program.debugWindow != null)
                {
                    Program.debugWindow.AddLog($"刷新桌面失败: {ex.Message}");
                }
            }
        }
        
        // 安全重启资源管理器
        private void SafeRestartExplorer()
        {
            try
            {
                if (Program.debugWindow != null)
                {
                    Program.debugWindow.AddLog("安全重启资源管理器...");
                }
                
                // 只重启桌面进程，不影响其他资源管理器窗口
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = "/c taskkill /f /im explorer.exe && start explorer.exe";
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    process.WaitForExit(5000);
                }
                
                if (Program.debugWindow != null)
                {
                    Program.debugWindow.AddLog("资源管理器已安全重启");
                }
            }
            catch (Exception ex)
            {
                if (Program.debugWindow != null)
                {
                    Program.debugWindow.AddLog($"安全重启资源管理器失败: {ex.Message}");
                }
            }
        }
        
        private string GetHotkeyError(int errorCode)
        {
            switch (errorCode)
            {
                case 1409: return "热键已被其他程序占用";
                case 5: return "访问被拒绝（需要管理员权限）";
                case 87: return "无效参数";
                case 1410: return "类已存在";
                case 1411: return "类不存在";
                case 1412: return "窗口不存在";
                default: return $"未知错误 (代码: {errorCode})";
            }
        }
        
        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false); // 永远不显示窗口
        }
    }
}
