using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GestioneCespiti.Models;
using GestioneCespiti.Services;

namespace GestioneCespiti.Managers
{
    public class GridManager
    {
        private const string CauseDismissioneColumn = "Causa dismissione";
        private const string TipoAssetColumn = "Tipo asset";
        private const string CustomOptionLabel = "Personalizza...";

        private readonly bool _isReadOnly;

        public event EventHandler<CellValueChangedEventArgs>? CellValueChanged;

        public GridManager(bool isReadOnly)
        {
            _isReadOnly = isReadOnly;
        }

        public void BindGridToSheet(DataGridView grid, AssetSheet sheet, AppSettings settings)
        {
            if (grid.DataSource is DataTable oldTable)
            {
                grid.DataSource = null;
                oldTable.Dispose();
            }

            var table = new DataTable();

            table.Columns.Add("#", typeof(int));

            foreach (var col in sheet.Columns)
            {
                table.Columns.Add(col, typeof(string));
            }

            int rowNumber = 1;
            foreach (var asset in sheet.Rows)
            {
                var row = table.NewRow();
                row["#"] = rowNumber++;
                foreach (var col in sheet.Columns)
                {
                    row[col] = asset[col];
                }
                table.Rows.Add(row);
            }

            grid.AutoGenerateColumns = false;
            grid.Columns.Clear();

            var rowNumberColumn = new DataGridViewTextBoxColumn
            {
                HeaderText = "#",
                Name = "#",
                DataPropertyName = "#",
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                Width = 50,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.LightGray,
                    ForeColor = Color.Black,
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9, FontStyle.Bold)
                }
            };
            grid.Columns.Add(rowNumberColumn);

            foreach (var colName in sheet.Columns)
            {
                DataGridViewColumn column;

                if (colName == CauseDismissioneColumn || colName == TipoAssetColumn)
                {
                    var options = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (colName == CauseDismissioneColumn)
                    {
                        options.UnionWith(settings.CauseDismissioneOptions);
                    }
                    else
                    {
                        options.UnionWith(settings.TipoAssetOptions);
                        options.Add(CustomOptionLabel);
                    }

                    foreach (var asset in sheet.Rows)
                    {
                        var value = asset[colName];
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            options.Add(value);
                        }
                    }

                    var comboColumn = new DataGridViewComboBoxColumn
                    {
                        HeaderText = colName,
                        Name = colName,
                        DataPropertyName = colName,
                        DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                        SortMode = DataGridViewColumnSortMode.NotSortable
                    };

                    foreach (var option in options.OrderBy(option => option))
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
                        DataPropertyName = colName,
                        SortMode = DataGridViewColumnSortMode.NotSortable
                    };
                }

                grid.Columns.Add(column);
            }

            grid.DataSource = table;

            grid.CellValueChanged -= Grid_CellValueChanged;
            grid.CellClick -= Grid_CellClick;
            grid.EditingControlShowing -= Grid_EditingControlShowing;
            grid.CellBeginEdit -= Grid_CellBeginEdit;
            grid.DataError -= Grid_DataError;

            if (!_isReadOnly)
            {
                grid.CellValueChanged += Grid_CellValueChanged;
                grid.CellClick += Grid_CellClick;
                grid.EditingControlShowing += Grid_EditingControlShowing;
                grid.CellBeginEdit += Grid_CellBeginEdit;
            }
            else
            {
                grid.DefaultCellStyle.BackColor = Color.WhiteSmoke;
                grid.DefaultCellStyle.ForeColor = Color.DarkGray;
            }

            grid.DataError += Grid_DataError;
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
            if (e.Control is ComboBox comboBox && sender is DataGridView grid)
            {
                var columnName = grid.Columns[grid.CurrentCell.ColumnIndex].Name;
                comboBox.DropDownStyle = columnName == TipoAssetColumn
                    ? ComboBoxStyle.DropDown
                    : ComboBoxStyle.DropDownList;
            }
        }

        private void Grid_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
        {
            if (sender is not DataGridView grid)
                return;

            var cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
            cell.Tag = cell.Value;
        }

        private void Grid_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || _isReadOnly)
                return;

            if (e.ColumnIndex == 0)
                return;

            var grid = sender as DataGridView;
            if (grid?.Tag is not AssetSheet sheet)
                return;

            string colName = sheet.Columns[e.ColumnIndex - 1];
            var cell = grid.Rows[e.RowIndex].Cells[e.ColumnIndex];
            string newValue = cell.Value?.ToString() ?? string.Empty;

            if (colName == TipoAssetColumn && string.Equals(newValue, CustomOptionLabel, StringComparison.OrdinalIgnoreCase))
            {
                string previousValue = cell.Tag?.ToString() ?? string.Empty;
                cell.Value = previousValue;
                grid.BeginEdit(true);
                return;
            }

            CellValueChanged?.Invoke(this, new CellValueChangedEventArgs(sheet, e.RowIndex, colName, newValue));
        }

        private void Grid_DataError(object? sender, DataGridViewDataErrorEventArgs e)
        {
            e.ThrowException = false;

            if (sender is not DataGridView grid)
            {
                Logger.LogWarning("Errore DataGridView: sender non valido");
                return;
            }

            if (grid.Tag is AssetSheet sheet && e.RowIndex >= 0 && e.ColumnIndex > 0 && e.ColumnIndex - 1 < sheet.Columns.Count)
            {
                string columnName = sheet.Columns[e.ColumnIndex - 1];
                string currentValue = sheet.Rows[e.RowIndex][columnName];
                Logger.LogWarning($"Valore non valido per colonna '{columnName}': '{currentValue}'");
                return;
            }

            Logger.LogWarning($"Errore DataGridView: {e.Exception?.Message}");
        }
    }

    public class CellValueChangedEventArgs : EventArgs
    {
        public AssetSheet Sheet { get; }
        public int RowIndex { get; }
        public string ColumnName { get; }
        public string NewValue { get; }

        public CellValueChangedEventArgs(AssetSheet sheet, int rowIndex, string columnName, string newValue)
        {
            Sheet = sheet;
            RowIndex = rowIndex;
            ColumnName = columnName;
            NewValue = newValue;
        }
    }
}
