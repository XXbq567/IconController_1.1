// src/HiddenForm.cs
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace IconController
{
    public partial class HiddenForm : Form
    {
        #region Win32 常量与 API
        private const int WM_HOTKEY   = 0x0312;
        private const int HOTKEY_ID   = 9000;
        private const int MOD_ALT     = 0x0001;
        private const int MOD_CONTROL = 0x0002;
        private const int VK_Q        = 0x51;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter,
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
            // 彻底隐藏的主窗口
            WindowState    = FormWindowState.Minimized;
            ShowInTaskbar  = false;
            Visible        = false;
            FormBorderStyle  = FormBorderStyle.None;
            Size           = new Size(0, 0);

            Load       += new EventHandler(OnFormLoad);
            FormClosing += new FormClosingEventHandler(OnFormClosing);
        }

        #region 热键注册/注销
        private void OnFormLoad(object sender, EventArgs e)
        {
            Program.Log("尝试注册热键 Alt+Ctrl+Q");
            if (!RegisterHotKey(Handle, HOTKEY_ID, MOD_ALT | MOD_CONTROL, VK_Q))
            {
                int err = Marshal.GetLastWin32Error();
                Program.Log($"热键注册失败: {err}");
                MessageBox.Show($"无法注册热键，错误码 {err}", "热键错误",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                Program.Log("热键注册成功");
            }
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterHotKey(Handle, HOTKEY_ID);
            Program.Log("热键已注销");
        }
        #endregion

        #region 消息循环
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                Program.Log("检测到热键按下");
                ToggleDesktopIcons();
            }
            base.WndProc(ref m);
        }
        #endregion

        #region 核心：隐藏/显示桌面图标
        /// <summary>获取桌面 SysListView32 句柄</summary>
        private static IntPtr GetDesktopListViewHandle()
        {
            IntPtr hShell = FindWindow("Progman", "Program Manager");
            if (hShell == IntPtr.Zero) return IntPtr.Zero;

            IntPtr hDefView = FindWindowEx(hShell, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (hDefView == IntPtr.Zero)
            {
                IntPtr worker = IntPtr.Zero;
                while ((worker = FindWindowEx(IntPtr.Zero, worker, "WorkerW", null)) != IntPtr.Zero)
                {
                    hDefView = FindWindowEx(worker, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (hDefView != IntPtr.Zero) break;
                }
            }
            return FindWindowEx(hDefView, IntPtr.Zero, "SysListView32", null);
        }

        /// <summary>立即切换图标显示状态</summary>
        private static void ToggleDesktopIcons()
        {
            IntPtr hList = GetDesktopListViewHandle();
            if (hList == IntPtr.Zero)
            {
                Program.Log("未找到桌面图标句柄");
                return;
            }

            bool visible = IsWindowVisible(hList);
            ShowWindow(hList, visible ? SW_HIDE : SW_SHOW);
            Program.Log($"桌面图标已{(visible ? "隐藏" : "显示")}");
        }
        #endregion

        protected override void SetVisibleCore(bool value) => base.SetVisibleCore(false);
    }
}
