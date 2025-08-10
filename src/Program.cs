// 添加静态引用
private static DebugWindow debugWindow;

static void Main(string[] args)
{
    // 在Application.Run之前添加
    debugWindow = new DebugWindow();
    debugWindow.Show(); // 默认显示调试窗口
    
    // 在关键位置添加日志
    debugWindow.AddLog("程序启动");
    debugWindow.AddLog($"单实例检查: {(createdNew ? "通过" : "已有实例运行")}");
    
    // ... 其他代码不变 ...
    
    // 在finally块添加
    finally
    {
        debugWindow?.AddLog("程序退出");
    }
}

// 修改托盘菜单创建
private static void CreateTrayIcon()
{
    // ... 原有代码 ...
    
    // 添加调试窗口菜单项
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
}
