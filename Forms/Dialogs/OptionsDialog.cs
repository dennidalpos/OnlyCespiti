using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GestioneCespiti.Models;

namespace GestioneCespiti.Forms.Dialogs
{
    public class OptionsDialog : Form
    {
        private readonly ListBox listBox;
        private readonly AppSettings _settings;

        public OptionsDialog(AppSettings settings)
        {
            _settings = settings;

            _settings.CauseDismissioneOptions = _settings.CauseDismissioneOptions
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            Text = "Gestisci Opzioni Causa Dismissione";
            Size = new Size(450, 350);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var label = new Label
            {
                Text = "Opzioni disponibili per 'Causa dismissione':",
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

        private void RefreshList()
        {
            listBox.BeginUpdate();
            listBox.Items.Clear();
            foreach (var option in _settings.CauseDismissioneOptions)
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
                        !_settings.CauseDismissioneOptions.Contains(newOption, StringComparer.OrdinalIgnoreCase))
                    {
                        _settings.CauseDismissioneOptions.Add(newOption);
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

            if (AppSettings.IsDefaultOption(selected))
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
                _settings.CauseDismissioneOptions.Remove(selected);
                RefreshList();
            }
        }
    }
}
