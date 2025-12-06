using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using GestioneCespiti.Models;
using GestioneCespiti.Services;

namespace GestioneCespiti
{
    public partial class MainForm : Form
    {
        private readonly DataPersistenceService _persistenceService;
        private readonly ExcelExportService _excelService;
        private readonly SettingsService _settingsService;
        private readonly LockService _lockService;
        private AppSettings _appSettings;
        private bool _isReadOnly;
        private System.Threading.Timer? _saveTimer;
        private readonly object _saveLock = new object();
        private ToolStripLabel? _statusLabel;
        private System.Windows.Forms.Timer? _statusTimer;
        
        private List<SearchResult> _searchResults = new List<SearchResult>();
        private int _currentSearchIndex = -1;
        private bool _hasUnsavedChanges = false;

        public MainForm()
        {
            InitializeComponent();
            _persistenceService = new DataPersistenceService();
            _excelService = new ExcelExportService();
            _settingsService = new SettingsService();
            _lockService = new LockService();
            _appSettings = _settingsService.LoadSettings();

            InitializeStatusLabel();
            CheckApplicationLock();
            LoadAllSheets();

            if (tabControl?.TabCount == 0 && !_isReadOnly)
            {
                CreateNewSheet();
            }

            UpdateUIForReadOnlyMode();
        }

        private void InitializeStatusLabel()
        {
            _statusLabel = new ToolStripLabel
            {
                Text = "Pronto",
                Alignment = ToolStripItemAlignment.Left
            };
            statusStrip.Items.Add(_statusLabel);
        }

        private void CheckApplicationLock()
        {
            if (_lockService.TryAcquireLock())
            {
                _isReadOnly = false;
            }
            else
            {
                var lockInfo = _lockService.GetCurrentLock();
                if (lockInfo != null)
                {
                    string message = $"L'applicazione è già in uso da:\n\n" +
                                   $"Utente: {lockInfo.UserName}\n" +
                                   $"Computer: {lockInfo.HostName}\n" +
                                   $"Dal: {lockInfo.LockTime:dd/MM/yyyy HH:mm:ss}\n\n" +
                                   $"L'applicazione sarà aperta in modalità SOLA LETTURA.";

                    MessageBox.Show(message, "Applicazione in Uso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                _isReadOnly = true;
            }
        }

        private void UpdateUIForReadOnlyMode()
        {
            if (_isReadOnly)
            {
                this.Text = "Gestione Cespiti - Dismissioni [SOLA LETTURA]";
                menuNewSheet.Enabled = false;
                menuSave.Enabled = false;
                menuAddRow.Enabled = false;
                menuRemoveRow.Enabled = false;
                menuAddColumn.Enabled = false;
                menuRemoveColumn.Enabled = false;
                menuDeleteSheet.Enabled = false;
                menuArchiveSheet.Enabled = false;
                menuRenameSheet.Enabled = false;
                menuManageOptions.Enabled = false;

                var readOnlyLabel = new ToolStripLabel
                {
                    Text = "⚠ MODALITÀ SOLA LETTURA",
                    ForeColor = Color.Red,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold)
                };
                statusStrip.Items.Add(new ToolStripSeparator());
                statusStrip.Items.Add(readOnlyLabel);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Logger.LogInfo("Chiusura applicazione in corso...");

            if (_hasUnsavedChanges && !_isReadOnly)
            {
                var result = MessageBox.Show(
                    "Ci sono modifiche non salvate.\nVuoi salvare prima di uscire?",
                    "Modifiche Non Salvate",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    Logger.LogInfo("Chiusura annullata dall'utente (modifiche non salvate)");
                    return;
                }

                if (result == DialogResult.Yes)
                {
                    bool allSaved = true;
                    var errors = new List<string>();

                    foreach (TabPage tab in tabControl.TabPages)
                    {
                        if (tab.Tag is AssetSheet sheet)
                        {
                            try
                            {
                                _persistenceService.SaveSheet(sheet);
                                Logger.LogInfo($"Foglio '{sheet.Header}' salvato alla chiusura");
                            }
                            catch (Exception ex)
                            {
                                allSaved = false;
                                string error = $"Foglio '{sheet.Header}': {ex.Message}";
                                errors.Add(error);
                                Logger.LogError($"Errore salvataggio '{sheet.Header}' alla chiusura", ex);
                            }
                        }
                    }

                    if (!allSaved)
                    {
                        string errorMessage = "Attenzione: alcuni fogli non sono stati salvati:\n\n" +
                                            string.Join("\n", errors) +
                                            "\n\nVuoi comunque chiudere l'applicazione?";

                        var confirmResult = MessageBox.Show(
                            errorMessage,
                            "Errori di Salvataggio",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);

                        if (confirmResult == DialogResult.No)
                        {
                            e.Cancel = true;
                            Logger.LogInfo("Chiusura annullata dall'utente (errori salvataggio)");
                            return;
                        }
                    }
                }
            }

            lock (_saveLock)
            {
                _saveTimer?.Dispose();
                _saveTimer = null;
            }

            if (_statusTimer != null)
            {
                _statusTimer.Stop();
                _statusTimer.Dispose();
                _statusTimer = null;
            }

            if (!_isReadOnly)
            {
                _lockService.ReleaseLock();
                Logger.LogInfo("Lock rilasciato");
            }

            Logger.LogInfo("Applicazione chiusa");
            base.OnFormClosing(e);
        }

        private void searchTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                
                if (_searchResults.Count > 0)
                {
                    searchNextButton_Click(sender, e);
                }
                else
                {
                    searchButton_Click(sender, e);
                }
            }
        }

