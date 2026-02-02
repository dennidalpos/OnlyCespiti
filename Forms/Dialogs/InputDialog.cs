using System;
using System.Drawing;
using System.Windows.Forms;

namespace GestioneCespiti.Forms.Dialogs
{
    public class InputDialog : Form
    {
        private readonly TextBox textBox;
        private readonly Label validationLabel;
        private readonly int _maxLength;

        public string InputText => textBox.Text;

        public InputDialog(string prompt, string defaultValue, int maxLength = 100)
        {
            _maxLength = maxLength;

            Text = "Input";
            Size = new Size(400, 180);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var label = new Label
            {
                Text = prompt,
                Location = new Point(10, 10),
                Size = new Size(370, 20)
            };

            textBox = new TextBox
            {
                Text = defaultValue,
                Location = new Point(10, 35),
                Size = new Size(370, 20),
                MaxLength = _maxLength
            };

            validationLabel = new Label
            {
                Text = $"{defaultValue.Length}/{_maxLength} caratteri",
                Location = new Point(10, 60),
                Size = new Size(370, 15),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8)
            };

            textBox.TextChanged += (s, e) =>
            {
                int length = textBox.Text.Length;
                validationLabel.Text = $"{length}/{_maxLength} caratteri";
                validationLabel.ForeColor = length > _maxLength * 0.9 ? Color.OrangeRed : Color.Gray;
            };

            var btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(210, 90),
                Size = new Size(80, 25)
            };

            var btnCancel = new Button
            {
                Text = "Annulla",
                DialogResult = DialogResult.Cancel,
                Location = new Point(300, 90),
                Size = new Size(80, 25)
            };

            Controls.AddRange(new Control[] { label, textBox, validationLabel, btnOk, btnCancel });
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }
    }
}
