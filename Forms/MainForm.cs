using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using GestioneCespiti.Models;
using GestioneCespiti.Services;
using GestioneCespiti.Forms.Dialogs;
using GestioneCespiti.Managers;

namespace GestioneCespiti
{
    public partial class MainForm : Form
    {
        private readonly DataPersistenceService _persistenceService;
        private readonly ExcelExportService _excelService;
        private readonly SettingsService _settingsService;
        private readonly LockService _lockService;
        private AppSettings _defaultSettings;
        private readonly Dictionary<string, AppSettings> _sheetSettings = new Dictionary<string, AppSettings>(StringComparer.OrdinalIgnoreCase);
        private bool _isReadOnly;
        private System.Threading.Timer? _saveTimer;
        private readonly HashSet<AssetSheet> _pendingAutoSaveSheets = new HashSet<AssetSheet>();
        private readonly object _saveLock = new object();

        private SearchManager? _searchManager;
        private GridManager? _gridManager;
        private StatusManager? _statusManager;
        private DataGridViewCell? _lastHighlightedSearchCell;
        private Color _lastSearchCellBackColor = Color.Empty;
        private Color _lastSearchCellSelectionBackColor = Color.Empty;

        private bool _hasUnsavedChanges = false;
        private static readonly Color ActiveTabColor = Color.FromArgb(210, 233, 255);
        private static readonly Color InactiveTabColor = SystemColors.Control;
        private const string IncludeArchivedBaseText = "Includi archiviati";
        private const string MatchCaseBaseText = "Match case";

        public MainForm()
        {
            InitializeComponent();
            _persistenceService = new DataPersistenceService();
            _excelService = new ExcelExportService();
            _settingsService = new SettingsService();
            _lockService = new LockService();
            _defaultSettings = _settingsService.LoadSettings();

            CheckApplicationLock();
            InitializeManagers();
            LoadAllSheets();

            if (tabControl?.TabCount == 0 && !_isReadOnly)
            {
                CreateNewSheet();
            }

            ConfigureTabControlRendering();
            UpdateSearchToggleVisualState();
            UpdateUIForReadOnlyMode();
        }

        private void InitializeManagers()
        {
            _statusManager = new StatusManager(statusStrip, this);
            _searchManager = new SearchManager(_persistenceService);
            _gridManager = new GridManager(_isReadOnly);

            _searchManager.SearchCompleted += SearchManager_SearchCompleted;
            _searchManager.NavigateRequested += SearchManager_NavigateRequested;
            _gridManager.CellValueChanged += GridManager_CellValueChanged;
        }

        private void GridManager_CellValueChanged(object? sender, CellValueChangedEventArgs e)
        {
            e.Sheet.Rows[e.RowIndex][e.ColumnName] = e.NewValue;

            _hasUnsavedChanges = true;

            lock (_saveLock)
            {
                _pendingAutoSaveSheets.Add(e.Sheet);
                _saveTimer?.Dispose();
                _saveTimer = null;
                _saveTimer = new System.Threading.Timer(SaveTimerCallback, null, 2000, Timeout.Infinite);
            }
        }

        private void SearchManager_SearchCompleted(object? sender, SearchCompletedEventArgs e)
        {
            if (e.HasResults)
            {
                searchNextButton.Visible = e.ResultCount > 1;
                _statusManager?.UpdateStatus($"Trovati {e.ResultCount} risultati (1/{e.ResultCount})", Color.Blue);
                searchTextBox.BackColor = Color.LightYellow;
            }
            else
            {
                searchNextButton.Visible = false;
                searchTextBox.BackColor = SystemColors.Window;
                ClearSearchCellHighlight();
                _statusManager?.UpdateStatus("Nessun risultato trovato", Color.Gray);
            }
        }

