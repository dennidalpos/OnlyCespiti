namespace GestioneCespiti
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null!;
        private System.Windows.Forms.MenuStrip menuStrip = null!;

        // Menu File
        private System.Windows.Forms.ToolStripMenuItem menuFile = null!;
        private System.Windows.Forms.ToolStripMenuItem menuNewSheet = null!;
        private System.Windows.Forms.ToolStripMenuItem menuSave = null!;
        private System.Windows.Forms.ToolStripMenuItem menuExport = null!;

        // Menu Riga
        private System.Windows.Forms.ToolStripMenuItem menuRow = null!;
        private System.Windows.Forms.ToolStripMenuItem menuAddRow = null!;
        private System.Windows.Forms.ToolStripMenuItem menuRemoveRow = null!;

        // Menu Colonna
        private System.Windows.Forms.ToolStripMenuItem menuColumn = null!;
        private System.Windows.Forms.ToolStripMenuItem menuAddColumn = null!;
        private System.Windows.Forms.ToolStripMenuItem menuRemoveColumn = null!;

        // Menu Foglio
        private System.Windows.Forms.ToolStripMenuItem menuSheet = null!;
        private System.Windows.Forms.ToolStripMenuItem menuRenameSheet = null!;
        private System.Windows.Forms.ToolStripMenuItem menuDeleteSheet = null!;

        // Menu Archiviazione
        private System.Windows.Forms.ToolStripMenuItem menuArchive = null!;
        private System.Windows.Forms.ToolStripMenuItem menuArchiveSheet = null!;
        private System.Windows.Forms.ToolStripMenuItem menuViewArchived = null!;

        // Menu Strumenti
        private System.Windows.Forms.ToolStripMenuItem menuTools = null!;
        private System.Windows.Forms.ToolStripMenuItem menuManageColumns = null!;
        private System.Windows.Forms.ToolStripMenuItem menuManageCauseDismissione = null!;
        private System.Windows.Forms.ToolStripMenuItem menuManageTipoAsset = null!;

        // Menu Aiuto
        private System.Windows.Forms.ToolStripMenuItem menuHelp = null!;
        private System.Windows.Forms.ToolStripMenuItem menuAbout = null!;

        // Ricerca
        private System.Windows.Forms.ToolStripTextBox searchTextBox = null!;
        private System.Windows.Forms.ToolStripButton searchButton = null!;
        private System.Windows.Forms.ToolStripButton searchNextButton = null!;

        // Controlli principali
        private System.Windows.Forms.TabControl tabControl = null!;
        private System.Windows.Forms.ToolStrip statusStrip = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.menuStrip = new System.Windows.Forms.MenuStrip();

            // Menu File
            this.menuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.menuNewSheet = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSave = new System.Windows.Forms.ToolStripMenuItem();
            this.menuExport = new System.Windows.Forms.ToolStripMenuItem();

            // Menu Riga
            this.menuRow = new System.Windows.Forms.ToolStripMenuItem();
            this.menuAddRow = new System.Windows.Forms.ToolStripMenuItem();
            this.menuRemoveRow = new System.Windows.Forms.ToolStripMenuItem();

            // Menu Colonna
            this.menuColumn = new System.Windows.Forms.ToolStripMenuItem();
            this.menuAddColumn = new System.Windows.Forms.ToolStripMenuItem();
            this.menuRemoveColumn = new System.Windows.Forms.ToolStripMenuItem();

            // Menu Foglio
            this.menuSheet = new System.Windows.Forms.ToolStripMenuItem();
            this.menuRenameSheet = new System.Windows.Forms.ToolStripMenuItem();
            this.menuDeleteSheet = new System.Windows.Forms.ToolStripMenuItem();

            // Menu Archiviazione
            this.menuArchive = new System.Windows.Forms.ToolStripMenuItem();
            this.menuArchiveSheet = new System.Windows.Forms.ToolStripMenuItem();
            this.menuViewArchived = new System.Windows.Forms.ToolStripMenuItem();

            // Menu Strumenti
            this.menuTools = new System.Windows.Forms.ToolStripMenuItem();
            this.menuManageColumns = new System.Windows.Forms.ToolStripMenuItem();
            this.menuManageCauseDismissione = new System.Windows.Forms.ToolStripMenuItem();
            this.menuManageTipoAsset = new System.Windows.Forms.ToolStripMenuItem();

            // Menu Aiuto
            this.menuHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.menuAbout = new System.Windows.Forms.ToolStripMenuItem();

            // Ricerca
            this.searchTextBox = new System.Windows.Forms.ToolStripTextBox();
            this.searchButton = new System.Windows.Forms.ToolStripButton();
            this.searchNextButton = new System.Windows.Forms.ToolStripButton();

            // Controlli
            this.tabControl = new System.Windows.Forms.TabControl();
            this.statusStrip = new System.Windows.Forms.ToolStrip();

            this.menuStrip.SuspendLayout();
            this.SuspendLayout();

            // MenuStrip
            this.menuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.menuFile,
                this.menuRow,
                this.menuColumn,
                this.menuSheet,
                this.menuArchive,
                this.menuTools,
                this.menuHelp,
                this.searchTextBox,
                this.searchButton,
                this.searchNextButton
            });
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(1200, 28);
            this.menuStrip.TabIndex = 0;

            // Menu File
            this.menuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.menuNewSheet,
                this.menuSave,
                this.menuExport
            });
            this.menuFile.Name = "menuFile";
            this.menuFile.Size = new System.Drawing.Size(50, 24);
            this.menuFile.Text = "File";

            this.menuNewSheet.Name = "menuNewSheet";
            this.menuNewSheet.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.menuNewSheet.Size = new System.Drawing.Size(250, 26);
            this.menuNewSheet.Text = "Nuovo Foglio";
            this.menuNewSheet.Click += new System.EventHandler(this.btnNewSheet_Click);

            this.menuSave.Name = "menuSave";
            this.menuSave.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.menuSave.Size = new System.Drawing.Size(250, 26);
            this.menuSave.Text = "Salva";
            this.menuSave.Click += new System.EventHandler(this.btnSave_Click);

            this.menuExport.Name = "menuExport";
            this.menuExport.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E)));
            this.menuExport.Size = new System.Drawing.Size(250, 26);
            this.menuExport.Text = "Esporta in Excel";
            this.menuExport.Click += new System.EventHandler(this.btnExport_Click);

            // Menu Riga
            this.menuRow.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.menuAddRow,
                this.menuRemoveRow
            });
            this.menuRow.Name = "menuRow";
            this.menuRow.Size = new System.Drawing.Size(55, 24);
            this.menuRow.Text = "Riga";

            this.menuAddRow.Name = "menuAddRow";
            this.menuAddRow.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
            this.menuAddRow.Size = new System.Drawing.Size(250, 26);
            this.menuAddRow.Text = "Aggiungi Riga";
            this.menuAddRow.Click += new System.EventHandler(this.btnAddRow_Click);

            this.menuRemoveRow.Name = "menuRemoveRow";
            this.menuRemoveRow.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Delete)));
            this.menuRemoveRow.Size = new System.Drawing.Size(250, 26);
            this.menuRemoveRow.Text = "Rimuovi Riga";
            this.menuRemoveRow.Click += new System.EventHandler(this.btnRemoveRow_Click);

            // Menu Colonna
            this.menuColumn.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.menuAddColumn,
                this.menuRemoveColumn
            });
            this.menuColumn.Name = "menuColumn";
            this.menuColumn.Size = new System.Drawing.Size(75, 24);
            this.menuColumn.Text = "Colonna";

            this.menuAddColumn.Name = "menuAddColumn";
            this.menuAddColumn.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.K)));
            this.menuAddColumn.Size = new System.Drawing.Size(250, 26);
            this.menuAddColumn.Text = "Aggiungi Colonna";
            this.menuAddColumn.Click += new System.EventHandler(this.btnAddColumn_Click);

            this.menuRemoveColumn.Name = "menuRemoveColumn";
            this.menuRemoveColumn.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) | System.Windows.Forms.Keys.Delete)));
            this.menuRemoveColumn.Size = new System.Drawing.Size(250, 26);
            this.menuRemoveColumn.Text = "Rimuovi Colonna";
            this.menuRemoveColumn.Click += new System.EventHandler(this.btnRemoveColumn_Click);

            // Menu Foglio
            this.menuSheet.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.menuRenameSheet,
                this.menuDeleteSheet
            });
            this.menuSheet.Name = "menuSheet";
            this.menuSheet.Size = new System.Drawing.Size(60, 24);
            this.menuSheet.Text = "Foglio";

            this.menuRenameSheet.Name = "menuRenameSheet";
            this.menuRenameSheet.ShortcutKeys = System.Windows.Forms.Keys.F2;
            this.menuRenameSheet.Size = new System.Drawing.Size(250, 26);
            this.menuRenameSheet.Text = "Rinomina Foglio";
            this.menuRenameSheet.Click += new System.EventHandler(this.btnRenameSheet_Click);

            this.menuDeleteSheet.Name = "menuDeleteSheet";
            this.menuDeleteSheet.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D)));
            this.menuDeleteSheet.Size = new System.Drawing.Size(250, 26);
            this.menuDeleteSheet.Text = "Elimina Foglio";
            this.menuDeleteSheet.Click += new System.EventHandler(this.btnDeleteSheet_Click);

            // Menu Archiviazione
            this.menuArchive.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.menuArchiveSheet,
                this.menuViewArchived
            });
            this.menuArchive.Name = "menuArchive";
            this.menuArchive.Size = new System.Drawing.Size(105, 24);
            this.menuArchive.Text = "Archiviazione";

            this.menuArchiveSheet.Name = "menuArchiveSheet";
            this.menuArchiveSheet.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A)));
            this.menuArchiveSheet.Size = new System.Drawing.Size(320, 26);
            this.menuArchiveSheet.Text = "Archivia Foglio Corrente";
            this.menuArchiveSheet.Click += new System.EventHandler(this.btnArchiveSheet_Click);

            this.menuViewArchived.Name = "menuViewArchived";
            this.menuViewArchived.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.H)));
            this.menuViewArchived.Size = new System.Drawing.Size(320, 26);
            this.menuViewArchived.Text = "Recupera Foglio da Archivio";
            this.menuViewArchived.Click += new System.EventHandler(this.btnViewArchived_Click);

            // Menu Strumenti
            this.menuTools.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.menuManageColumns
            });
            this.menuTools.Name = "menuTools";
            this.menuTools.Size = new System.Drawing.Size(80, 24);
            this.menuTools.Text = "Strumenti";

            this.menuManageColumns.Name = "menuManageColumns";
            this.menuManageColumns.Size = new System.Drawing.Size(250, 26);
            this.menuManageColumns.Text = "Gestione Colonne";

            // Submenu Gestione Colonne
            this.menuManageCauseDismissione.Name = "menuManageCauseDismissione";
            this.menuManageCauseDismissione.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.menuManageCauseDismissione.Size = new System.Drawing.Size(280, 26);
            this.menuManageCauseDismissione.Text = "Causa Dismissione";
            this.menuManageCauseDismissione.Click += new System.EventHandler(this.btnManageOptions_Click);

            this.menuManageTipoAsset.Name = "menuManageTipoAsset";
            this.menuManageTipoAsset.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.T)));
            this.menuManageTipoAsset.Size = new System.Drawing.Size(280, 26);
            this.menuManageTipoAsset.Text = "Tipo Asset";
            this.menuManageTipoAsset.Click += new System.EventHandler(this.btnManageTipoAsset_Click);

            this.menuManageColumns.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.menuManageCauseDismissione,
                this.menuManageTipoAsset
            });

            // Menu Aiuto
            this.menuHelp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.menuAbout
            });
            this.menuHelp.Name = "menuHelp";
            this.menuHelp.Size = new System.Drawing.Size(24, 24);
            this.menuHelp.Text = "?";

            this.menuAbout.Name = "menuAbout";
            this.menuAbout.ShortcutKeys = System.Windows.Forms.Keys.F1;
            this.menuAbout.Size = new System.Drawing.Size(250, 26);
            this.menuAbout.Text = "Informazioni su...";
            this.menuAbout.Click += new System.EventHandler(this.menuAbout_Click);

            // Ricerca
            this.searchTextBox.Name = "searchTextBox";
            this.searchTextBox.Size = new System.Drawing.Size(200, 28);
            this.searchTextBox.ToolTipText = "Cerca in tutti i fogli (Invio per cercare/successivo)";
            this.searchTextBox.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.searchTextBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.searchTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.searchTextBox_KeyDown);

            this.searchButton.Name = "searchButton";
            this.searchButton.Text = "🔍 Cerca";
            this.searchButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.searchButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.searchButton.BackColor = System.Drawing.Color.LightBlue;
            this.searchButton.Click += new System.EventHandler(this.searchButton_Click);

            this.searchNextButton.Name = "searchNextButton";
            this.searchNextButton.Text = "▶";
            this.searchNextButton.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.searchNextButton.ToolTipText = "Risultato successivo";
            this.searchNextButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.searchNextButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.searchNextButton.BackColor = System.Drawing.Color.LightGreen;
            this.searchNextButton.ForeColor = System.Drawing.Color.DarkGreen;
            this.searchNextButton.Visible = false;
            this.searchNextButton.Click += new System.EventHandler(this.searchNextButton_Click);

            // StatusStrip
            this.statusStrip.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.statusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip.Location = new System.Drawing.Point(0, 572);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1200, 28);
            this.statusStrip.TabIndex = 2;

            // TabControl
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(0, 28);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(1200, 544);
            this.tabControl.TabIndex = 1;

            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 600);
            this.Controls.Add(this.tabControl);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.menuStrip);
            this.MainMenuStrip = this.menuStrip;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Gestione Cespiti - Dismissioni";
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
