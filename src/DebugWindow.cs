using System.Windows.Forms;
using System.Drawing;

namespace IconController
{
    public class DebugWindow : Form
    {
        private TextBox logBox;
        
        public DebugWindow()
        {
            this.Size = new Size(400, 300);
            this.Text = "IconController Debug";
            
            logBox = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true
            };
            
            this.Controls.Add(logBox);
        }
        
        public void AddLog(string message)
        {
            logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
        }
    }
}
