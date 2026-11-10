using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace GestioneCespiti.Forms.Dialogs
{
    public class OptionsDialog : Form
    {
        private readonly ListBox listBox;
        private readonly List<string> _targetOptions;
        private readonly List<string> _workingOptions;
        private readonly Func<string, bool>? _isProtectedOption;

        public OptionsDialog(string title, string labelText, List<string> options, Func<string, bool>? isProtectedOption = null)
        {
            _targetOptions = options;
            _isProtectedOption = isProtectedOption;
            _workingOptions = options
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            Text = title;
            Size = new Size(450, 350);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var label = new Label
            {
                Text = labelText,
                Location = new Point(10, 10),
                Size = new Size(420, 20)
            };

            listBox = new ListBox
            {
                Location = new Point(10, 35),
                Size = new Size(420, 200)
            };

            RefreshList();

            var btnAdd = new Button
            {
                Text = "Aggiungi",
                Location = new Point(10, 245),
                Size = new Size(100, 30)
            };
            btnAdd.Click += BtnAdd_Click;

            var btnRemove = new Button
            {
                Text = "Rimuovi",
                Location = new Point(120, 245),
                Size = new Size(100, 30)
            };
            btnRemove.Click += BtnRemove_Click;

            var btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(250, 245),
                Size = new Size(80, 30)
            };

            var btnCancel = new Button
            {
                Text = "Annulla",
                DialogResult = DialogResult.Cancel,
                Location = new Point(340, 245),
                Size = new Size(90, 30)
            };

            Controls.AddRange(new Control[] { label, listBox, btnAdd, btnRemove, btnOk, btnCancel });
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                _targetOptions.Clear();
                _targetOptions.AddRange(_workingOptions);
            }

            base.OnFormClosing(e);
        }

        private void RefreshList()
        {
            listBox.BeginUpdate();
            listBox.Items.Clear();
            foreach (var option in _workingOptions)
            {
                listBox.Items.Add(option);
            }
            listBox.EndUpdate();
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            using (var inputDialog = new InputDialog("Inserisci nuova opzione:", "", 100))
            {
                if (inputDialog.ShowDialog() == DialogResult.OK)
                {
                    string newOption = inputDialog.InputText.Trim();
                    if (!string.IsNullOrWhiteSpace(newOption) &&
                        !_workingOptions.Contains(newOption, StringComparer.OrdinalIgnoreCase))
                    {
                        _workingOptions.Add(newOption);
                        RefreshList();
                    }
                }
            }
        }

        private void BtnRemove_Click(object? sender, EventArgs e)
        {
            if (listBox.SelectedItem == null)
                return;

            string selected = listBox.SelectedItem.ToString()!;

            if (_isProtectedOption != null && _isProtectedOption(selected))
            {
                MessageBox.Show(
                    $"Non puoi rimuovere l'opzione di default '{selected}'.",
                    "Opzione Protetta",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Vuoi davvero rimuovere l'opzione '{selected}'?",
                "Conferma Rimozione",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _workingOptions.RemoveAll(option => option.Equals(selected, StringComparison.OrdinalIgnoreCase));
                RefreshList();
            }
        }
    }
}
