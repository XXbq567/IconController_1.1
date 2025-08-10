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
        private const int WM_HOTKEY      = 0x0312;
        private const int HOTKEY_ID      = 9000;
        private const int MOD_ALT        = 0x0001;
        private const int MOD_CONTROL    = 0x0002;
        private const int VK_Q           = 0x51;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private const int SHCNE_ASSOCCHANGED = 0x08000000;
        private const int SHCNF_IDLIST       = 0x0000;
        private const int SHCNF_FLUSH        = 0x1000;

        public HiddenForm()
        {
            WindowState    = FormWindowState.Minimized;
            ShowInTaskbar  = false;
            Visible        = false;
            FormBorderStyle  = FormBorderStyle.None;
            Size           = new Size(0, 0);

            Load       += new EventHandler(OnFormLoad);
            FormClosing += new FormClosingEventHandler(OnFormClosing);
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            Program.Log("尝试注册热键: Alt+Ctrl+Q");
            bool ok = RegisterHotKey(Handle, HOTKEY_ID, MOD_ALT | MOD_CONTROL, VK_Q);
            if (!ok)
            {
                int err = Marshal.GetLastWin32Error();
                Program.Log($"热键注册失败! {GetHotkeyError(err)}");
                MessageBox.Show($"无法注册热键 Alt+Ctrl+Q ({GetHotkeyError(err)})",
                                "热键错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else Program.Log("热键注册成功");
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterHotKey(Handle, HOTKEY_ID);
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
                const string path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
                using var key = Registry.CurrentUser.OpenSubKey(path, true);
                if (key == null) return;

                int cur = (int)(key.GetValue("HideIcons") ?? 0);
                int nxt = cur == 1 ? 0 : 1;
                key.SetValue("HideIcons", nxt, RegistryValueKind.DWord);

                RefreshDesktop();
                Program.Log($"桌面图标已{(nxt == 1 ? "隐藏" : "显示")}");
            }
            catch (Exception ex)
            {
                Program.Log($"切换异常: {ex.Message}");
                MessageBox.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshDesktop()
        {
            try
            {
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST | SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
                Program.Log("发送桌面刷新通知");

                using var p = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd",
                        Arguments = "/c taskkill /f /im explorer.exe && start explorer.exe",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true
                    }
                };
                p.Start();
                p.WaitForExit(5000);
                Program.Log("资源管理器已重启");
            }
            catch (Exception ex)
            {
                Program.Log($"刷新失败: {ex.Message}");
            }
        }

        private string GetHotkeyError(int code) => code switch
        {
            1409 => "热键已被占用",
            5    => "需要管理员权限",
            _    => $"未知错误 ({code})"
        };

        protected override void SetVisibleCore(bool value) => base.SetVisibleCore(false);
    }
}
