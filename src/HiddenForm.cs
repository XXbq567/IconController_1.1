using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;

namespace IconController
{
    public partial class HiddenForm : Form
    {
        #region Win32 API
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9000;
        private const int MOD_ALT = 0x0001;
        private const int MOD_CONTROL = 0x0002;
        private const int VK_Q = 0x51;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, 
                                                 string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        #endregion

        public HiddenForm()
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(0, 0);
            
            Program.Log("隐藏窗口初始化完成");
            
            this.Load += new EventHandler(OnFormLoad);
            this.FormClosing += new FormClosingEventHandler(OnFormClosing);
        }
        
        private void OnFormLoad(object sender, EventArgs e)
        {
            Program.Log("尝试注册热键: Alt+Ctrl+Q");
            
            bool success = RegisterHotKey(this.Handle, HOTKEY_ID, MOD_ALT | MOD_CONTROL, VK_Q);
            
            if (success)
            {
                Program.Log("热键注册成功");
            }
            else
            {
                int errorCode = Marshal.GetLastWin32Error();
                string errorMsg = GetHotkeyError(errorCode);
                Program.Log($"热键注册失败! 错误代码: {errorCode} - {errorMsg}");
                
                MessageBox.Show($"无法注册热键 Alt+Ctrl+Q\n\n原因: {errorMsg}\n\n请尝试更换快捷键组合", 
                    "热键错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        
        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                UnregisterHotKey(this.Handle, HOTKEY_ID);
                Program.Log("热键已注销");
            }
            catch (Exception ex)
            {
                Program.Log($"取消注册热键异常: {ex.Message}");
            }
        }
        
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                Program.Log("检测到热键按下");
                ToggleDesktopIcons();
            }
            base.WndProc(ref m);
        }
        
        private void ToggleDesktopIcons()
        {
            Program.Log("开始切换桌面图标...");
            
            try
            {
                IntPtr desktopIconsHandle = GetDesktopIconsHandle();
                
                if (desktopIconsHandle != IntPtr.Zero)
                {
                    bool isVisible = IsWindowVisible(desktopIconsHandle);
                    ShowWindow(desktopIconsHandle, isVisible ? SW_HIDE : SW_SHOW);
                    Program.Log($"桌面图标已{(isVisible ? "隐藏" : "显示")}");
                }
                else
                {
                    Program.Log("找不到桌面图标窗口句柄");
                }
            }
            catch (Exception ex)
            {
                Program.Log($"切换桌面图标异常: {ex.Message}");
                MessageBox.Show($"操作失败: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        // 获取桌面图标窗口句柄 (GitHub 验证方案)
        private IntPtr GetDesktopIconsHandle()
        {
            // 查找主桌面窗口
            IntPtr hShell = FindWindow("Progman", "Program Manager");
            if (hShell == IntPtr.Zero)
            {
                Program.Log("找不到 Progman 窗口");
                return IntPtr.Zero;
            }
            
            // 查找桌面视图窗口
            IntPtr hDefView = FindWindowEx(hShell, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (hDefView == IntPtr.Zero)
            {
                // 多显示器兼容方案
                Program.Log("尝试多显示器兼容方案...");
                IntPtr workerW = IntPtr.Zero;
                while ((workerW = FindWindowEx(IntPtr.Zero, workerW, "WorkerW", null)) != IntPtr.Zero)
                {
                    hDefView = FindWindowEx(workerW, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (hDefView != IntPtr.Zero) break;
                }
            }
            
            if (hDefView == IntPtr.Zero)
            {
                Program.Log("找不到 SHELLDLL_DefView 窗口");
                return IntPtr.Zero;
            }
            
            // 查找桌面图标列表窗口
            IntPtr hListView = FindWindowEx(hDefView, IntPtr.Zero, "SysListView32", "FolderView");
            if (hListView == IntPtr.Zero)
            {
                Program.Log("找不到 SysListView32 窗口");
                return IntPtr.Zero;
            }
            
            Program.Log($"找到桌面图标窗口句柄: 0x{hListView.ToInt64():X}");
            return hListView;
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
            base.SetVisibleCore(false);
        }
    }
}