        private void SearchManager_NavigateRequested(object? sender, SearchNavigateEventArgs e)
        {
            NavigateToSearchResult(e.Result);
            if (_searchManager != null)
            {
                _statusManager?.UpdateStatus($"Risultato {_searchManager.CurrentIndex + 1}/{_searchManager.TotalResults}", Color.Blue);
            }
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
                                   "Per evitare conflitti, l'applicazione sarà aperta in modalità SOLA LETTURA.";

                    MessageBox.Show(message, "Applicazione in Uso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show(
                        "Non è stato possibile acquisire il lock esclusivo.\n" +
                        "L'applicazione verrà aperta in modalità SOLA LETTURA.",
                        "Lock non disponibile",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
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
                menuManageColumns.Enabled = false;

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
                    if (!SaveAllSheets())
                    {
                        var confirmResult = MessageBox.Show(
                            "Alcuni fogli non sono stati salvati.\n\nVuoi comunque chiudere l'applicazione?",
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

            ClearSearchCellHighlight();

            _statusManager?.Dispose();

            if (!_isReadOnly)
            {
                _lockService.ReleaseLock();
                Logger.LogInfo("Lock rilasciato");
            }

            Logger.LogInfo("Applicazione chiusa");
            base.OnFormClosing(e);
        }

        private bool SaveAllSheets()
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

            return allSaved;
        }

        private void searchTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;

                if (_searchManager?.HasResults == true)
                {
                    searchNextButton_Click(sender, e);
                }
                else
                {
                    searchButton_Click(sender, e);
                }
            }
        }


        private void searchFilter_CheckedChanged(object? sender, EventArgs e)
        {
            var includeArchived = searchIncludeArchivedToggle.Checked ? "ON" : "OFF";
            var matchCase = searchCaseSensitiveToggle.Checked ? "ON" : "OFF";
            UpdateSearchToggleVisualState();
            _statusManager?.UpdateStatus($"Filtri ricerca - Archiviati: {includeArchived}, Match case: {matchCase}", Color.Gray);

            string searchText = searchTextBox.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                _searchManager?.PerformSearch(searchText, searchIncludeArchivedToggle.Checked, searchCaseSensitiveToggle.Checked, false);
            }
            else
            {
                searchNextButton.Visible = false;
                searchTextBox.BackColor = SystemColors.Window;
                ClearSearchCellHighlight();
            }
        }

        private void searchButton_Click(object? sender, EventArgs e)
        {
            string searchText = searchTextBox.Text?.Trim() ?? string.Empty;
            _searchManager?.PerformSearch(searchText, searchIncludeArchivedToggle.Checked, searchCaseSensitiveToggle.Checked, true);
        }

