using System;
using System.Drawing;
using System.Windows.Forms;

namespace GestioneCespiti.Managers
{
    public class StatusManager : IDisposable
    {
        private readonly ToolStripLabel _statusLabel;
        private System.Windows.Forms.Timer? _statusTimer;
        private readonly Form _parentForm;

        public StatusManager(ToolStrip statusStrip, Form parentForm)
        {
            _parentForm = parentForm;
            _statusLabel = new ToolStripLabel
            {
                Text = "Pronto",
                Alignment = ToolStripItemAlignment.Left
            };
            statusStrip.Items.Add(_statusLabel);
        }

        public void UpdateStatus(string message, Color color)
        {
            if (_statusLabel == null || _parentForm.IsDisposed)
                return;

            if (_parentForm.InvokeRequired)
            {
                _parentForm.BeginInvoke(new Action(() => UpdateStatus(message, color)));
                return;
            }

            if (_statusTimer != null)
            {
                _statusTimer.Stop();
                _statusTimer.Dispose();
                _statusTimer = null;
            }

            _statusLabel.Text = message;
            _statusLabel.ForeColor = color;

            _statusTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            _statusTimer.Tick += StatusTimer_Tick;
            _statusTimer.Start();
        }

        private void StatusTimer_Tick(object? sender, EventArgs e)
        {
            if (sender is not System.Windows.Forms.Timer timer)
                return;

            try
            {
                timer.Stop();

                if (!_parentForm.IsDisposed && _statusLabel != null)
                {
                    _statusLabel.Text = "Pronto";
                    _statusLabel.ForeColor = Color.Black;
                }
            }
            finally
            {
                timer.Tick -= StatusTimer_Tick;
                timer.Dispose();

                if (_statusTimer == timer)
                    _statusTimer = null;
            }
        }

        public void Dispose()
        {
            if (_statusTimer != null)
            {
                _statusTimer.Stop();
                _statusTimer.Dispose();
                _statusTimer = null;
            }
        }
    }
}
