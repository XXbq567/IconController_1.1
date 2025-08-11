using System;
using System.Windows.Forms;
using System.Drawing;

namespace IconController
{
    public class DebugWindow : Form
    {
        private TextBox logBox;
        
        public DebugWindow()
        {
            this.Size = new Size(500, 400);
            this.Text = "IconController 调试信息";
            this.StartPosition = FormStartPosition.CenterScreen;
            
            logBox = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 10),
                BackColor = Color.Black,
                ForeColor = Color.Lime
            };
            
            this.Controls.Add(logBox);
            
            // 添加复制按钮
            Button copyButton = new Button
            {
                Text = "复制日志",
                Dock = DockStyle.Bottom,
                Height = 30
            };
            copyButton.Click += (s, e) => 
            {
                if (!string.IsNullOrEmpty(logBox.Text))
                {
                    Clipboard.SetText(logBox.Text);
                    AddLog("日志已复制到剪贴板");
                }
            };
            
            this.Controls.Add(copyButton);
        }
        
        public void AddLog(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AddLog(message)));
                return;
            }
            
            logBox.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {message}\r\n");
            logBox.SelectionStart = logBox.TextLength;
            logBox.ScrollToCaret();
        }
        
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
            base.OnFormClosing(e);
        }
    }
}