        private void searchButton_Click(object? sender, EventArgs e)
        {
            string searchText = searchTextBox.Text?.Trim() ?? string.Empty;
            
            if (string.IsNullOrEmpty(searchText))
            {
                MessageBox.Show("Inserisci un testo da cercare.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            searchTextBox.BackColor = Color.LightYellow;

            _searchResults.Clear();
            _currentSearchIndex = -1;

            var allSheets = _persistenceService.LoadAllSheets(true);

            foreach (var sheet in allSheets)
            {
                for (int rowIndex = 0; rowIndex < sheet.Rows.Count; rowIndex++)
                {
                    var asset = sheet.Rows[rowIndex];
                    foreach (var column in sheet.Columns)
                    {
                        string value = asset[column];
                        if (value.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _searchResults.Add(new SearchResult
                            {
                                Sheet = sheet,
                                RowIndex = rowIndex,
                                ColumnName = column,
                                Value = value
                            });
                        }
                    }
                }
            }

            if (_searchResults.Count == 0)
            {
                MessageBox.Show($"Nessun risultato trovato per '{searchText}'.", "Ricerca", MessageBoxButtons.OK, MessageBoxIcon.Information);
                searchNextButton.Visible = false;
                searchTextBox.BackColor = SystemColors.Window;
                return;
            }

            if (_searchResults.Count > 1)
            {
                searchNextButton.Visible = true;
            }
            else
            {
                searchNextButton.Visible = false;
            }

            _currentSearchIndex = 0;
            NavigateToSearchResult(_searchResults[0]);
            UpdateStatus($"Trovati {_searchResults.Count} risultati (1/{_searchResults.Count})", Color.Blue);
        }

        private void searchNextButton_Click(object? sender, EventArgs e)
        {
            if (_searchResults.Count == 0)
                return;

            _currentSearchIndex++;
            if (_currentSearchIndex >= _searchResults.Count)
            {
                _currentSearchIndex = 0;
            }

            NavigateToSearchResult(_searchResults[_currentSearchIndex]);
            UpdateStatus($"Risultato {_currentSearchIndex + 1}/{_searchResults.Count}", Color.Blue);
        }

        private void NavigateToSearchResult(SearchResult result)
        {
            var existingTab = FindTabForSheet(result.Sheet);

            if (existingTab == null)
            {
                if (result.Sheet.IsArchived)
                {
                    var confirmResult = MessageBox.Show(
                        $"Il foglio '{result.Sheet.Header}' è archiviato.\nVuoi caricarlo?",
                        "Foglio Archiviato",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (confirmResult == DialogResult.Yes)
                    {
                        AddSheetTab(result.Sheet);
                        existingTab = FindTabForSheet(result.Sheet);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    AddSheetTab(result.Sheet);
                    existingTab = FindTabForSheet(result.Sheet);
                }
            }

            if (existingTab != null)
            {
                tabControl.SelectedTab = existingTab;

                var grid = GetGridFromTab(existingTab);
                if (grid != null)
                {
                    int colIndex = result.Sheet.Columns.IndexOf(result.ColumnName);
                    if (colIndex >= 0 && result.RowIndex < grid.Rows.Count)
                    {
                        grid.ClearSelection();
                        grid.Rows[result.RowIndex].Cells[colIndex].Selected = true;
                        grid.FirstDisplayedScrollingRowIndex = result.RowIndex;
                        grid.Focus();
                    }
                }
            }
        }

        private TabPage? FindTabForSheet(AssetSheet sheet)
        {
            foreach (TabPage tab in tabControl.TabPages)
            {
                if (tab.Tag is AssetSheet tabSheet && tabSheet.FileName == sheet.FileName)
                {
                    return tab;
                }
            }
            return null;
        }

        private DataGridView? GetGridFromTab(TabPage tab)
        {
            foreach (Control control in tab.Controls)
            {
                if (control is Panel panel)
                {
                    foreach (Control innerControl in panel.Controls)
                    {
                        if (innerControl is DataGridView grid)
                            return grid;
                    }
                }
            }
            return null;
        }

        private void LoadAllSheets()
        {
            var sheets = _persistenceService.LoadAllSheets(false);
            foreach (var sheet in sheets)
            {
                AddSheetTab(sheet);
            }
        }

        private void CreateNewSheet()
        {
            if (_isReadOnly) return;

            using (var inputDialog = new InputDialog("Inserisci intestazione modulo", "Es: 2025/11, 2025, Dismissioni Gennaio", 100))
            {
                if (inputDialog.ShowDialog() == DialogResult.OK)
                {
                    string header = inputDialog.InputText.Trim();
                    if (string.IsNullOrWhiteSpace(header))
                    {
                        MessageBox.Show("L'intestazione non può essere vuota.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var newSheet = AssetSheet.CreateNew(header);
                    AddSheetTab(newSheet);
                    _persistenceService.SaveSheet(newSheet);
                    UpdateStatus("Nuovo foglio creato", Color.Green);
                }
            }
        }

        private void AddSheetTab(AssetSheet sheet)
        {
            var tabPage = new TabPage(sheet.Header + (sheet.IsArchived ? " [Archiviato]" : ""));
            tabPage.Tag = sheet;

            var panel = new Panel { Dock = DockStyle.Fill };

            var lblHeader = new Label
            {
                Text = sheet.Header,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };

            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = _isReadOnly,
                Tag = sheet
            };

            BindGridToSheet(grid, sheet);

            panel.Controls.Add(grid);
            panel.Controls.Add(lblHeader);
            tabPage.Controls.Add(panel);
            tabControl.TabPages.Add(tabPage);
        }

        private void BindGridToSheet(DataGridView grid, AssetSheet sheet)
        {
            if (grid.DataSource is DataTable oldTable)
            {
                grid.DataSource = null;
                oldTable.Dispose();
            }

            var table = new DataTable();

            foreach (var col in sheet.Columns)
            {
                table.Columns.Add(col, typeof(string));
            }

            foreach (var asset in sheet.Rows)
            {
                var row = table.NewRow();
                foreach (var col in sheet.Columns)
                {
                    row[col] = asset[col];
                }
                table.Rows.Add(row);
            }

            grid.AutoGenerateColumns = false;
            grid.Columns.Clear();

            foreach (var colName in sheet.Columns)
            {
                DataGridViewColumn column;

                if (colName == "Causa dismissione")
                {
                    var comboColumn = new DataGridViewComboBoxColumn
                    {
                        HeaderText = colName,
                        Name = colName,
                        DataPropertyName = colName,
                        DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
                    };

                    foreach (var option in _appSettings.CauseDismissioneOptions)
                    {
                        comboColumn.Items.Add(option);
                    }

                    column = comboColumn;
                }
                else
                {
                    column = new DataGridViewTextBoxColumn
                    {
                        HeaderText = colName,
                        Name = colName,
                        DataPropertyName = colName
                    };
                }

                grid.Columns.Add(column);
            }

            grid.DataSource = table;

            grid.CellValueChanged -= Grid_CellValueChanged;
            grid.CellClick -= Grid_CellClick;
            grid.EditingControlShowing -= Grid_EditingControlShowing;

            if (!_isReadOnly)
            {
                grid.CellValueChanged += Grid_CellValueChanged;
                grid.CellClick += Grid_CellClick;
                grid.EditingControlShowing += Grid_EditingControlShowing;
            }
            else
            {
                grid.DefaultCellStyle.BackColor = Color.WhiteSmoke;
                grid.DefaultCellStyle.ForeColor = Color.DarkGray;
            }
        }

        private void Grid_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || _isReadOnly)
                return;

            var grid = sender as DataGridView;
            if (grid == null)
                return;

            var column = grid.Columns[e.ColumnIndex];
            if (column is DataGridViewComboBoxColumn)
            {
                grid.BeginEdit(true);
                
                var editControl = grid.EditingControl as ComboBox;
                if (editControl != null)
                {
                    editControl.DroppedDown = true;
                }
            }
        }

        private void Grid_EditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (e.Control is ComboBox comboBox)
            {
                comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            }
        }

        private void Grid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || _isReadOnly)
                return;

            var sheet = GetCurrentSheet();
            var grid = GetCurrentGrid();
            if (sheet == null || grid == null)
                return;

            string colName = sheet.Columns[e.ColumnIndex];
            string newValue = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? string.Empty;
            sheet.Rows[e.RowIndex][colName] = newValue;

            _hasUnsavedChanges = true;

            lock (_saveLock)
            {
                _saveTimer?.Dispose();
                _saveTimer = null;

                _saveTimer = new System.Threading.Timer(SaveTimerCallback, sheet, 2000, Timeout.Infinite);
            }
        }

