private void OnFormLoad(object sender, EventArgs e)
{
    bool success = RegisterHotKey(this.Handle, HOTKEY_ID, MOD_ALT | MOD_CONTROL, VK_Q);
    
    // 添加热键注册结果日志
    Program.debugWindow?.AddLog(success ? 
        "热键注册成功" : 
        $"热键注册失败! 错误代码: {Marshal.GetLastWin32Error()}");
}

private void ToggleDesktopIcons()
{
    Program.debugWindow?.AddLog("开始切换桌面图标");
    
    try
    {
        // ... 注册表操作代码 ...
        
        // 添加操作结果日志
        Program.debugWindow?.AddLog($"设置HideIcons={newValue}");
        
        // 增强刷新机制:cite[8]
        SHChangeNotify(0x8000000, 0x1000, IntPtr.Zero, IntPtr.Zero);
        RefreshExplorer();
    }
    catch (Exception ex)
    {
        Program.debugWindow?.AddLog($"操作失败: {ex.Message}");
    }
}

// 添加增强的刷新方法
private void RefreshExplorer()
{
    try
    {
        foreach (var process in Process.GetProcessesByName("explorer"))
        {
            process.Kill();
        }
        Process.Start("explorer.exe");
        Program.debugWindow?.AddLog("资源管理器已重启");
    }
    catch (Exception ex)
    {
        Program.debugWindow?.AddLog($"重启资源管理器失败: {ex.Message}");
    }
}
