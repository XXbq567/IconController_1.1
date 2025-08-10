using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

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
        
        public HiddenForm()
        {
            // 创建完全隐藏的窗口
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new System.Drawing.Size(1, 1);
            
            // 注册热键
            this.Load += OnFormLoad;
            this.FormClosing += OnFormClosing;
        }
        
        private void OnFormLoad(object sender, EventArgs e)
        {
            // 注册Alt+Ctrl+Q热键
            RegisterHotKey(this.Handle, HOTKEY_ID, MOD_ALT | MOD_CONTROL, VK_Q);
        }
        
        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterHotKey(this.Handle, HOTKEY_ID);
        }
        
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                ToggleDesktopIcons();
            }
            base.WndProc(ref m);
        }
        
        private void ToggleDesktopIcons()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true))
                {
                    if (key != null)
                    {
                        object currentValue = key.GetValue("HideIcons");
                        int newValue = (currentValue != null && (int)currentValue == 1) ? 0 : 1;
                        
                        key.SetValue("HideIcons", newValue, RegistryValueKind.DWord);
                        
                        // 刷新桌面
                        SHChangeNotify(0x8000000, 0x1000, IntPtr.Zero, IntPtr.Zero);
                    }
                }
            }
            catch { }
        }
        
        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false); // 永远不显示窗口
        }
    }
}
