using System;
using System.Windows.Forms;
using System.Drawing;

namespace IconController
{
    public class DebugWindow : Form
    {
        private readonly TextBox logBox;

        public DebugWindow()
        {
            Text = "IconController 调试信息";
            Size = new Size(500, 400);
            StartPosition = FormStartPosition.CenterScreen;

            logBox = new TextBox
            {
                Multiline = true,
                Dock      = DockStyle.Fill,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly   = true,
                Font       = new Font("Consolas", 10),
                BackColor  = Color.Black,
                ForeColor  = Color.Lime
            };
            Controls.Add(logBox);

            var copyBtn = new Button { Text = "复制日志", Dock = DockStyle.Bottom, Height = 30 };
            copyBtn.Click += (_, __) =>
            {
                if (!string.IsNullOrEmpty(logBox.Text))
                {
                    Clipboard.SetText(logBox.Text);
                    AddLog("日志已复制到剪贴板");
                }
            };
            Controls.Add(copyBtn);
        }

        public void AddLog(string msg)
        {
            if (InvokeRequired) { Invoke(new Action(() => AddLog(msg))); return; }
            logBox.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {msg}\r\n");
            logBox.SelectionStart = logBox.TextLength;
            logBox.ScrollToCaret();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            e.Cancel = true;
            Hide();
            base.OnFormClosing(e);
        }
    }
}