        private void SaveTimerCallback(object? state)
        {
            if (_isReadOnly || this.IsDisposed)
                return;

            if (state is not AssetSheet sheet)
            {
                Logger.LogWarning("SaveTimerCallback chiamato con state invalido");
                return;
            }

            try
            {
                _persistenceService.SaveSheet(sheet);
                Logger.LogInfo($"Salvataggio automatico completato: {sheet.Header}");

                _hasUnsavedChanges = false;

                if (!this.IsDisposed)
                {
                    if (this.InvokeRequired)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            if (!this.IsDisposed)
                            {
                                UpdateStatus("Salvato automaticamente", Color.Green);
                            }
                        }));
                    }
                    else
                    {
                        UpdateStatus("Salvato automaticamente", Color.Green);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Errore salvataggio automatico foglio '{sheet?.Header}'", ex);

                if (!this.IsDisposed && this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        if (!this.IsDisposed)
                        {
                            UpdateStatus("Errore salvataggio automatico", Color.Red);
                        }
                    }));
                }
            }
            finally
            {
                lock (_saveLock)
                {
                    _saveTimer?.Dispose();
                    _saveTimer = null;
                }
            }
        }

        private AssetSheet? GetCurrentSheet()
        {
            if (tabControl.SelectedTab == null)
                return null;

            return tabControl.SelectedTab.Tag as AssetSheet;
        }

        private DataGridView? GetCurrentGrid()
        {
            if (tabControl.SelectedTab == null)
                return null;

            foreach (Control control in tabControl.SelectedTab.Controls)
            {
                if (control is Panel panel)
                {
                    foreach (Control innerControl in panel.Controls)
                    {
                        if (innerControl is DataGridView grid)
                            return grid;
                    }
                }
            }
            return null;
        }

        private Label? GetCurrentHeaderLabel()
        {
            if (tabControl.SelectedTab == null)
                return null;

            foreach (Control control in tabControl.SelectedTab.Controls)
            {
                if (control is Panel panel)
                {
                    foreach (Control innerControl in panel.Controls)
                    {
                        if (innerControl is Label label)
                            return label;
                    }
                }
            }
            return null;
        }

        private void RefreshCurrentGrid()
        {
            var sheet = GetCurrentSheet();
            var grid = GetCurrentGrid();
            if (sheet != null && grid != null)
            {
                BindGridToSheet(grid, sheet);
            }
        }

        private void RefreshAllGrids()
        {
            foreach (TabPage tab in tabControl.TabPages)
            {
                var sheet = tab.Tag as AssetSheet;
                if (sheet != null)
                {
                    foreach (Control control in tab.Controls)
                    {
                        if (control is Panel panel)
                        {
                            foreach (Control innerControl in panel.Controls)
                            {
                                if (innerControl is DataGridView grid)
                                {
                                    BindGridToSheet(grid, sheet);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void UpdateStatus(string message, Color color)
        {
            if (_statusLabel == null || this.IsDisposed)
                return;

            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateStatus(message, color)));
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

                if (!this.IsDisposed && _statusLabel != null)
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

        private void RemoveTabAndDispose(TabPage tabPage)
        {
            foreach (Control control in tabPage.Controls)
            {
                if (control is Panel panel)
                {
                    foreach (Control innerControl in panel.Controls)
                    {
                        if (innerControl is DataGridView grid)
                        {
                            if (grid.DataSource is DataTable table)
                            {
                                grid.DataSource = null;
                                table.Dispose();
                            }
                            grid.Dispose();
                        }
                    }
                    panel.Dispose();
                }
            }

            tabControl.TabPages.Remove(tabPage);
            tabPage.Dispose();
        }

        private void btnNewSheet_Click(object? sender, EventArgs e)
        {
            if (_isReadOnly) return;
            CreateNewSheet();
        }

        private void btnSave_Click(object? sender, EventArgs e)
        {
            if (_isReadOnly) return;

            var sheet = GetCurrentSheet();
            if (sheet == null)
            {
                MessageBox.Show("Nessun foglio selezionato.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _persistenceService.SaveSheet(sheet);
                _hasUnsavedChanges = false;
                UpdateStatus("Foglio salvato", Color.Green);
                MessageBox.Show("Foglio salvato con successo.", "Successo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Logger.LogError("Errore salvataggio foglio manuale", ex);
                MessageBox.Show($"Errore durante il salvataggio:\n{ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnExport_Click(object? sender, EventArgs e)
        {
            var sheet = GetCurrentSheet();
            if (sheet == null)
            {
                MessageBox.Show("Nessun foglio selezionato.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "File Excel|*.xlsx";
                sfd.Title = "Esporta in Excel";
                sfd.FileName = sheet.Header + ".xlsx";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _excelService.ExportToExcel(sheet, sfd.FileName);
                        UpdateStatus("Esportato in Excel", Color.Green);
                        MessageBox.Show("Esportazione completata con successo.", "Successo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Errore export Excel", ex);
                        MessageBox.Show($"Errore durante l'esportazione:\n{ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnAddRow_Click(object? sender, EventArgs e)
        {
            if (_isReadOnly) return;

            var sheet = GetCurrentSheet();
            if (sheet == null) return;

            var newAsset = new Asset();
            sheet.Rows.Add(newAsset);
            RefreshCurrentGrid();
            
            try
            {
                _persistenceService.SaveSheet(sheet);
                _hasUnsavedChanges = false;
                UpdateStatus("Riga aggiunta", Color.Green);
            }
            catch (Exception ex)
            {
                Logger.LogError("Errore aggiunta riga", ex);
            }
        }

        private void btnRemoveRow_Click(object? sender, EventArgs e)
        {
            if (_isReadOnly) return;

            var sheet = GetCurrentSheet();
            var grid = GetCurrentGrid();
            if (sheet == null || grid == null) return;

            if (grid.SelectedRows.Count == 0)
            {
                MessageBox.Show("Seleziona una riga da eliminare.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("Vuoi davvero eliminare la riga selezionata?", "Conferma", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                int rowIndex = grid.SelectedRows[0].Index;
                if (rowIndex >= 0 && rowIndex < sheet.Rows.Count)
                {
                    sheet.Rows.RemoveAt(rowIndex);
                    RefreshCurrentGrid();
                    
                    try
                    {
                        _persistenceService.SaveSheet(sheet);
                        _hasUnsavedChanges = false;
                        UpdateStatus("Riga eliminata", Color.Green);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Errore rimozione riga", ex);
                    }
                }
            }
        }

        private void btnAddColumn_Click(object? sender, EventArgs e)
        {
            if (_isReadOnly) return;

            var sheet = GetCurrentSheet();
            if (sheet == null) return;

            using (var inputDialog = new InputDialog("Inserisci nome nuova colonna", "", 100))
            {
                if (inputDialog.ShowDialog() == DialogResult.OK)
                {
                    string colName = inputDialog.InputText.Trim();
                    if (string.IsNullOrWhiteSpace(colName))
                    {
                        MessageBox.Show("Il nome della colonna non può essere vuoto.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (sheet.Columns.Any(c => c.Equals(colName, StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show("Esiste già una colonna con questo nome.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    sheet.Columns.Add(colName);
                    RefreshCurrentGrid();
                    
                    try
                    {
                        _persistenceService.SaveSheet(sheet);
                        _hasUnsavedChanges = false;
                        UpdateStatus("Colonna aggiunta", Color.Green);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Errore aggiunta colonna", ex);
                    }
                }
            }
        }

        private void btnRemoveColumn_Click(object? sender, EventArgs e)
        {
            if (_isReadOnly) return;

            var sheet = GetCurrentSheet();
            var grid = GetCurrentGrid();
            if (sheet == null || grid == null) return;

            if (grid.SelectedCells.Count == 0)
            {
                MessageBox.Show("Seleziona una cella della colonna da eliminare.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int colIndex = grid.SelectedCells[0].ColumnIndex;
            if (colIndex < AssetSheet.StandardColumnCount)
            {
                MessageBox.Show("Non puoi eliminare le colonne standard.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string colName = sheet.Columns[colIndex];
            var result = MessageBox.Show($"Vuoi davvero eliminare la colonna '{colName}'?", "Conferma", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                sheet.Columns.RemoveAt(colIndex);

                foreach (var asset in sheet.Rows)
                {
                    if (asset.Values.ContainsKey(colName))
                        asset.Values.Remove(colName);
                }

                RefreshCurrentGrid();
                
                try
                {
                    _persistenceService.SaveSheet(sheet);
                    _hasUnsavedChanges = false;
                    UpdateStatus("Colonna eliminata", Color.Green);
                }
                catch (Exception ex)
                {
                    Logger.LogError("Errore rimozione colonna", ex);
                }
            }
        }

        private void btnRenameSheet_Click(object? sender, EventArgs e)
        {
            if (_isReadOnly) return;

            var sheet = GetCurrentSheet();
            var currentTab = tabControl.SelectedTab;
            var headerLabel = GetCurrentHeaderLabel();

            if (sheet == null || currentTab == null)
            {
                MessageBox.Show("Nessun foglio selezionato.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var inputDialog = new InputDialog("Rinomina foglio", sheet.Header, 100))
            {
                if (inputDialog.ShowDialog() == DialogResult.OK)
                {
                    string newHeader = inputDialog.InputText.Trim();
                    if (string.IsNullOrWhiteSpace(newHeader))
                    {
                        MessageBox.Show("Il nome del foglio non può essere vuoto.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    sheet.Header = newHeader;
                    currentTab.Text = newHeader + (sheet.IsArchived ? " [Archiviato]" : "");

                    if (headerLabel != null)
                    {
                        headerLabel.Text = newHeader;
                    }

                    try
                    {
                        _persistenceService.SaveSheet(sheet);
                        _hasUnsavedChanges = false;
                        UpdateStatus("Foglio rinominato", Color.Green);
                        MessageBox.Show("Foglio rinominato con successo.", "Successo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Errore rinomina foglio", ex);
                        MessageBox.Show($"Errore durante il salvataggio:\n{ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnDeleteSheet_Click(object? sender, EventArgs e)
        {
            if (_isReadOnly) return;

            var sheet = GetCurrentSheet();
            var currentTab = tabControl.SelectedTab;

            if (sheet == null || currentTab == null)
            {
                MessageBox.Show("Nessun foglio selezionato.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show($"Vuoi davvero eliminare il foglio '{sheet.Header}'?\nQuesta operazione non può essere annullata.",
                "Conferma eliminazione", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                try
                {
                    _persistenceService.DeleteSheet(sheet);
                    RemoveTabAndDispose(currentTab);
                    _hasUnsavedChanges = false;
                    UpdateStatus("Foglio eliminato", Color.Green);
                    MessageBox.Show("Foglio eliminato con successo.", "Successo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    Logger.LogError("Errore eliminazione foglio", ex);
                    MessageBox.Show($"Errore durante l'eliminazione:\n{ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnArchiveSheet_Click(object? sender, EventArgs e)
        {
            if (_isReadOnly) return;

            var sheet = GetCurrentSheet();
            var currentTab = tabControl.SelectedTab;

            if (sheet == null || currentTab == null)
            {
                MessageBox.Show("Nessun foglio selezionato.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (sheet.IsArchived)
            {
                MessageBox.Show("Questo foglio è già archiviato.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show($"Vuoi archiviare il foglio '{sheet.Header}'?", "Conferma archiviazione", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    _persistenceService.ArchiveSheet(sheet);
                    RemoveTabAndDispose(currentTab);
                    _hasUnsavedChanges = false;
                    UpdateStatus("Foglio archiviato", Color.Green);
                    MessageBox.Show("Foglio archiviato con successo.", "Successo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    Logger.LogError("Errore archiviazione foglio", ex);
                    MessageBox.Show($"Errore durante l'archiviazione:\n{ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnViewArchived_Click(object? sender, EventArgs e)
        {
            var archivedSheets = _persistenceService.LoadAllSheets(true).Where(s => s.IsArchived).ToList();

            if (archivedSheets.Count == 0)
            {
                MessageBox.Show("Nessun foglio archiviato trovato.", "Informazione", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var archiveDialog = new ArchiveDialog(archivedSheets, _persistenceService, _isReadOnly))
            {
                if (archiveDialog.ShowDialog() == DialogResult.OK)
                {
                    if (!_isReadOnly)
                    {
                        while (tabControl.TabPages.Count > 0)
                        {
                            RemoveTabAndDispose(tabControl.TabPages[0]);
                        }
                        LoadAllSheets();
                        UpdateStatus("Fogli ricaricati", Color.Green);
                    }
                }
            }
        }

        private void btnManageOptions_Click(object? sender, EventArgs e)
        {
            if (_isReadOnly) return;

            using (var optionsDialog = new OptionsDialog(_appSettings))
            {
                if (optionsDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _settingsService.SaveSettings(_appSettings);
                        UpdateStatus("Opzioni salvate", Color.Green);
                        MessageBox.Show("Impostazioni salvate con successo.", "Successo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        RefreshAllGrids();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Errore salvataggio opzioni", ex);
                        MessageBox.Show($"Errore durante il salvataggio:\n{ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }

    public class SearchResult
    {
        public AssetSheet Sheet { get; set; } = null!;
        public int RowIndex { get; set; }
        public string ColumnName { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

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
