using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;

namespace IconController
{
    public partial class HiddenForm : Form
    {
        // 常量定义保持不变...
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        
        [DllImport("shell32.dll")]
        private static extern int SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
        
        // 添加新API用于刷新桌面
        [DllImport("user32.dll")]
        private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);
        
        private const uint SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;
        private const uint SHCNF_FLUSH = 0x1000;
        
        public HiddenForm()
        {
            // 创建完全隐藏的窗口
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new System.Drawing.Size(0, 0);  // 改为0x0更安全
            
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
                int errorCode = Marshal.GetLastWin32Error();
                string errorMsg = GetHotkeyError(errorCode);
                Program.debugWindow?.AddLog($"热键注册失败! 错误代码: {errorCode} - {errorMsg}");
                
                MessageBox.Show($"无法注册热键 Alt+Ctrl+Q\n\n原因: {errorMsg}\n\n请尝试更换快捷键组合", 
                    "热键错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        // WndProc 保持不变...
        
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
                        
                        // 使用更可靠的刷新方法
                        RefreshDesktop();
                        
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
        
        // 更可靠的桌面刷新方法
        private void RefreshDesktop()
        {
            try
            {
                // 方法1: 发送桌面刷新通知
                SHChangeNotify((int)SHCNE_ASSOCCHANGED, SHCNF_IDLIST | SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
                Program.debugWindow?.AddLog("发送桌面刷新通知 (SHChangeNotify)");
                
                // 方法2: 安全重启资源管理器
                SafeRestartExplorer();
            }
            catch (Exception ex)
            {
                Program.debugWindow?.AddLog($"刷新桌面失败: {ex.Message}");
            }
        }
        
        // 安全重启资源管理器
        private void SafeRestartExplorer()
        {
            try
            {
                Program.debugWindow?.AddLog("安全重启资源管理器...");
                
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
                
                Program.debugWindow?.AddLog("资源管理器已安全重启");
            }
            catch (Exception ex)
            {
                Program.debugWindow?.AddLog($"安全重启资源管理器失败: {ex.Message}");
            }
        }
        
        // GetHotkeyError 保持不变...
        
        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false); // 永远不显示窗口
        }
    }
}
