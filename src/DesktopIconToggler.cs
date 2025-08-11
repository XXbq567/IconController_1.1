// src/DesktopIconToggler.cs
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace IconController
{
    internal static class DesktopIconToggler
    {
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);

        // 找到桌面 ListView
        private static IntPtr GetDesktopListViewHandle()
        {
            IntPtr hShell = FindWindow("Progman", "Program Manager");
            if (hShell == IntPtr.Zero) return IntPtr.Zero;

            IntPtr hDefView = FindWindowEx(hShell, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (hDefView == IntPtr.Zero)
            {
                // Win10/11 多显示器/虚拟桌面兼容
                IntPtr worker = IntPtr.Zero;
                while ((worker = FindWindowEx(IntPtr.Zero, worker, "WorkerW", null)) != IntPtr.Zero)
                {
                    hDefView = FindWindowEx(worker, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (hDefView != IntPtr.Zero) break;
                }
            }
            return FindWindowEx(hDefView, IntPtr.Zero, "SysListView32", null);
        }

        private static IntPtr FindWindow(string lpClassName, string lpWindowName)
            => FindWindowNative(lpClassName, lpWindowName);
        private static IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter,
                                           string lpszClass, string lpszWindow)
            => FindWindowExNative(hWndParent, hWndChildAfter, lpszClass, lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowNative(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowExNative(IntPtr hWndParent, IntPtr hWndChildAfter,
                                                        string lpszClass, string lpszWindow);

        /// <summary>立即切换桌面图标显示状态，返回新的可见状态</summary>
        public static bool Toggle()
        {
            IntPtr hList = GetDesktopListViewHandle();
            if (hList == IntPtr.Zero) return false;

            bool visible = IsWindowVisible(hList);
            ShowWindow(hList, visible ? SW_HIDE : SW_SHOW);
            return !visible;
        }
    }
}
