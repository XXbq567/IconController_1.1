using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;

namespace IconController
{
    public partial class HiddenForm : Form
    {
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9000;
        private const int MOD_ALT = 0x0001;
        private const int MOD_CONTROL = 0x0002;
        private const int VK_Q = 0x51;
        
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);
        
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        
        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);
        
        [DllImport("user32.dll")]
        private static extern int GetLastError();
        
        public HiddenForm()
        {
            // 创建完全隐藏的窗口
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new System.Drawing.Size(1, 1);
            
            Program.debugWindow?.AddLog("隐藏窗口初始化完成");
            
            // 注册热键
            this.Load += OnFormLoad;
            this.FormClosing += OnFormClosing;
        }
        
        private void OnFormLoad(object sender, EventArgs e)
        {
            Program.debugWindow?.AddLog("尝试注册热键: Alt+Ctrl+Q");
            
            // 注册Alt+Ctrl+Q热键
            bool success = RegisterHotKey(this.Handle, HOTKEY_ID, MOD_ALT | MOD_CONTROL, VK_Q);
            
            if (success)
            {
                Program.debugWindow?.AddLog("热键注册成功");
            }
            else
            {
                int errorCode = GetLastError();
                string errorMsg = GetHotkeyError(errorCode);
                Program.debugWindow?.AddLog($"热键注册失败! 错误代码: {errorCode} - {errorMsg}");
                
                MessageBox.Show($"无法注册热键 Alt+Ctrl+Q\n\n原因: {errorMsg}\n\n请尝试更换快捷键组合", 
                    "热键错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                bool success = UnregisterHotKey(this.Handle, HOTKEY_ID);
                Program.debugWindow?.AddLog($"热键取消注册: {(success ? "成功" : "失败")}");
            }
            catch (Exception ex)
            {
                Program.debugWindow?.AddLog($"取消注册热键异常: {ex.Message}");
            }
        }
        
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                Program.debugWindow?.AddLog("检测到热键按下");
                ToggleDesktopIcons();
            }
            base.WndProc(ref m);
        }
        
        private void ToggleDesktopIcons()
        {
            Program.debugWindow?.AddLog("开始切换桌面图标...");
            
            try
            {
                const string regPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
                
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(regPath, true))
                {
                    if (key != null)
                    {
                        object currentValue = key.GetValue("HideIcons");
                        Program.debugWindow?.AddLog($"当前 HideIcons 值: {currentValue ?? "null"}");
                        
                        int newValue = (currentValue != null && (int)currentValue == 1) ? 0 : 1;
                        Program.debugWindow?.AddLog($"设置新值: {newValue}");
                        
                        key.SetValue("HideIcons", newValue, RegistryValueKind.DWord);
                        Program.debugWindow?.AddLog("注册表写入成功");
                        
                        // 刷新桌面
                        SHChangeNotify(0x8000000, 0x1000, IntPtr.Zero, IntPtr.Zero);
                        Program.debugWindow?.AddLog("发送桌面刷新通知");
                        
                        // 强制刷新资源管理器
                        RefreshExplorer();
                        
                        string status = newValue == 1 ? "隐藏" : "显示";
                        Program.debugWindow?.AddLog($"桌面图标已{status}");
                    }
                    else
                    {
                        Program.debugWindow?.AddLog("无法打开注册表项");
                        MessageBox.Show("无法访问注册表设置", "注册表错误", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.debugWindow?.AddLog($"切换桌面图标异常: {ex.Message}");
                MessageBox.Show($"操作失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void RefreshExplorer()
        {
            try
            {
                Program.debugWindow?.AddLog("尝试刷新资源管理器...");
                
                // 方法1: 发送刷新通知
                SHChangeNotify(0x8000000, 0x1000, IntPtr.Zero, IntPtr.Zero);
                
                // 方法2: 重启资源管理器
                foreach (var process in Process.GetProcessesByName("explorer"))
                {
                    process.Kill();
                }
                
                // 稍等片刻再启动
                System.Threading.Thread.Sleep(1000);
                Process.Start("explorer.exe");
                
                Program.debugWindow?.AddLog("资源管理器已重启");
            }
            catch (Exception ex)
            {
                Program.debugWindow?.AddLog($"刷新资源管理器失败: {ex.Message}");
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