        private void searchNextButton_Click(object? sender, EventArgs e)
        {
            bool moved = _searchManager?.NavigateNext() ?? false;
            if (!moved && _searchManager?.HasResults == true)
            {
                _statusManager?.UpdateStatus("Nessun'altra occorrenza trovata", Color.Gray);
            }
            else if (!moved)
            {
                _statusManager?.UpdateStatus("Esegui prima una ricerca", Color.Gray);
            }
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
                    bool rowInRange = result.RowIndex >= 0 && result.RowIndex < grid.Rows.Count;
                    bool columnInRange = colIndex >= 0 && (colIndex + 1) < grid.Columns.Count;

                    if (rowInRange && columnInRange)
                    {
                        HighlightSearchCell(grid, result.RowIndex, colIndex + 1);
                        grid.ClearSelection();
                        grid.Rows[result.RowIndex].Cells[colIndex + 1].Selected = true;
                        grid.FirstDisplayedScrollingRowIndex = result.RowIndex;
                        grid.Focus();
                    }
                    else
                    {
                        _statusManager?.UpdateStatus("Risultato non più disponibile: struttura foglio cambiata", Color.DarkOrange);
                    }
                }
            }
        }

        private void HighlightSearchCell(DataGridView grid, int rowIndex, int columnIndex)
        {
            ClearSearchCellHighlight();

            if (rowIndex < 0 || columnIndex < 0 || rowIndex >= grid.Rows.Count || columnIndex >= grid.Columns.Count)
                return;

            var targetCell = grid.Rows[rowIndex].Cells[columnIndex];
            _lastHighlightedSearchCell = targetCell;
            _lastSearchCellBackColor = targetCell.Style.BackColor;
            _lastSearchCellSelectionBackColor = targetCell.Style.SelectionBackColor;

            targetCell.Style.BackColor = Color.Khaki;
            targetCell.Style.SelectionBackColor = Color.Gold;
        }

        private void ClearSearchCellHighlight()
        {
            if (_lastHighlightedSearchCell == null)
                return;

            if (_lastHighlightedSearchCell.DataGridView != null)
            {
                _lastHighlightedSearchCell.Style.BackColor = _lastSearchCellBackColor;
                _lastHighlightedSearchCell.Style.SelectionBackColor = _lastSearchCellSelectionBackColor;
            }

            _lastHighlightedSearchCell = null;
            _lastSearchCellBackColor = Color.Empty;
            _lastSearchCellSelectionBackColor = Color.Empty;
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
                    InitializeSheetSettings(newSheet);
                    _statusManager?.UpdateStatus("Nuovo foglio creato", Color.Green);
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = _isReadOnly,
                Tag = sheet,
                EditMode = DataGridViewEditMode.EditOnEnter
            };

            var settings = GetSheetSettings(sheet);
            _gridManager?.BindGridToSheet(grid, sheet, settings);

            panel.Controls.Add(grid);
            panel.Controls.Add(lblHeader);
            tabPage.Controls.Add(panel);
            tabControl.TabPages.Add(tabPage);
            tabControl.Invalidate();
        }

        private void ConfigureTabControlRendering()
        {
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem -= TabControl_DrawItem;
            tabControl.DrawItem += TabControl_DrawItem;
            tabControl.SelectedIndexChanged -= TabControl_SelectedIndexChanged;
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
            tabControl.Invalidate();
        }

        private void TabControl_SelectedIndexChanged(object? sender, EventArgs e)
        {
            tabControl.Invalidate();
        }

        private void TabControl_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= tabControl.TabPages.Count)
                return;

            var tabPage = tabControl.TabPages[e.Index];
            var bounds = e.Bounds;
            bool isSelected = e.Index == tabControl.SelectedIndex;

            using (var backBrush = new SolidBrush(isSelected ? ActiveTabColor : InactiveTabColor))
            {
                e.Graphics.FillRectangle(backBrush, bounds);
            }

            TextRenderer.DrawText(
                e.Graphics,
                tabPage.Text,
                e.Font,
                bounds,
                isSelected ? Color.Navy : SystemColors.ControlText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

            using (var borderPen = new Pen(Color.SteelBlue))
            {
                e.Graphics.DrawRectangle(borderPen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            }
        }

        private void UpdateSearchToggleVisualState()
        {
            SetToggleButtonStyle(searchIncludeArchivedToggle, IncludeArchivedBaseText, searchIncludeArchivedToggle.Checked);
            SetToggleButtonStyle(searchCaseSensitiveToggle, MatchCaseBaseText, searchCaseSensitiveToggle.Checked);
        }

        private static void SetToggleButtonStyle(ToolStripButton button, string baseText, bool isActive)
        {
            button.Text = isActive ? $"✓ {baseText}" : baseText;
            button.BackColor = isActive ? Color.LightGreen : Color.Transparent;
            button.ForeColor = isActive ? Color.DarkGreen : SystemColors.ControlText;
            button.Owner?.Invalidate();
        }

        private static string BuildSafeExportFileName(string header)
        {
            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            string safeHeader = string.IsNullOrWhiteSpace(header)
                ? "foglio"
                : new string(header.Trim().Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());

            if (string.IsNullOrWhiteSpace(safeHeader))
            {
                safeHeader = "foglio";
            }

            return safeHeader + ".xlsx";
        }

        private void SaveTimerCallback(object? state)
        {
            if (_isReadOnly || this.IsDisposed)
                return;

            List<AssetSheet> sheetsToSave;
            lock (_saveLock)
            {
                sheetsToSave = _pendingAutoSaveSheets.ToList();
                _pendingAutoSaveSheets.Clear();
            }

            bool allSaved = true;

            try
            {
                foreach (var sheet in sheetsToSave)
                {
                    _persistenceService.SaveSheet(sheet);
                    Logger.LogInfo($"Salvataggio automatico completato: {sheet.Header}");
                }

                _hasUnsavedChanges = false;

                if (!this.IsDisposed && this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        if (!this.IsDisposed)
                        {
                            _statusManager?.UpdateStatus("Salvataggio automatico completato", Color.Green);
                        }
                    }));
                }
                else if (!this.IsDisposed)
                {
                    _statusManager?.UpdateStatus("Salvataggio automatico completato", Color.Green);
                }
            }
            catch (Exception ex)
            {
                allSaved = false;
                Logger.LogError("Errore salvataggio automatico", ex);

                if (!this.IsDisposed && this.InvokeRequired)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        if (!this.IsDisposed)
                        {
                            _statusManager?.UpdateStatus("Errore salvataggio automatico", Color.Red);
                        }
                    }));
                }
            }
            finally
            {
                lock (_saveLock)
                {
                    if (!allSaved)
                    {
                        foreach (var unsavedSheet in sheetsToSave)
                        {
                            _pendingAutoSaveSheets.Add(unsavedSheet);
                        }

                        _hasUnsavedChanges = true;
                    }

                    _saveTimer?.Dispose();
                    _saveTimer = null;

                    if (_pendingAutoSaveSheets.Count > 0 && !_isReadOnly && !this.IsDisposed)
                    {
                        _saveTimer = new System.Threading.Timer(SaveTimerCallback, null, 2000, Timeout.Infinite);
                    }
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

            return GetGridFromTab(tabControl.SelectedTab);
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
                var settings = GetSheetSettings(sheet);
                _gridManager?.BindGridToSheet(grid, sheet, settings);
            }
        }

        private void RefreshAllGrids()
        {
            foreach (TabPage tab in tabControl.TabPages)
            {
                var sheet = tab.Tag as AssetSheet;
                if (sheet != null)
                {
                    var grid = GetGridFromTab(tab);
                    if (grid != null)
                    {
                        var settings = GetSheetSettings(sheet);
                        _gridManager?.BindGridToSheet(grid, sheet, settings);
                    }
                }
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
                _statusManager?.UpdateStatus("Foglio salvato", Color.Green);
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
                sfd.FileName = BuildSafeExportFileName(sheet.Header);

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _excelService.ExportToExcel(sheet, sfd.FileName);
                        _statusManager?.UpdateStatus("Esportato in Excel", Color.Green);
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
                _statusManager?.UpdateStatus("Riga aggiunta", Color.Green);
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
                        _statusManager?.UpdateStatus("Riga eliminata", Color.Green);
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
                        _statusManager?.UpdateStatus("Colonna aggiunta", Color.Green);
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

            if (colIndex == 0)
            {
                MessageBox.Show("Non puoi eliminare la colonna numero riga.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int sheetColIndex = colIndex - 1;

            if (sheetColIndex < 0 || sheetColIndex >= sheet.Columns.Count)
            {
                MessageBox.Show("Colonna non valida o non più disponibile.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (sheetColIndex < AssetSheet.StandardColumnCount)
            {
                MessageBox.Show("Non puoi eliminare le colonne standard.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string colName = sheet.Columns[sheetColIndex];
            var result = MessageBox.Show($"Vuoi davvero eliminare la colonna '{colName}'?", "Conferma", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                sheet.Columns.RemoveAt(sheetColIndex);

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
                    _statusManager?.UpdateStatus("Colonna eliminata", Color.Green);
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
                        _statusManager?.UpdateStatus("Foglio rinominato", Color.Green);
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
                    _statusManager?.UpdateStatus("Foglio eliminato", Color.Green);
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
                    _statusManager?.UpdateStatus("Foglio archiviato", Color.Green);
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
                        _statusManager?.UpdateStatus("Fogli ricaricati", Color.Green);
                    }
                }
            }
        }

        private void btnManageOptions_Click(object? sender, EventArgs e)
        {
            if (_isReadOnly) return;

            var sheet = GetCurrentSheet();
            if (sheet == null)
            {
                MessageBox.Show("Nessun foglio selezionato.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var optionsDialog = new OptionsDialog(
                "Gestisci Opzioni Causa Dismissione",
                "Opzioni disponibili per 'Causa dismissione':",
                GetSheetSettings(sheet).CauseDismissioneOptions,
                AppSettings.IsDefaultOption))
            {
                if (optionsDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var sheetSettings = GetSheetSettings(sheet);
                        _settingsService.SaveSettingsForSheet(sheetSettings, sheet.FileName);
                        _sheetSettings[sheet.FileName] = sheetSettings;
                        _statusManager?.UpdateStatus("Opzioni salvate", Color.Green);
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

        private void btnManageTipoAsset_Click(object? sender, EventArgs e)
        {
            if (_isReadOnly) return;

            var sheet = GetCurrentSheet();
            if (sheet == null)
            {
                MessageBox.Show("Nessun foglio selezionato.", "Attenzione", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var optionsDialog = new OptionsDialog(
                "Gestisci Opzioni Tipo Asset",
                "Opzioni disponibili per 'Tipo asset':",
                GetSheetSettings(sheet).TipoAssetOptions))
            {
                if (optionsDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var sheetSettings = GetSheetSettings(sheet);
                        _settingsService.SaveSettingsForSheet(sheetSettings, sheet.FileName);
                        _sheetSettings[sheet.FileName] = sheetSettings;
                        _statusManager?.UpdateStatus("Opzioni salvate", Color.Green);
                        MessageBox.Show("Impostazioni salvate con successo.", "Successo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        RefreshAllGrids();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("Errore salvataggio opzioni tipo asset", ex);
                        MessageBox.Show($"Errore durante il salvataggio:\n{ex.Message}", "Errore", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void menuAbout_Click(object? sender, EventArgs e)
        {
            using (var aboutDialog = new AboutDialog())
            {
                aboutDialog.ShowDialog(this);
            }
        }

        private AppSettings GetSheetSettings(AssetSheet? sheet)
        {
            if (sheet == null || string.IsNullOrWhiteSpace(sheet.FileName))
            {
                return CreateCopy(_defaultSettings);
            }

            if (_sheetSettings.TryGetValue(sheet.FileName, out var cachedSettings))
            {
                return cachedSettings;
            }

            var settings = _settingsService.LoadSettingsForSheet(sheet.FileName, _defaultSettings);
            _sheetSettings[sheet.FileName] = settings;
            return settings;
        }

        private void InitializeSheetSettings(AssetSheet sheet)
        {
            if (string.IsNullOrWhiteSpace(sheet.FileName))
                return;

            if (_sheetSettings.ContainsKey(sheet.FileName))
                return;

            var settings = CreateCopy(_defaultSettings);
            _settingsService.SaveSettingsForSheet(settings, sheet.FileName);
            _sheetSettings[sheet.FileName] = settings;
        }

        private static AppSettings CreateCopy(AppSettings settings)
        {
            return new AppSettings
            {
                CauseDismissioneOptions = new List<string>(settings.CauseDismissioneOptions ?? AppSettings.GetDefaultOptions()),
                TipoAssetOptions = new List<string>(settings.TipoAssetOptions ?? new List<string>())
            };
        }
    }
}
