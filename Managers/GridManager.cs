using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using GestioneCespiti.Models;

namespace GestioneCespiti.Managers
{
    public class GridManager
    {
        private readonly AppSettings _appSettings;
        private readonly bool _isReadOnly;

        public event EventHandler<CellValueChangedEventArgs>? CellValueChanged;

        public GridManager(AppSettings appSettings, bool isReadOnly)
        {
            _appSettings = appSettings;
            _isReadOnly = isReadOnly;
        }

        public void BindGridToSheet(DataGridView grid, AssetSheet sheet)
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

            if (e.ColumnIndex == 0)
                return;

            var grid = sender as DataGridView;
            if (grid?.Tag is not AssetSheet sheet)
                return;

            string colName = sheet.Columns[e.ColumnIndex - 1];
            string newValue = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? string.Empty;

            CellValueChanged?.Invoke(this, new CellValueChangedEventArgs(sheet, e.RowIndex, colName, newValue));
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
