using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GestioneCespiti.Models;
using GestioneCespiti.Services;

namespace GestioneCespiti.Forms.Dialogs
{
    public class ArchiveDialog : Form
    {
        private readonly ListBox listBox;
        private readonly DataPersistenceService _persistenceService;
        private readonly bool _isReadOnly;

        public ArchiveDialog(List<AssetSheet> archivedSheets, DataPersistenceService persistenceService, bool isReadOnly)
        {
            _persistenceService = persistenceService;
            _isReadOnly = isReadOnly;

            Text = "Fogli Archiviati";
            Size = new Size(500, 400);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var label = new Label
            {
                Text = _isReadOnly ? "Fogli archiviati (sola lettura):" : "Seleziona un foglio da ripristinare o eliminare:",
                Location = new Point(10, 10),
                Size = new Size(470, 20)
            };

            listBox = new ListBox
            {
                Location = new Point(10, 35),
                Size = new Size(470, 250),
                DisplayMember = "Header"
            };

            foreach (var sheet in archivedSheets)
            {
                listBox.Items.Add(sheet);
            }

            var btnRestore = new Button
            {
                Text = "Ripristina",
                Location = new Point(10, 300),
                Size = new Size(100, 30),
                Enabled = !_isReadOnly
            };
            btnRestore.Click += (s, e) => RestoreSheet();

            var btnDelete = new Button
            {
                Text = "Elimina",
                Location = new Point(120, 300),
                Size = new Size(100, 30),
                Enabled = !_isReadOnly
            };
            btnDelete.Click += (s, e) => DeleteSheet();

            var btnClose = new Button
            {
                Text = "Chiudi",
                DialogResult = DialogResult.OK,
                Location = new Point(380, 300),
                Size = new Size(100, 30)
            };

            Controls.AddRange(new Control[] { label, listBox, btnRestore, btnDelete, btnClose });
        }

        private void RestoreSheet()
        {
            if (_isReadOnly) return;

            if (listBox.SelectedItem is AssetSheet sheet)
            {
                var result = MessageBox.Show($"Vuoi ripristinare il foglio '{sheet.Header}'?", "Conferma", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    try
                    {
                        _persistenceService.UnarchiveSheet(sheet);
                        listBox.Items.Remove(sheet);
                        MessageBox.Show("Foglio ripristinato con successo.", "Successo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Errore ripristino foglio archiviato", ex);
                        MessageBox.Show($"Errore: {ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void DeleteSheet()
        {
            if (_isReadOnly) return;

            if (listBox.SelectedItem is AssetSheet sheet)
            {
                var result = MessageBox.Show($"Vuoi eliminare definitivamente il foglio '{sheet.Header}'?", "Conferma", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.Yes)
                {
                    try
                    {
                        _persistenceService.DeleteSheet(sheet);
                        listBox.Items.Remove(sheet);
                        MessageBox.Show("Foglio eliminato con successo.", "Successo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Errore eliminazione foglio archiviato", ex);
                        MessageBox.Show($"Errore: {ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
